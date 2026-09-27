using UnityEngine;

namespace TheLostShrine.Player
{
    // A tiny marker makes facing visible until directional character art exists.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerMovement))]
    public sealed class PlayerFacing : MonoBehaviour
    {
        [SerializeField] private Transform marker;
        [SerializeField, Min(0f)] private float markerDistance = 0.24f;
        private PlayerMovement movement;

        // Visual facing stays cardinal; movement retains its full direction for gameplay.
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Awake() => movement = GetComponent<PlayerMovement>();

        private void LateUpdate()
        {
            Vector2 direction = movement.FacingDirection;
            // Prefer horizontal facing on exact diagonals to support four-direction art.
            FacingDirection = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? (direction.x > 0f ? Vector2.right : Vector2.left)
                : (direction.y > 0f ? Vector2.up : Vector2.down);

            if (marker == null)
                return;

            marker.localPosition = new Vector3(
                FacingDirection.x * markerDistance, FacingDirection.y * markerDistance, 0f);
        }
    }
}
