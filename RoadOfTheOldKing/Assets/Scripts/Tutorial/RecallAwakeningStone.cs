using System;
using System.Collections;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Input;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    // A thrown axe that strikes the stone awakens Recall: brief control lock, the stone
    // wakes, the axe returns to hand on its own, then controls return and progress saves once.
    [DisallowMultipleComponent, DefaultExecutionOrder(250)]
    public sealed class RecallAwakeningStone : MonoBehaviour, IHitReceiver, IProgressParticipant
    {
        public enum Look { Dormant, Glint, Charging, Awakening, Awakened }

        [Serializable]
        private struct Frame { public Sprite top; public Sprite footing; }

        [SerializeField, Min(0.1f)] private float awakeningSeconds = 1.4f;
        [SerializeField, Min(0.5f)] private float returnTimeout = 3f;
        [Header("Presentation")]
        [Tooltip("Split like the tile ruins: footing draws under the player, the rest over them.")]
        [SerializeField] private SpriteRenderer topRenderer;
        [SerializeField] private SpriteRenderer footingRenderer;
        [Tooltip("Indexed by Look: Dormant, Glint, Charging, Awakening, Awakened.")]
        [SerializeField] private Frame[] frames = new Frame[5];
        [Tooltip("Ready-only cyan carving pulse and two small glimmers; no cue before safe recovery.")]
        [SerializeField, Min(0.5f)] private float glintInterval = 2.4f;
        [SerializeField, Min(0.05f)] private float glintSeconds = 0.75f;
        [SerializeField] private Sprite glimmerSprite;
        [SerializeField, Min(0.1f)] private float chargingFraction = 0.4f;
        [SerializeField, Min(0.02f)] private float flickerSeconds = 0.1f;
        [Header("Hit body")]
        [Tooltip("Catches throws aimed at the carving; the player walks behind it (collision ignored for the player only).")]
        [SerializeField] private Collider2D hitBody;
        [Header("Waking condition")]
        [Tooltip("Progress milestone the stone waits for (e.g. the first wolf's defeat). Until then it doesn't " +
            "glint and a throw only shows silentNotice. Empty: ready from the start.")]
        [SerializeField] private string requiredMilestone;
        [SerializeField, TextArea] private string silentNotice = "The stone is cold.";
        [SerializeField, TextArea] private string awakenedNotice = "Recall awakened {recall}";
        [SerializeField] private string recoveryMilestone;
        private PlayerControlLocks locks;
        private PlayerHealth lockedHealth;
        private Coroutine sequence;
        private float silentNoticeAt = -10f;
        private readonly SpriteRenderer[] glimmer = new SpriteRenderer[4];
        private float readyAge, glimmerPhase;

        public bool IsAwakened { get; private set; }
        public bool IsReady => (string.IsNullOrEmpty(requiredMilestone) ||
            (GameSession.Instance != null && GameSession.Instance.Progress.Has(requiredMilestone))) &&
            (string.IsNullOrEmpty(recoveryMilestone) || (GameSession.Instance != null &&
            GameSession.Instance.Progress.Has(recoveryMilestone) && !EncounterState.InCombat));
        public bool IsAwakening => sequence != null;
        public Look CurrentLook { get; private set; }
        public event Action Awakened;

        private void Awake()
        {
            if (topRenderer == null || glimmerSprite == null) return;
            for (int i = 0; i < glimmer.Length; i++)
            {
                glimmer[i] = PickupPresentation.MakeSprite("Recall glimmer", topRenderer.transform, glimmerSprite, topRenderer);
                PickupPresentation.BindSolidTexture(glimmer[i]);
            }
        }

        private void Start()
        {
            if (hitBody == null) return;
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player != null)
                foreach (var own in player.GetComponentsInChildren<Collider2D>(true))
                    Physics2D.IgnoreCollision(hitBody, own, true);
        }

        public bool ReceiveHit(CombatHit hit)
        {
            if (IsAwakened || sequence != null || hit.Kind != AttackKind.Throw || hit.Source == null)
                return false;
            var player = hit.Source.GetComponent<PlayerCombatController>();
            if (player == null || player.CanRecall || player.Weapon == null)
                return false;
            if (!IsReady)
            {
                if (!string.IsNullOrEmpty(silentNotice) && Time.time - silentNoticeAt > 4f)
                {
                    silentNoticeAt = Time.time;
                    HudNotifications.Post(silentNotice);
                }
                return false;
            }
            sequence = StartCoroutine(Awaken(player));
            return true;
        }

        private IEnumerator Awaken(PlayerCombatController player)
        {
            var weapon = player.Weapon;
            LockControls(player);
            for (float t = 0f; t < awakeningSeconds; t += Time.deltaTime)
            {
                if (!PlayerAlive()) { Abort(); yield break; }
                float charged = awakeningSeconds * chargingFraction;
                Show(t < charged ? Look.Charging
                    : (int)((t - charged) / flickerSeconds) % 2 == 0 ? Look.Awakening : Look.Awakened);
                yield return null;
            }
            IsAwakened = true;
            Show(Look.Awakening);
            player.UnlockRecall();
            weapon.TryRecall();
            for (float t = 0f; weapon.IsAway && t < returnTimeout; t += Time.deltaTime)
            {
                if (!PlayerAlive()) break;
                yield return null;
            }
            Show(Look.Awakened);
            UnlockControls();
            sequence = null;
            if (!PlayerAlive())
                yield break;
            GameSession.Instance?.SaveProgress();
            if (!string.IsNullOrEmpty(awakenedNotice))
                TutorialNotice.Show(awakenedNotice, 7f);
            Awakened?.Invoke();
        }

        private void Update()
        {
            if (sequence != null) return;
            if (IsAwakened) { Show(Look.Awakened); return; }
            if (!IsReady) { readyAge = 0f; Show(Look.Dormant); return; }
            float cycle = Mathf.Repeat(readyAge, Mathf.Max(.5f, glintInterval));
            glimmerPhase = cycle / Mathf.Max(.05f, glintSeconds);
            Show(cycle < glintSeconds && FeedbackSettings.FlashScale > 0f ? Look.Glint : Look.Dormant);
            readyAge += Time.deltaTime;
        }

        private void LateUpdate()
        {
            if (CurrentLook != Look.Glint || sequence != null || IsAwakened || !IsReady ||
                topRenderer == null || !topRenderer.enabled || topRenderer.forceRenderingOff || FeedbackSettings.FlashScale <= 0f)
            { HideGlimmer(); return; }
            for (int i = 0; i < glimmer.Length; i++)
            {
                var mark = glimmer[i];
                if (mark == null) continue;
                int spark = i / 2;
                float t = (glimmerPhase - spark * .38f) / .62f;
                mark.enabled = t > 0f && t < 1f;
                if (!mark.enabled) continue;
                // Native centers of the approved wheel's upper-left/lower-right glyph marks,
                // relative to Top's bottom pivot. The actual stone and hit body never move.
                mark.transform.localPosition = spark == 0 ? new Vector3(-3.5f / 16, 14.5f / 16) : new Vector3(2.5f / 16, 8.5f / 16);
                int length = t > .25f && t < .75f ? (spark == 0 ? 5 : 3) : 1;
                PickupPresentation.Size(mark, (i % 2 == 0 ? new Vector2(length, 1) : new Vector2(1, length)) / 16f);
                mark.sortingLayerID = topRenderer.sortingLayerID;
                mark.sortingOrder = topRenderer.sortingOrder + 1;
                Color color = new Color32(207, 248, 255, 255);
                color.a = Mathf.Sin(t * Mathf.PI) * Mathf.Clamp01(FeedbackSettings.FlashScale) * topRenderer.color.a;
                mark.color = color;
            }
        }

        private void HideGlimmer()
        {
            foreach (var mark in glimmer) if (mark != null) mark.enabled = false;
        }

        private void Show(Look look)
        {
            CurrentLook = look;
            if (look != Look.Glint) HideGlimmer();
            if (frames == null || (int)look >= frames.Length) return;
            var frame = frames[(int)look];
            if (topRenderer != null && frame.top != null) topRenderer.sprite = frame.top;
            if (footingRenderer != null && frame.footing != null) footingRenderer.sprite = frame.footing;
        }

        private bool PlayerAlive() => lockedHealth == null || lockedHealth.IsAlive;

        private void Abort()
        {
            UnlockControls();
            sequence = null;
            Show(IsAwakened ? Look.Awakened : Look.Dormant);
        }

        // A control-lock lease, like the bonfire menu; released only by this sequence.
        private void LockControls(PlayerCombatController player)
        {
            lockedHealth = player.GetComponent<PlayerHealth>();
            locks = player.GetComponent<PlayerControlLocks>();
            if (locks != null) locks.Lock(this);
        }

        private void UnlockControls()
        {
            // Death disables input separately; releasing the lease never revives a dead player.
            if (locks != null) locks.Unlock(this);
            locks = null;
        }

        private void OnDisable()
        {
            if (sequence != null) Abort();
            readyAge = 0f;
            HideGlimmer();
        }

        private void OnDestroy()
        {
            foreach (var mark in glimmer) if (mark != null) Destroy(mark.gameObject);
        }

        public void CaptureProgress(ProgressState state) { }

        public void RestoreProgress(ProgressState state)
        {
            IsAwakened = state.recallUnlocked;
            readyAge = 0f;
            Show(IsAwakened ? Look.Awakened : Look.Dormant);
        }
    }
}
