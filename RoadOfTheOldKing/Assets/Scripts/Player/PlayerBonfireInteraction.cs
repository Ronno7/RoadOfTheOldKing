using System.Linq;
using RoadOfTheOldKing.Input;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerBonfireInteraction : MonoBehaviour
    {
        private PlayerHealth health;
        private PlayerCombatController combat;
        private PlayerControlLocks locks;
        public Bonfire Nearby { get; private set; }
        public Bonfire ActiveFire { get; private set; }
        public bool IsOpen => ActiveFire != null;
        // The bonfire menu presents these; this component only owns resting and the control lock.
        public event System.Action<Bonfire> Opened;
        public event System.Action Closed;
        public WorldPickup NearbyPickup { get; private set; }
        public string Prompt => IsOpen || !CanInteract ? "" : NearbyPickup != null
            ? "F - " + NearbyPickup.Prompt : Nearby != null ? "F - Rest at " + Nearby.DisplayName : "";
        private bool CanInteract => isActiveAndEnabled && health != null && health.IsAlive &&
            combat != null && combat.CanStartAttack && !combat.IsAttacking;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombatController>();
            locks = GetComponent<PlayerControlLocks>();
        }

        private void Update()
        {
            RefreshNearby();
            if (health == null || !health.IsAlive)
            {
                Close();
                return;
            }
            // Escape is routed to the bonfire menu through MenuStack; F toggles here.
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (RoadOfTheOldKing.UI.DevToolsPanel.CapturesInput) return;
#endif
            var keyboard = Keyboard.current;
            if (IsOpen && Nearby != ActiveFire)
                Close();
            else if (Time.timeScale > 0f && Application.isFocused && keyboard != null && keyboard.fKey.wasPressedThisFrame)
                TryInteract();
        }

        private void RefreshNearby()
        {
            Nearby = null;
            NearbyPickup = null;
            if (health == null || !health.IsAlive) return;
            var session = GameSession.Instance;
            if (session != null)
                Nearby = session.Checkpoints.Fires.Where(f => f != null && f.CanUse(transform))
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
            var session = GameSession.Instance;
            if (session == null || !session.Checkpoints.Rest(fire))
                return false;
            ActiveFire = fire;
            if (locks != null) locks.Lock(this);
            Opened?.Invoke(fire);
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            ActiveFire = null;
            if (locks != null) locks.Unlock(this);
            Closed?.Invoke();
        }

        private void OnDisable() => Close();
    }
}
