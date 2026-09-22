using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    [DisallowMultipleComponent]
    public sealed class Bonfire : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Bonfire";
        [SerializeField] private int displayOrder;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.7f;
        public string Id => id;
        public string DisplayName => displayName;
        public int DisplayOrder => displayOrder;
        public Vector2 SpawnPosition => spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        public bool IsDiscovered => CheckpointSession.Instance != null &&
            CheckpointSession.Instance.Progress.discoveredFires.Contains(id);
        public bool CanUse(Transform player) => !string.IsNullOrEmpty(id) && player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRadius;
    }
}
