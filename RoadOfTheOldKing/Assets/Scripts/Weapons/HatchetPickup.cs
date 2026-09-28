using TheLostShrine.Player;
using TheLostShrine.World;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    [DisallowMultipleComponent, RequireComponent(typeof(HatchetWeapon), typeof(Collider2D))]
    public sealed class HatchetPickup : WorldPickup
    {
        private HatchetWeapon weapon;
        public override string Prompt => "Pick up hatchet";
        protected override float PickupDistance => 1.1f;
        private void Awake() => weapon = GetComponent<HatchetWeapon>();

        public override bool CanCollect(PlayerHealth player)
        {
            if (!base.CanCollect(player) || weapon == null) return false;
            var combat = player.GetComponent<PlayerCombatController>();
            return combat != null && weapon.State == HatchetState.OnGround && combat.Weapon == null;
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
