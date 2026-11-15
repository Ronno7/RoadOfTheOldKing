using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Owns dash timing and cost; PlayerMovement remains the only movement motor.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerStamina))]
    public sealed class PlayerDash : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float distance = 2.2f;
        [SerializeField, Min(0.02f)] private float duration = 0.18f;
        [SerializeField, Min(0f)] private float staminaCost = 25f;
        [Tooltip("Recovery time after a complete dash before another can start.")]
        [SerializeField, Min(0f)] private float cooldown = 0.35f;
        [Tooltip("Protection at the start of a dash, shorter than the full movement.")]
        [SerializeField, Min(0f)] private float dodgeWindow = 0.1f;
        [Tooltip("How front-loaded the dodge is. 0 is one steady speed; higher bursts out faster and eases off toward the exit speed. Distance and duration stay exact.")]
        [SerializeField, Min(0f)] private float easeOut = 2f;
        [Tooltip("Units/s the dodge eases down to as it ends. Walking speed hands straight back to movement.")]
        [SerializeField, Min(0f)] private float exitSpeed = 4.5f;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private PlayerHealth health;
        private HitReaction reaction;
        private PlayerFlask flask;
        private float remaining;
        private float cooldownRemaining;

        public bool IsDashing => isActiveAndEnabled && remaining > 0f;
        public bool HasDodgeProtection => IsDashing && duration - remaining < dodgeWindow;
        public Vector2 Direction { get; private set; }
        public float CooldownRemaining => cooldownRemaining;
        // Read-only presentation samples; animation cannot extend movement or dodge protection.
        public float Progress => Mathf.Clamp01(1f - remaining / duration);
        public float RecoverySeconds { get; private set; } = float.PositiveInfinity;
        public uint Sequence { get; private set; }

        private void Awake()
        {
            stamina = GetComponent<PlayerStamina>();
            combat = GetComponent<PlayerCombatController>();
            health = GetComponent<PlayerHealth>();
            reaction = GetComponent<HitReaction>();
            flask = GetComponent<PlayerFlask>();
        }

        public bool TryStart(Vector2 direction)
        {
            if (!isActiveAndEnabled || IsDashing || cooldownRemaining > 0f ||
                direction.sqrMagnitude < 0.001f || (health != null && !health.IsAlive) ||
                (reaction != null && reaction.IsStaggered) ||
                (combat != null && combat.IsAttacking && !combat.CanCancelThrowAim) || (flask != null && flask.IsDrinking))
                return false;
            if (!stamina.TrySpend(staminaCost))
                return false;
            // A rejected dash leaves aim intact. An accepted dash consumes the E cycle.
            if (combat != null) combat.CancelThrowAim();
            Direction = direction.normalized;
            remaining = duration;
            RecoverySeconds = float.PositiveInfinity;
            Sequence++;
            cooldownRemaining = duration + cooldown;
            return true;
        }

        // Called exactly once by the motor at the start of each physics step.
        public void Tick(float deltaTime)
        {
            bool finishing = remaining > 0f;
            float overshoot = Mathf.Max(0f, deltaTime - remaining);
            remaining = Mathf.Max(0f, remaining - deltaTime);
            if (finishing && remaining == 0f) RecoverySeconds = overshoot;
            else if (!finishing && !float.IsPositiveInfinity(RecoverySeconds)) RecoverySeconds += deltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - deltaTime);
        }

        // This step's share of the curve, so the dodge lands on its distance at any physics rate.
        public Vector2 GetVelocity(float deltaTime)
        {
            if (!IsDashing || deltaTime <= 0f) return Vector2.zero;
            float from = duration - remaining, to = Mathf.Min(duration, from + deltaTime);
            return Direction * ((Covered(to) - Covered(from)) / deltaTime);
        }

        // Distance covered t seconds in. Speed falls from a burst to the exit speed as (1 - t/duration)^easeOut,
        // with the burst sized so the whole dodge covers exactly `distance`.
        private float Covered(float t)
        {
            float average = distance / duration, end = Mathf.Min(exitSpeed, average);
            float u = Mathf.Clamp01(t / duration);
            return end * u * duration + (average - end) * duration * (1f - Mathf.Pow(1f - u, easeOut + 1f));
        }

        public void Cancel()
        {
            remaining = 0f;
            RecoverySeconds = float.PositiveInfinity;
        }

        public void ResetAtRest()
        {
            Cancel();
            cooldownRemaining = 0f;
        }

        private void OnDisable() => Cancel();
        private void OnValidate() => dodgeWindow = Mathf.Clamp(dodgeWindow, 0f, duration);
    }
}
