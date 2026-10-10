using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A few discrete props use their fixed ground contact for player-relative depth.
    // Animation height must not move the sorting boundary.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class WorldPropDepth : MonoBehaviour
    {
        [SerializeField] private Transform groundAnchor;
        private SpriteRenderer body, playerBody;
        private Transform player;

        private void Start()
        {
            body = GetComponent<SpriteRenderer>();
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (movement == null) { enabled = false; return; }
            player = movement.transform;
            playerBody = movement.GetComponentInChildren<SpriteRenderer>();
        }

        private void LateUpdate()
        {
            if (body == null || player == null || playerBody == null) return;
            float feet = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
            body.sortingLayerID = playerBody.sortingLayerID;
            body.sortingOrder = playerBody.sortingOrder + (player.position.y > feet ? 5 : -3);
        }
    }
}
