using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour, IStamina
    {
        [SerializeField, Min(1f)] private float maximum = 100f;
        [SerializeField, Min(0.1f)] private float recoveryPerSecond = 25f;
        [SerializeField, Min(0f)] private float recoveryDelay = 0.8f;
        private PlayerHealth health;
        private float recoveryRemaining;
        private float rejectedUntil;

        public float Current { get; private set; }
        public float Maximum => maximum;
        public float Normalized => Current / maximum;
        public bool WasSpendRejected => Time.unscaledTime < rejectedUntil;

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

        public bool TrySpend(float amount)
        {
            if (!isActiveAndEnabled || (health != null && !health.IsAlive) ||
                amount < 0f || float.IsNaN(amount) || float.IsInfinity(amount))
                return false;
            // Continuous sprint spending accumulates small floating-point errors.
            if (Current + 0.001f < amount)
            {
                rejectedUntil = Time.unscaledTime + 0.35f;
                return false;
            }
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
