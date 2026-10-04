using TheLostShrine.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLostShrine.Combat
{
    // World-space pixel "!" (UI/AwarenessMark.uxml, sprite Art/Sprites/UI/AwarenessMark.png).
    // Detection belongs to the enemy; this only presents its transition.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(310)]
    public sealed class EnemyAwarenessIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PanelSettings panel;
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField, Min(.1f)] private float duration = .75f;
        [Tooltip("Clearance above the anchor, leaving room for the health bar.")]
        [SerializeField, Min(0f)] private float gap = .375f;
        [Tooltip("Height above the enemy's pivot to anchor on (world units). Steadier than sprite bounds, whose canvases differ between animations; 0 falls back to the bounds.")]
        [SerializeField, Min(0f)] private float anchorHeight;
        private IEnemy enemy;
        private Behaviour enemyBehaviour;
        private WorldUIDocument ui;
        private float remaining;
        private int resetVersion;
        private const float Pixel = 1f / 16f;

        private void Awake()
        {
            enemy = GetComponent<IEnemy>();
            enemyBehaviour = enemy as Behaviour;
            if (enemy == null) { enabled = false; return; }
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) { enabled = false; return; }
            if (panel == null || layout == null) { Debug.LogError("EnemyAwarenessIndicator needs the world panel and its layout.", this); enabled = false; return; }
            ui = WorldUIDocument.Create("Enemy awareness !", transform, panel, layout, 30001, Pivot.BottomCenter);
            ui.Show(false);
        }

        private void OnEnable()
        {
            if (enemy == null) return;
            resetVersion = enemy.ResetVersion;
            enemy.PlayerDetected += OnDetected;
        }

        private void OnDetected()
        {
            resetVersion = enemy.ResetVersion;
            remaining = duration;
        }

        private void LateUpdate() => Present(Time.deltaTime);

        private void Present(float deltaTime)
        {
            if (ui == null) return;
            if (!enemyBehaviour.isActiveAndEnabled || !enemy.IsAware || resetVersion != enemy.ResetVersion)
            {
                remaining = 0f;
                resetVersion = enemy.ResetVersion;
            }
            bool show = remaining > 0f && body.enabled && body.gameObject.activeInHierarchy;
            ui.Show(show);
            if (!show) return;
            // One-pixel settle instead of smooth scaling or blur; keep the glyph upright.
            float pop = remaining > duration - .12f ? Pixel : 0f;
            float top = anchorHeight > 0f ? transform.position.y + anchorHeight : body.bounds.max.y;
            ui.Place(new Vector3(transform.position.x, top + gap + pop, transform.position.z));
            remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));
        }

        private void OnDisable()
        {
            if (enemy != null) enemy.PlayerDetected -= OnDetected;
            remaining = 0f;
            if (ui != null) ui.Show(false);
        }

        private void OnDestroy() { if (ui != null) Destroy(ui.gameObject); }
    }
}
