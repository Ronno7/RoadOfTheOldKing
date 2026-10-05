using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.World;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // The one-time axe pickup (the Tutorial stump). Once a save owns the axe, the player spawns its
    // own copy on load, so an unowned world axe with this pickup hides itself instead of duplicating it.
    [DisallowMultipleComponent, RequireComponent(typeof(AxeWeapon), typeof(Collider2D))]
    public sealed class AxePickup : WorldPickup, IProgressParticipant
    {
        private AxeWeapon weapon;
        public override string Prompt => "Take axe";
        protected override float PickupDistance => 1.6f;
        private void Awake() => weapon = GetComponent<AxeWeapon>();

        public override bool CanCollect(PlayerHealth player)
        {
            if (!base.CanCollect(player) || weapon == null) return false;
            var combat = player.GetComponent<PlayerCombatController>();
            return combat != null && weapon.State == AxeState.OnGround && combat.Weapon == null;
        }

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            var combat = player.GetComponent<PlayerCombatController>();
            bool collected = combat.TryEquip(weapon);
            if (collected) GameSession.Instance?.SaveProgress();
            return collected;
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state)
        {
            if (state.hasAxe && weapon != null && weapon.Owner == null) gameObject.SetActive(false);
        }
    }
}
