using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Souls-style stamina: any action can start while stamina is above zero, and its cost may push
    // stamina into a deficit (which lengthens the wait and weakens that attack). Recovery begins after
    // a short delay and pauses during attacks, dodges and sprinting (each of them delays it).
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour, IStamina
    {
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField, Min(0.1f)] private float recoveryPerSecond = 40f;
        [SerializeField, Min(0f)] private float recoveryDelay = 0.6f;
        [Tooltip("How far below zero an over-budget action can push stamina.")]
        [SerializeField, Min(0f)] private float deficitLimit = 40f;
        private PlayerHealth health;
        private float recoveryRemaining;
        private float rejectedUntil;

        public float Current { get; private set; }
        public float Maximum => maximum;
        public float DeficitLimit => deficitLimit;
        // Clamped to 0..1 for meters; Current itself can be negative.
        public float Normalized => Mathf.Clamp01(Current / maximum);
        public bool IsExhausted => Current < 0f;
        public bool WasSpendRejected => Time.unscaledTime < rejectedUntil;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
        public bool DebugInfinite { get; set; }
        public void DebugSetCurrent(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return;
            Current = Mathf.Clamp(value, -deficitLimit, maximum);
            DelayRecovery();
        }
#endif

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
            Restore();
        }

        private void Update() => Tick(Time.deltaTime);

        private void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || (health != null && !health.IsAlive))
                return;
            float recoveringTime = Mathf.Max(0f, deltaTime - recoveryRemaining);
            recoveryRemaining = Mathf.Max(0f, recoveryRemaining - deltaTime);
            Current = Mathf.Min(maximum, Current + recoveryPerSecond * recoveringTime);
        }

        private bool CanAct(float amount) => isActiveAndEnabled && (health == null || health.IsAlive) &&
            amount >= 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);

        public bool TrySpend(float amount)
        {
            if (!CanAct(amount)) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (DebugInfinite) { Restore(); return true; }
#endif
            // Any positive stamina is enough to act; continuous sprinting leaves tiny float remainders.
            if (Current <= 0.001f)
            {
                rejectedUntil = Time.unscaledTime + 0.35f;
                return false;
            }
            if (amount > 0f)
            {
                Current = Mathf.Max(-deficitLimit, Current - amount);
                DelayRecovery();
            }
            return true;
        }

        public bool TryDrain(float amount)
        {
            if (!CanAct(amount)) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (DebugInfinite) { Restore(); return true; }
#endif
            if (Current <= 0.001f) return false;
            if (amount > 0f)
            {
                Current = Mathf.Max(0f, Current - amount);
                DelayRecovery();
            }
            return true;
        }

        public void DelayRecovery() => recoveryRemaining = recoveryDelay;

        // Stamina is transient: every spawn and bonfire rest starts full.
        public void Restore()
        {
            Current = maximum;
            recoveryRemaining = 0f;
            rejectedUntil = 0f;
        }
    }
}
