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

        private void Awake() => movement = GetComponent<PlayerMovement>();

        private void LateUpdate()
        {
            if (marker == null)
                return;

            Vector2 direction = movement.FacingDirection;
            marker.localPosition = new Vector3(
                direction.x * markerDistance, direction.y * markerDistance, 0f);
        }
    }
}
