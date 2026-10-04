using TheLostShrine.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLostShrine.Combat
{
    // Reads the same health used by combat, including restoration and capacity changes.
    // World-space pixel bar (UI/EnemyHealthBar.uxml: a PixelBar in the corner vitals' style) above the body.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    [DefaultExecutionOrder(300)]
    public sealed class EnemyHealthIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PanelSettings panel;
        [SerializeField] private VisualTreeAsset layout;
        [Tooltip("Clearance between the anchor and the bar.")]
        [SerializeField, Min(0f)] private float gap = .12f;
        [Tooltip("Height above the enemy's pivot to anchor on (world units). Steadier than sprite bounds, whose canvases differ between animations; 0 falls back to the bounds.")]
        [SerializeField, Min(0f)] private float anchorHeight;
        private Damageable health;
        private WorldUIDocument ui;
        private PixelBar bar;
        private float previousFraction = -1f;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) { enabled = false; return; }
            if (panel == null || layout == null) { Debug.LogError("EnemyHealthIndicator needs the world panel and its layout.", this); enabled = false; return; }
            ui = WorldUIDocument.Create("Enemy health bar", transform, panel, layout, 30000, Pivot.BottomCenter);
            bar = ui.Q<PixelBar>("enemy-health");
            ui.Show(false);
        }

        private void LateUpdate()
        {
            if (ui == null) return;
            bool show = health.IsAlive && body != null && body.enabled && body.gameObject.activeInHierarchy;
            ui.Show(show);
            if (!show) return;
            float top = anchorHeight > 0f ? transform.position.y + anchorHeight : body.bounds.max.y;
            ui.Place(new Vector3(transform.position.x, top + gap, transform.position.z));
            float fraction = Mathf.Clamp01((float)health.Health / Mathf.Max(1, health.MaxHealth));
            if (fraction == previousFraction) return;
            previousFraction = fraction;
            bar.SetValue(fraction);
        }

        private void OnDisable() { if (ui != null) ui.Show(false); }
        private void OnDestroy() { if (ui != null) Destroy(ui.gameObject); }
    }
}
