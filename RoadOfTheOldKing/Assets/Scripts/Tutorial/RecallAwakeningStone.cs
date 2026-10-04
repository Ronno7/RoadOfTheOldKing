using System;
using System.Collections;
using TheLostShrine.Combat;
using TheLostShrine.Input;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.UI;
using UnityEngine;

namespace TheLostShrine.Tutorial
{
    // A thrown axe that strikes the stone awakens Recall: brief control lock, the stone
    // wakes, the axe returns to hand on its own, then controls return and progress saves once.
    [DisallowMultipleComponent]
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
        [Tooltip("The dormant carving blinks cyan so this stone reads apart from the plain ruin pillars.")]
        [SerializeField, Min(0.5f)] private float glintInterval = 2.4f;
        [SerializeField, Min(0.05f)] private float glintSeconds = 0.3f;
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

        public bool IsAwakened { get; private set; }
        public bool IsReady => (string.IsNullOrEmpty(requiredMilestone) ||
            (CheckpointSession.Instance != null && CheckpointSession.Instance.Progress.Has(requiredMilestone))) &&
            (string.IsNullOrEmpty(recoveryMilestone) || (CheckpointSession.Instance != null &&
            CheckpointSession.Instance.Progress.Has(recoveryMilestone) && !EncounterState.InCombat));
        public bool IsAwakening => sequence != null;
        public Look CurrentLook { get; private set; }
        public event Action Awakened;

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
            CheckpointSession.Instance?.SaveProgress();
            if (!string.IsNullOrEmpty(awakenedNotice))
                TutorialNotice.Show(awakenedNotice, 7f);
            Awakened?.Invoke();
        }

        private void Update()
        {
            if (sequence != null) return;
            if (IsAwakened) Show(Look.Awakened);
            else Show(IsReady && Mathf.Repeat(Time.time, glintInterval) < glintSeconds ? Look.Glint : Look.Dormant);
        }

        private void Show(Look look)
        {
            CurrentLook = look;
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
        }

        public void CaptureProgress(ProgressState state) { }

        public void RestoreProgress(ProgressState state)
        {
            IsAwakened = state.recallUnlocked;
            Show(IsAwakened ? Look.Awakened : Look.Dormant);
        }
    }
}
