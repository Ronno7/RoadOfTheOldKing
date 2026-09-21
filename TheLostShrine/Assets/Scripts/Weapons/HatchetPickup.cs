using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    [DisallowMultipleComponent, RequireComponent(typeof(HatchetWeapon), typeof(Collider2D))]
    public sealed class HatchetPickup : MonoBehaviour
    {
        private HatchetWeapon weapon;
        private void Awake() => weapon = GetComponent<HatchetWeapon>();
        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        private void TryCollect(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerCombatController>();
            if (player != null)
                player.TryEquip(weapon);
        }
    }
}
