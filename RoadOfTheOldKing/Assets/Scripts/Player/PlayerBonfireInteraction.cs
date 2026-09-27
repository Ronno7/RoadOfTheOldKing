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
        private PlayerMovementInput movementInput;
        private PlayerCombatInput combatInput;
        public Bonfire Nearby { get; private set; }
        public Bonfire ActiveFire { get; private set; }
        public bool IsOpen => ActiveFire != null;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            movementInput = GetComponent<PlayerMovementInput>();
            combatInput = GetComponent<PlayerCombatInput>();
        }

        private void Update()
        {
            var session = CheckpointSession.Instance;
            if (session == null)
                return;
            if (!health.IsAlive)
            {
                Close();
                Nearby = null;
                return;
            }
            Nearby = session.Fires.Where(f => f != null && f.CanUse(transform))
                .OrderBy(f => Vector2.SqrMagnitude(f.transform.position - transform.position)).FirstOrDefault();
            var keyboard = Keyboard.current;
            if (IsOpen && (Nearby != ActiveFire || (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)))
                Close();
            else if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                if (IsOpen) Close();
                else if (Nearby != null) Open(Nearby);
            }
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
