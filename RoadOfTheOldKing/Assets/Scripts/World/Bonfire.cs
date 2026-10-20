using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    [DisallowMultipleComponent]
    public sealed class Bonfire : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Bonfire";
        [SerializeField] private int displayOrder;
        [SerializeField] private bool allowsUpgrades;
        public bool AllowsUpgrades => allowsUpgrades;
        [Tooltip("Regional bonfire menu skin: BonfireMenu puts skin-<name> on its panel (UI/BonfireMenu.uss). Same layout everywhere; only the frame changes.")]
        [SerializeField] private string menuSkin = "hearth";
        public string MenuSkin => string.IsNullOrEmpty(menuSkin) ? "hearth" : menuSkin;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.7f;
        public string Id => id;
        public string DisplayName => displayName;
        public int DisplayOrder => displayOrder;
        public Vector2 SpawnPosition => spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        public bool IsDiscovered => GameSession.Instance != null && GameSession.Instance.Progress.HasFire(id);
        public bool CanUse(Transform player) => !string.IsNullOrEmpty(id) && player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRadius;
    }
}
