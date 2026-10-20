using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // A visual cutaway only: the room, its interactions and its collision remain live.
    // Each renderer belongs to one group in one cutaway; leave outdoor machinery outside it.
    [DisallowMultipleComponent]
    public sealed class BuildingCutaway : MonoBehaviour
    {
        [SerializeField] private Transform occupant;
        [Tooltip("Interior and doorway rectangles in this object's local XY plane. Use the player's feet as occupant.")]
        [SerializeField] private Rect[] revealAreas = System.Array.Empty<Rect>();
        [Tooltip("Roof/facade renderers only. Sprite and Tilemap colors fade; other renderer types switch visibility.")]
        [SerializeField] private Renderer[] coveringRenderers = System.Array.Empty<Renderer>();
        [Tooltip("Interior artwork revealed as the covers fade. Use renderers whose visibility is owned by this cutaway, not another animation/state component.")]
        [SerializeField] private Renderer[] interiorRenderers = System.Array.Empty<Renderer>();
        [SerializeField, Min(.01f)] private float fadeSeconds = .2f;
        [Tooltip("Keep the roof open this far beyond the interior boundary to avoid flickering at a doorway.")]
        [SerializeField, Min(0f)] private float exitHysteresis = .25f;

        private struct Cover
        {
            public Renderer Renderer;
            public SpriteRenderer Sprite;
            public Tilemap Tiles;
            public Color Color;
            public bool Enabled;
            public bool Interior;
        }

        private Cover[] covers;
        public bool IsRevealed { get; private set; }
        // One is the intact exterior; zero is a fully revealed interior.
        public float Coverage { get; private set; } = 1f;

        private void Cache()
        {
            if (covers != null) return;
            int coverCount = coveringRenderers != null ? coveringRenderers.Length : 0;
            int count = coverCount + (interiorRenderers != null ? interiorRenderers.Length : 0);
            covers = new Cover[count];
            for (int i = 0; i < count; i++)
            {
                bool interior = i >= coverCount;
                Renderer renderer = interior ? interiorRenderers[i - coverCount] : coveringRenderers[i];
                if (renderer == null) continue;
                var sprite = renderer as SpriteRenderer;
                var tiles = renderer is TilemapRenderer ? renderer.GetComponent<Tilemap>() : null;
                covers[i] = new Cover
                {
                    Renderer = renderer,
                    Sprite = sprite,
                    Tiles = tiles,
                    Color = sprite != null ? sprite.color : tiles != null ? tiles.color : Color.white,
                    Enabled = renderer.enabled,
                    Interior = interior
                };
            }
        }

        private void OnEnable()
        {
            Cache();
            IsRevealed = false;
            // Continue or a scene arrival may start beneath the roof. Do not obscure
            // that first frame while waiting for an entrance trigger or fade.
            if (occupant != null) EvaluateAt(occupant.position, true);
            else
            {
                Coverage = 1f;
                Draw();
            }
        }

        // Scene-session placement may run after OnEnable, while the transition
        // curtain is still up. Start observes that final arrival before rendering.
        private void Start()
        {
            if (occupant != null) EvaluateAt(occupant.position, true);
        }

        private void LateUpdate()
        {
            if (occupant != null) EvaluateAt(occupant.position);
            else IsRevealed = false;
            Advance(Time.deltaTime);
        }

        // Also usable for an explicit authoring preview or deterministic verification.
        // This never moves the occupant; disabling the component restores authored visuals.
        public void EvaluateAt(Vector2 worldPosition, bool immediate = false)
        {
            Cache();
            Vector3 local = transform.InverseTransformPoint(
                new Vector3(worldPosition.x, worldPosition.y, transform.position.z));
            float margin = IsRevealed ? Mathf.Max(0f, exitHysteresis) : 0f;
            bool inside = false;
            if (revealAreas != null)
            {
                for (int i = 0; i < revealAreas.Length; i++)
                {
                    Rect area = revealAreas[i];
                    if (area.width <= 0f || area.height <= 0f) continue;
                    if (local.x >= area.xMin - margin && local.x < area.xMax + margin &&
                        local.y >= area.yMin - margin && local.y < area.yMax + margin)
                    {
                        inside = true;
                        break;
                    }
                }
            }
            IsRevealed = inside;
            if (!immediate) return;
            Coverage = inside ? 0f : 1f;
            Draw();
        }

        private void Advance(float seconds)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            float next = Mathf.MoveTowards(Coverage, IsRevealed ? 0f : 1f,
                seconds / Mathf.Max(.01f, fadeSeconds));
            if (next == Coverage) return;
            Coverage = next;
            Draw();
        }

        private void Draw()
        {
            Cache();
            for (int i = 0; i < covers.Length; i++)
            {
                Cover cover = covers[i];
                if (cover.Renderer == null) continue;
                float visibility = cover.Interior ? 1f - Coverage : Coverage;
                Color color = cover.Color;
                color.a *= visibility;
                if (cover.Sprite != null) cover.Sprite.color = color;
                else if (cover.Tiles != null) cover.Tiles.color = color;
                cover.Renderer.enabled = cover.Enabled && visibility > 0f;
            }
        }

        // Ends an authoring preview before rebuilding artwork or renderer bindings.
        // Also restores the complete authored state when the component is disabled.
        public void RestoreAuthoredVisuals()
        {
            if (covers == null) return;
            for (int i = 0; i < covers.Length; i++)
            {
                Cover cover = covers[i];
                if (cover.Renderer == null) continue;
                if (cover.Sprite != null) cover.Sprite.color = cover.Color;
                else if (cover.Tiles != null) cover.Tiles.color = cover.Color;
                cover.Renderer.enabled = cover.Enabled;
            }
            covers = null;
            IsRevealed = false;
            Coverage = 1f;
        }

        private void OnDisable() => RestoreAuthoredVisuals();
    }
}
