using RoadOfTheOldKing.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.Enemies
{
    // World-space pixel "!" (UI/AwarenessMark.uxml, sprite Art/Sprites/UI/HUD/AwarenessMark.png).
    // Detection belongs to the enemy; this only presents its transition.
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(310)]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "EnemyAwarenessIndicator")]
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
        private PursuitBrain pursuit;
        private Behaviour enemyBehaviour;
        private WorldUIDocument ui;
        private VisualElement mark, track, fill;
        private float remaining;
        private int resetVersion;
        private const float Pixel = 1f / 16f;

        private void Awake()
        {
            enemy = GetComponent<IEnemy>();
            pursuit = GetComponent<PursuitBrain>();
            enemyBehaviour = enemy as Behaviour;
            if (enemy == null) { enabled = false; return; }
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) { enabled = false; return; }
            if (panel == null || layout == null) { Debug.LogError("EnemyAwarenessIndicator needs the world panel and its layout.", this); enabled = false; return; }
            ui = WorldUIDocument.Create("Enemy awareness !", transform, panel, layout, 30001, Pivot.BottomCenter);
            mark = ui.Q<VisualElement>("awareness-mark");
            track = ui.Q<VisualElement>("awareness-track");
            fill = ui.Q<VisualElement>("awareness-fill");
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
            bool watching = enemyBehaviour.isActiveAndEnabled && pursuit != null && pursuit.IsWatching;
            bool show = (watching || remaining > 0f) && body.enabled && body.gameObject.activeInHierarchy;
            ui.Show(show);
            if (!show) return;
            mark.style.display = watching ? DisplayStyle.None : DisplayStyle.Flex;
            track.style.display = watching ? DisplayStyle.Flex : DisplayStyle.None;
            if (watching)
            {
                // Eight native-pixel steps, so progress remains crisp at gameplay zoom.
                fill.style.width = Mathf.Ceil(pursuit.Suspicion * 8f) * 2f;
                fill.style.backgroundColor = (Color)(pursuit.Suspicion < .5f
                    ? new Color32(220, 220, 195, 255) : new Color32(231, 184, 92, 255));
            }
            mark.style.unityBackgroundImageTintColor = pursuit != null && pursuit.HasWatchingStage
                ? new Color(1f, .3f, .3f) : Color.white;
            // One-pixel settle instead of smooth scaling or blur; keep the glyph upright.
            float pop = !watching && remaining > duration - .12f ? Pixel : 0f;
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
