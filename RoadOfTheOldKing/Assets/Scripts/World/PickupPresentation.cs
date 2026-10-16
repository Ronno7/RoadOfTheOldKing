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
        private SpriteRenderer source, art;
        private readonly SpriteRenderer[] glint = new SpriteRenderer[2];
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
                    if (turn < .55f) width = Mathf.Max(.25f, Mathf.Round(Mathf.Abs(Mathf.Cos(turn / .55f * Mathf.PI)) * 4f) / 4f);
                    if (age < .45f) lift += Mathf.Sin(age / .45f * Mathf.PI) * .5f;
                }
                if (style != PickupStyle.Grounded) lift += focus / 16f;
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
        }

        private void OnDisable()
        {
            if (source != null) source.forceRenderingOff = previousForceOff;
            if (art != null) art.enabled = false;
            foreach (var mark in glint) if (mark != null) mark.enabled = false;
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
