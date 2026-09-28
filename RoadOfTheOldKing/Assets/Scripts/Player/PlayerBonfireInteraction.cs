using System.Linq;
using TheLostShrine.Input;
using TheLostShrine.Progression;
using TheLostShrine.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerBonfireInteraction : MonoBehaviour
    {
        private PlayerHealth health;
        private PlayerCombatController combat;
        private PlayerMovementInput movementInput;
        private PlayerCombatInput combatInput;
        public Bonfire Nearby { get; private set; }
        public Bonfire ActiveFire { get; private set; }
        public bool IsOpen => ActiveFire != null;
        public WorldPickup NearbyPickup { get; private set; }
        public string Prompt => IsOpen || !CanInteract ? "" : NearbyPickup != null
            ? "F - " + NearbyPickup.Prompt : Nearby != null ? "F - Rest at " + Nearby.DisplayName : "";
        private bool CanInteract => isActiveAndEnabled && health != null && health.IsAlive &&
            combat != null && combat.CanStartAttack && !combat.IsAttacking;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombatController>();
            movementInput = GetComponent<PlayerMovementInput>();
            combatInput = GetComponent<PlayerCombatInput>();
        }

        private void Update()
        {
            RefreshNearby();
            if (health == null || !health.IsAlive)
            {
                Close();
                return;
            }
            var keyboard = Keyboard.current;
            if (IsOpen && (Nearby != ActiveFire || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)))
                Close();
            else if (Time.timeScale > 0f && Application.isFocused && keyboard != null && keyboard.fKey.wasPressedThisFrame)
                TryInteract();
        }

        private void RefreshNearby()
        {
            Nearby = null;
            NearbyPickup = null;
            if (health == null || !health.IsAlive) return;
            var session = CheckpointSession.Instance;
            if (session != null)
                Nearby = session.Fires.Where(f => f != null && f.CanUse(transform))
                    .OrderBy(f => Vector2.SqrMagnitude(f.transform.position - transform.position)).FirstOrDefault();
            if (IsOpen || !CanInteract) return;
            float nearest = float.PositiveInfinity;
            foreach (var pickup in WorldPickup.Active)
            {
                if (pickup == null || !pickup.CanCollect(health)) continue;
                float distance = (pickup.transform.position - transform.position).sqrMagnitude;
                if (distance < nearest || distance == nearest && NearbyPickup != null &&
                    pickup.GetInstanceID() < NearbyPickup.GetInstanceID())
                {
                    nearest = distance;
                    NearbyPickup = pickup;
                }
            }
        }

        // One key press commits one interaction. No trigger collection or held-key repeats.
        public bool TryInteract()
        {
            if (Time.timeScale <= 0f || health == null || !health.IsAlive) return false;
            if (IsOpen) { Close(); return true; }
            if (!CanInteract) return false;
            RefreshNearby();
            if (NearbyPickup != null)
            {
                bool collected = NearbyPickup.TryCollect(health);
                RefreshNearby();
                return collected;
            }
            return Nearby != null && Open(Nearby);
        }

        public bool Open(Bonfire fire)
        {
            var session = CheckpointSession.Instance;
            if (session == null || !session.Rest(fire))
                return false;
            ActiveFire = fire;
            if (movementInput != null) movementInput.enabled = false;
            if (combatInput != null) combatInput.enabled = false;
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            ActiveFire = null;
            if (health != null && health.IsAlive)
            {
                if (movementInput != null) movementInput.enabled = true;
                if (combatInput != null) combatInput.enabled = true;
            }
        }

        private void OnDisable() => Close();
    }
}
