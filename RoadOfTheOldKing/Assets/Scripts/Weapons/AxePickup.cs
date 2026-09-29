using TheLostShrine.Player;
using TheLostShrine.World;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    [DisallowMultipleComponent, RequireComponent(typeof(AxeWeapon), typeof(Collider2D))]
    public sealed class AxePickup : WorldPickup
    {
        private AxeWeapon weapon;
        public override string Prompt => "Pick up axe";
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
            if (collected) CheckpointSession.Instance?.SaveProgress();
            return collected;
        }
    }
}
