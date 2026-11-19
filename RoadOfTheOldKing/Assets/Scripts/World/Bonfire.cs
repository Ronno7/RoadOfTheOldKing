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
        // Travel groups fires by the region they stand in.
        public string Region => AreaName.RegionAt(transform.position);
        public Vector2 SpawnPosition => spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        public bool IsDiscovered => GameSession.Instance != null && GameSession.Instance.Progress.HasFire(id);
        // The moment the player lights it (never on load or travel): BonfireSpriteView plays the ignition.
        public event System.Action Lit;
        internal void NotifyLit() => Lit?.Invoke();
        public bool CanUse(Transform player) => !string.IsNullOrEmpty(id) && player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRadius;

#if UNITY_EDITOR
        // Ticking or unticking upgrades swaps the saved base between the stone ring and the bronze brazier.
        private void OnValidate() { if (TryGetComponent(out BonfireSpriteView view)) view.SyncSavedBase(); }
#endif
    }
}
