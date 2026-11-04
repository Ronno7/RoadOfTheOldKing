using System;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The Zelda beat on a real find: the world stops and the hero holds up what they found. The reward is already
    // saved when this starts, so nothing depends on it finishing. Presentation only; it holds the pause itself, so
    // it runs on unscaled time and PlayerSpriteAnimator reads IsPlaying to face the camera.
    [DisallowMultipleComponent]
    public sealed class RewardHoldUp : MonoBehaviour
    {
        [SerializeField] private AudioClip sound;
        [SerializeField, Range(0f, 1f)] private float volume = .45f;
        [SerializeField, Min(.05f)] private float riseSeconds = .22f;
        [SerializeField, Min(.1f)] private float holdSeconds = .9f;
        [SerializeField, Min(.05f)] private float fadeSeconds = .16f;
        [Tooltip("Where the item is held, in units above the hero's feet.")]
        [SerializeField, Min(0f)] private float overHead = 2.1f;

        // Staged checks step time themselves. Set per Play session; every new session starts with it off.
        public static bool SkipForChecks;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSkip() => SkipForChecks = false;

        private const int MoteCount = 6, SortOrder = 200;
        private SpriteRenderer item, column;
        private readonly SpriteRenderer[] motes = new SpriteRenderer[MoteCount];
        private PlayerControlLocks locks;
        private PlayerDash dash;
        private AudioSource speaker;
        private IDisposable pause;
        private Vector3 from;
        private Color accent;
        private float age;

        public bool IsPlaying => pause != null;

        // False when it can't run, so the caller falls back to its ordinary pickup burst.
        public static bool TryPlay(PlayerHealth player, Sprite held, Vector3 from, Color accent, SpriteRenderer template)
        {
            if (SkipForChecks || player == null || held == null || template == null) return false;
            var holdUp = player.GetComponent<RewardHoldUp>();
            return holdUp != null && holdUp.isActiveAndEnabled && holdUp.Begin(held, from, accent, template);
        }

        private void Awake()
        {
            locks = GetComponent<PlayerControlLocks>();
            dash = GetComponent<PlayerDash>();
        }

        private bool Begin(Sprite held, Vector3 start, Color glow, SpriteRenderer template)
        {
            Finish();
            Build(template);
            item.sprite = held;
            item.color = Color.white;
            item.enabled = true;
            from = start;
            accent = glow;
            age = 0f;
            pause = SimulationPause.Acquire();
            if (locks != null) locks.Lock(this);
            if (dash != null) dash.Cancel();
            if (sound != null) speaker.PlayOneShot(sound, volume);
            Advance(0f);
            return true;
        }

        private void Build(SpriteRenderer template)
        {
            if (item != null) return;
            var root = new GameObject("Reward hold-up");
            root.layer = gameObject.layer;
            root.transform.SetParent(transform, false);
            speaker = root.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            item = PickupPresentation.MakeSprite("Held item", root.transform, null, template);
            column = PickupPresentation.MakeSprite("Held light", root.transform, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f), template);
            PickupPresentation.BindSolidTexture(column);
            for (int i = 0; i < motes.Length; i++)
            {
                motes[i] = PickupPresentation.MakeSprite("Held mote", root.transform, column.sprite, template);
                PickupPresentation.BindSolidTexture(motes[i]);
            }
            item.sortingLayerName = column.sortingLayerName = "Player";
            item.sortingOrder = SortOrder;
            column.sortingOrder = SortOrder - 1;
            foreach (var mote in motes) { mote.sortingLayerName = "Player"; mote.sortingOrder = SortOrder + 1; }
        }

        private void Update()
        {
            if (!IsPlaying) return;
            age += Time.unscaledDeltaTime;
            if (age >= riseSeconds + holdSeconds + fadeSeconds) { Finish(); return; }
            Advance(age);
        }

        private void Advance(float t)
        {
            float rise = Mathf.Clamp01(t / riseSeconds);
            rise = 1f - (1f - rise) * (1f - rise);
            Vector3 head = transform.position + Vector3.up * overHead;
            item.transform.position = Snap(Vector3.Lerp(from, head, rise));
            float fade = 1f - Mathf.Clamp01((t - riseSeconds - holdSeconds) / fadeSeconds);
            item.color = new Color(1f, 1f, 1f, fade);

            float flash = Mathf.Clamp01(FeedbackSettings.FlashScale) * fade;
            bool lit = flash > 0f && t >= riseSeconds * .5f;
            column.enabled = lit;
            if (lit)
            {
                // A brief column behind the item, the same light a shard falls in under.
                float settle = Mathf.Clamp01((t - riseSeconds) / holdSeconds);
                PickupPresentation.Size(column, new Vector2(settle < .25f ? 4f : 2f, 40f) / 16f);
                column.transform.position = Snap(item.transform.position);
                column.color = new Color(accent.r, accent.g, accent.b, (1f - settle) * .55f * flash);
            }
            for (int i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                mote.enabled = lit;
                if (!lit) continue;
                // Whole-pixel motes rising past the item on staggered cycles.
                float p = Mathf.Repeat(t * .9f + i / (float)motes.Length, 1f);
                float side = (i - (motes.Length - 1) * .5f) * 3f;
                mote.transform.position = Snap(item.transform.position + new Vector3(side / 16f, (p * 22f - 8f) / 16f, 0f));
                PickupPresentation.Size(mote, Vector2.one * (i % 3 == 0 ? 2f : 1f) / 16f);
                mote.color = new Color(accent.r, accent.g, accent.b, Mathf.Sin(p * Mathf.PI) * flash);
            }
        }

        private static Vector3 Snap(Vector3 p) => new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, p.z);

        private void Finish()
        {
            if (item != null) item.enabled = false;
            if (column != null) column.enabled = false;
            foreach (var mote in motes) if (mote != null) mote.enabled = false;
            if (pause == null) return;
            pause.Dispose();
            pause = null;
            if (locks != null) locks.Unlock(this);
        }

        private void OnDisable() => Finish();
    }
}
