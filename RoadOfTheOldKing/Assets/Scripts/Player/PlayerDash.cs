using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Player
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

        public Vector2 GetVelocity(float deltaTime) => IsDashing && deltaTime > 0f
            ? Direction * (distance / duration) * Mathf.Min(1f, remaining / deltaTime)
            : Vector2.zero;

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
