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
        private InputAction padInteract;
        private Coroutine releasing;
        public Bonfire Nearby { get; private set; }
        public Bonfire ActiveFire { get; private set; }
        public bool IsOpen => ActiveFire != null;
        // The bonfire menu presents these; this component owns interaction and its control lock.
        public event System.Action<Bonfire> Opened;
        public event System.Action Closed;
        public WorldPickup NearbyPickup { get; private set; }
        public string Prompt => IsOpen || !CanInteract ? "" : NearbyPickup != null
            ? Key + " - " + NearbyPickup.Prompt : Nearby != null
                ? Key + " - " + (Nearby.IsDiscovered ? "Rest at " + Nearby.DisplayName : "Light Bonfire") : "";
        private static string Key => RoadOfTheOldKing.UI.ControlLabels.Get("interact");
        private bool CanInteract => isActiveAndEnabled && health != null && health.IsAlive &&
            combat != null && combat.CanStartAttack && !combat.IsAttacking;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombatController>();
            locks = GetComponent<PlayerControlLocks>();
            padInteract = new InputAction("Interact", InputActionType.Button, GamepadLayout.Interact);
        }

        private void OnEnable() => padInteract.Enable();
        private void OnDestroy() => padInteract?.Dispose();

        private void Update()
        {
            RefreshNearby();
            if (health == null || !health.IsAlive)
            {
                Close();
                return;
            }
            // Escape/B are routed to the bonfire menu through MenuStack; F toggles here. The controller's A
            // also selects menu entries, so it interacts only while no menu is open and controls are free.
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (RoadOfTheOldKing.UI.DevToolsPanel.CapturesInput) return;
#endif
            var keyboard = Keyboard.current;
            bool keyPressed = keyboard != null && keyboard.fKey.wasPressedThisFrame;
            bool padPressed = padInteract.WasPressedThisFrame() && !RoadOfTheOldKing.UI.MenuStack.IsAnyOpen &&
                (locks == null || !locks.IsLocked);
            if (IsOpen && Nearby != ActiveFire)
                Close();
            else if (Time.timeScale > 0f && Application.isFocused && (keyPressed || padPressed))
                TryInteract();
        }

        private void RefreshNearby()
        {
            Nearby = null;
            NearbyPickup = null;
            if (health == null || !health.IsAlive) return;
            var session = GameSession.Instance;
            if (session != null)
            {
                // Nearest usable fire; ties keep list order. A plain loop: this runs every frame.
                float nearestFire = float.PositiveInfinity;
                foreach (var fire in session.Checkpoints.Fires)
                {
                    if (fire == null || !fire.CanUse(transform)) continue;
                    float distance = Vector2.SqrMagnitude(fire.transform.position - transform.position);
                    if (distance < nearestFire) { nearestFire = distance; Nearby = fire; }
                }
            }
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
            if (Nearby == null) return false;
            if (Nearby.IsDiscovered) return Open(Nearby);
            if (!GameSession.Instance.Checkpoints.Light(Nearby)) return false;
            RoadOfTheOldKing.UI.HudNotifications.ShowBanner("BONFIRE LIT");
            return true;
        }

        public bool Open(Bonfire fire)
        {
            var session = GameSession.Instance;
            if (!CanInteract || IsOpen || session == null || !session.Checkpoints.Rest(fire))
                return false;
            ActiveFire = fire;
            if (releasing != null) { StopCoroutine(releasing); releasing = null; }
            if (locks != null) locks.Lock(this);
            Opened?.Invoke(fire);
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            ActiveFire = null;
            // Keep the closing press (Escape/F, B/A) out of gameplay for the rest of this frame,
            // as the other menus do; B also dodges once controls return.
            if (locks != null)
            {
                if (isActiveAndEnabled) releasing = StartCoroutine(UnlockNextFrame());
                else locks.Unlock(this);
            }
            Closed?.Invoke();
        }

        private System.Collections.IEnumerator UnlockNextFrame()
        {
            yield return null;
            releasing = null;
            if (locks != null && !IsOpen) locks.Unlock(this);
        }

        private void OnDisable()
        {
            Close();
            padInteract?.Disable();
            if (releasing != null) { StopCoroutine(releasing); releasing = null; }
            if (locks != null && !IsOpen) locks.Unlock(this);
        }
    }
}
