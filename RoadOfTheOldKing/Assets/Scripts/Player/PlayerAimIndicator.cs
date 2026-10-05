using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Displays input aim only; attack commitment and hit detection stay with the weapon.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerCombatController))]
    [DefaultExecutionOrder(210)]
    public sealed class PlayerAimIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Stable visual center relative to the player's ground pivot.")]
        [SerializeField] private Vector2 centerOffset;
        [SerializeField, Min(0.1f)] private float orbitRadius = 0.95f;
        [SerializeField, Min(0.01f)] private float chevronLength = 0.16f;
        [SerializeField, Min(0.01f)] private float chevronHalfWidth = 0.13f;
        [SerializeField, Min(0.01f)] private float strokeWidth = 0.045f;
        [SerializeField] private Color color = new Color(0.96f, 0.89f, 0.66f, 0.9f);
        private PlayerCombatController combat;
        private LineRenderer outline;
        private LineRenderer marker;

        private void Awake()
        {
            combat = GetComponent<PlayerCombatController>();
            if (body == null)
                body = GetComponentInChildren<SpriteRenderer>();
            if (body == null)
            {
                enabled = false;
                return;
            }
            outline = CreateLine("Aim outline", new Color(0.12f, 0.14f, 0.12f, 0.85f));
            marker = CreateLine("Aim indicator", color);
        }

        private LineRenderer CreateLine(string objectName, Color tint)
        {
            var child = new GameObject(objectName);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = body.sharedMaterial;
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.startColor = line.endColor = tint;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        private void LateUpdate()
        {
            bool visible = combat.CanContinueAction && body != null && body.enabled;
            outline.enabled = marker.enabled = visible;
            if (!visible) return;

            // Follow combat direction around a stable visual center, without
            // cardinal snapping, smoothing lag or sprite-animation bobbing.
            Vector3 direction = combat.AimDirection;
            Vector3 side = new Vector3(-direction.y, direction.x, 0f);
            Vector3 tip = transform.position + (Vector3)centerOffset + direction * orbitRadius;
            Vector3 rear = tip - direction * chevronLength;
            Draw(outline, rear + side * chevronHalfWidth, tip,
                rear - side * chevronHalfWidth, strokeWidth + 0.04f, 3);
            Draw(marker, rear + side * chevronHalfWidth, tip,
                rear - side * chevronHalfWidth, strokeWidth, 4);
        }

        private void Draw(LineRenderer line, Vector3 a, Vector3 b, Vector3 c, float width, int order)
        {
            line.startWidth = line.endWidth = width;
            line.sortingLayerID = body.sortingLayerID;
            line.sortingOrder = body.sortingOrder + order;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.SetPosition(2, c);
        }

        private void OnDisable()
        {
            if (outline != null) outline.enabled = false;
            if (marker != null) marker.enabled = false;
        }

        private void OnDestroy()
        {
            if (outline != null) Destroy(outline.gameObject);
            if (marker != null) Destroy(marker.gameObject);
        }
    }
}
