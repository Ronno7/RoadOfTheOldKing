using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum PickupStyle { Heart, SunShard, Coin, Grounded }

    // Art-only proxy: the authored renderer, collider, ground anchor and reward owner never move.
    // Explicitly authored on pickup artwork; no scene scans or loot RNG in the animation loop.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer)), DefaultExecutionOrder(250)]
    public sealed class PickupPresentation : MonoBehaviour
    {
        [SerializeField] private PickupStyle style;
        [SerializeField] private Sprite sparkSprite;
        [Tooltip("Coin style: shown instead of the single coin when the pickup holds more than one.")]
        [SerializeField] private Sprite pileSprite;
        [Tooltip("Played once when the reward first appears (an encounter unlock), never on load.")]
        [SerializeField] private AudioClip revealSound;
        private SpriteRenderer source, art, column;
        private readonly SpriteRenderer[] glint = new SpriteRenderer[2];
        // Shards and hearts keep a slow rise of pixel motes while uncollected: a Souls-style item glow that reads
        // past grass and from a distance without a continuous flash.
        private readonly SpriteRenderer[] beacon = new SpriteRenderer[3];
        private Sprite singleSprite;
        private bool pile;
        private float reveal = -1f;
        private const float RevealDrop = .45f, RevealSettle = .2f, RevealLight = 1.1f, BeaconCycle = 1.8f;
        private WorldPickup owner;
        private RewardChest chest;
        private PlayerBonfireInteraction interaction;
        private float age, phase, focus;
        private bool previousForceOff;
        public PickupStyle Style => style;
        internal static readonly Color Warm = new Color32(255, 240, 179, 255);
        internal static readonly Color Heart = new Color32(230, 162, 117, 255);

        private void Awake()
        {
            source = GetComponent<SpriteRenderer>();
            owner = GetComponentInParent<WorldPickup>();
            chest = owner as RewardChest;
            phase = Mathf.Repeat(transform.position.x * .754877f + transform.position.y * .56984f, 1f);
            art = MakeSprite("Animated pickup art", transform, source.sprite, source);
            for (int i = 0; i < glint.Length; i++)
            {
                glint[i] = MakeSprite("Pickup glint", art.transform, sparkSprite, source);
                BindSolidTexture(glint[i]);
            }
            if (sparkSprite == null || (style != PickupStyle.SunShard && style != PickupStyle.Heart)) return;
            for (int i = 0; i < beacon.Length; i++)
            {
                beacon[i] = MakeSprite("Pickup beacon", transform, sparkSprite, source);
                BindSolidTexture(beacon[i]);
            }
            column = MakeSprite("Reveal light", transform, sparkSprite, source);
            BindSolidTexture(column);
        }

        private void OnEnable()
        {
            age = 0f;
            focus = 0f;
            previousForceOff = source.forceRenderingOff;
        }

        private void Start() => interaction = FindFirstObjectByType<PlayerBonfireInteraction>();

        private void LateUpdate() => Advance(Time.deltaTime);

        private void Advance(float elapsed)
        {
            age += elapsed;
            bool visible = source.enabled && source.sprite != null && !previousForceOff;
            source.forceRenderingOff = visible || previousForceOff;
            art.enabled = visible;
            art.sprite = source.sprite;
            art.color = source.color;
            art.flipX = source.flipX;
            art.flipY = source.flipY;
            art.sortingLayerID = source.sortingLayerID;
            art.sortingOrder = source.sortingOrder;
            bool readable = visible && (chest == null || !chest.IsOpen) && DarkInterior.CanRead(transform.position);
            bool selected = readable && owner != null && interaction != null && interaction.NearbyPickup == owner;
            focus = Mathf.MoveTowards(focus, selected ? 1f : 0f, elapsed * 6f);
            float t = age + phase * 4f;
            float lift = 0f, width = 1f, height = 1f;
            if (readable)
            {
                if (style == PickupStyle.Heart)
                {
                    lift = (1f + Mathf.Sin(t * 2.1f)) / 16f;
                    float beat = Mathf.Repeat(t, 2.6f);
                    // Two short beats with a long rest. No whole-object brightness flash.
                    if (beat < .12f || (beat > .25f && beat < .34f)) { width = 1.0625f; height = 1.0625f; }
                }
                else if (style == PickupStyle.SunShard)
                    lift = (2f + 1.5f * Mathf.Sin(t * 1.7f)) / 16f;
                else if (style == PickupStyle.Coin)
                {
                    lift = (1f + Mathf.Sin(t * 2.4f)) / 32f;
                    // A brief turn, then hold the readable face; never continuously strobe.
                    float turn = Mathf.Repeat(t, 3.4f);
                    if (turn < .55f && !pile) width = Mathf.Max(.25f, Mathf.Round(Mathf.Abs(Mathf.Cos(turn / .55f * Mathf.PI)) * 4f) / 4f);
                    if (age < .45f) lift += Mathf.Sin(age / .45f * Mathf.PI) * .5f;
                }
                if (style != PickupStyle.Grounded) lift += focus / 16f;
            }
            if (reveal >= 0f)
            {
                // Appearance: fall in from above, one small bounce, while the light column fades.
                reveal += elapsed;
                if (reveal < RevealDrop) { float k = reveal / RevealDrop; lift += 1.25f * (1f - k * k); }
                else if (reveal < RevealDrop + RevealSettle) lift += Mathf.Sin((reveal - RevealDrop) / RevealSettle * Mathf.PI) * 2f / 16f;
                if (reveal > RevealLight) reveal = -1f;
            }
            art.transform.localPosition = Vector3.up * (Mathf.Round(lift * 16f) / 16f);
            art.transform.localScale = new Vector3(width, height, 1f);

            float cycle = Mathf.Repeat(t, selected ? 1.8f : style == PickupStyle.Coin ? 4.6f : 3.6f);
            bool shine = readable && sparkSprite != null && cycle < .42f && FeedbackSettings.FlashScale > 0f;
            Vector3 tip = source.sprite == null ? Vector3.zero : source.sprite.bounds.center + new Vector3(source.sprite.bounds.extents.x * .85f, source.sprite.bounds.extents.y * .7f, 0f);
            tip.x = Mathf.Round(tip.x * 16f) / 16f;
            tip.y = Mathf.Round(tip.y * 16f) / 16f;
            float inkAlpha = Mathf.Clamp01(FeedbackSettings.FlashScale) * source.color.a;
            Color accent = Warm;
            accent.a = inkAlpha;
            for (int i = 0; i < glint.Length; i++)
            {
                var mark = glint[i];
                mark.enabled = shine;
                if (!shine) continue;
                mark.sortingLayerID = art.sortingLayerID;
                mark.sortingOrder = art.sortingOrder + 1;
                mark.transform.localPosition = tip;
                int length = cycle > .09f && cycle < .28f ? 3 : 1;
                Vector2 size = i % 2 == 0 ? new Vector2(length, 1) : new Vector2(1, length);
                Size(mark, size / 16f);
                mark.color = accent;
            }
            UpdateBeacon(readable, inkAlpha, t);
        }

        private void UpdateBeacon(bool readable, float flash, float t)
        {
            Color accent = style == PickupStyle.Heart ? Heart : Warm;
            if (column != null)
            {
                bool lit = readable && reveal >= 0f && flash > 0f;
                column.enabled = lit;
                if (lit)
                {
                    float k = reveal / RevealLight;
                    float height = Mathf.Round(Mathf.Lerp(.5f, 3f, Mathf.Min(1f, k * 4f)) * 16f) / 16f;
                    column.sortingLayerID = art.sortingLayerID;
                    column.sortingOrder = art.sortingOrder - 1;
                    column.transform.localPosition = new Vector3(0f, Mathf.Round(height * 8f) / 16f, 0f);
                    Size(column, new Vector2(k < .25f ? 4f : 2f, height * 16f) / 16f);
                    Color light = accent;
                    light.a = (1f - k) * .75f * flash;
                    column.color = light;
                }
            }
            for (int i = 0; i < beacon.Length; i++)
            {
                var mote = beacon[i];
                if (mote == null) continue;
                mote.enabled = readable && flash > 0f;
                if (!mote.enabled) continue;
                float cycle = t + i * BeaconCycle / beacon.Length;
                float p = Mathf.Repeat(cycle, BeaconCycle) / BeaconCycle;
                // Each rise starts at a new deterministic offset across the item's width.
                float jitter = Mathf.Repeat(Mathf.Sin(Mathf.Floor(cycle / BeaconCycle) * 12.9898f + i * 78.233f) * 43758.5453f, 1f);
                mote.transform.localPosition = new Vector3(Mathf.Round((jitter - .5f) * 8f) / 16f, Mathf.Round(2f + p * 14f) / 16f, 0f);
                mote.sortingLayerID = art.sortingLayerID;
                mote.sortingOrder = art.sortingOrder + 1;
                Size(mote, Vector2.one * (i == 0 ? 2f : 1f) / 16f);
                Color glow = accent;
                glow.a = Mathf.Sin(p * Mathf.PI) * .9f * flash;
                mote.color = glow;
            }
        }

        private void OnDisable()
        {
            if (source != null) source.forceRenderingOff = previousForceOff;
            if (art != null) art.enabled = false;
            foreach (var mark in glint) if (mark != null) mark.enabled = false;
            foreach (var mote in beacon) if (mote != null) mote.enabled = false;
            if (column != null) column.enabled = false;
            reveal = -1f;
        }

        // Call when a reward first appears in play (not on load or restore): it falls in under a brief light
        // with a chime, so a drop that appears where the last guard fell is noticed.
        public static void Revealed(GameObject visual)
        {
            if (visual == null) return;
            foreach (var view in visual.GetComponentsInChildren<PickupPresentation>(true))
            {
                if (!view.isActiveAndEnabled) continue;
                view.reveal = 0f;
                if (view.revealSound != null && DarkInterior.CanRead(view.transform.position)) PlayOnce(view.revealSound, view.transform.position);
            }
        }

        // Coin pickups: a pile for more than one coin, the single coin otherwise.
        public static void ShowCoinAmount(GameObject visual, int amount)
        {
            if (visual == null) return;
            foreach (var view in visual.GetComponentsInChildren<PickupPresentation>(true))
            {
                if (view.style != PickupStyle.Coin || view.pileSprite == null) continue;
                var renderer = view.GetComponent<SpriteRenderer>();
                if (view.singleSprite == null) view.singleSprite = renderer.sprite;
                view.pile = amount > 1;
                renderer.sprite = view.pile ? view.pileSprite : view.singleSprite;
            }
        }

        private static void PlayOnce(AudioClip clip, Vector3 at)
        {
            var go = new GameObject("Reward reveal sound");
            go.transform.position = at;
            var audio = go.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = .5f;
            audio.PlayOneShot(clip);
            Destroy(go, clip.length + .1f);
        }

        private void OnDestroy()
        {
            if (art != null) Destroy(art.gameObject);
        }

        // Call only after a successful reward transaction, before hiding its visual.
        // Restore/respawn/reload never call this and therefore never replay a collection.
        public static void Collected(GameObject visual, PlayerHealth player = null)
        {
            if (visual == null) return;
            foreach (var view in visual.GetComponentsInChildren<PickupPresentation>(true))
            {
                if (view.art == null || !view.source.enabled || !view.gameObject.activeInHierarchy ||
                    !DarkInterior.CanRead(view.transform.position)) continue;
                PickupCollectBurst.Play(view.art, view.sparkSprite, view.style, player);
            }
        }

        internal static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, SpriteRenderer template)
        {
            var go = new GameObject(name);
            go.layer = template.gameObject.layer;
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = template.sharedMaterial;
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sortingOrder = template.sortingOrder;
            renderer.maskInteraction = template.maskInteraction;
            renderer.enabled = false;
            return renderer;
        }

        internal static void Size(SpriteRenderer renderer, Vector2 size)
        {
            if (renderer.sprite == null) return;
            var bounds = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1f);
        }

        internal static void BindSolidTexture(SpriteRenderer renderer)
        {
            // Explicit per-renderer binding prevents tiny marks sampling the preceding atlas draw.
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            renderer.SetPropertyBlock(properties);
        }
    }
}
