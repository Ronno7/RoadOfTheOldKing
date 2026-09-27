using System;
using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour, IHitReceiver
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField] private MonoBehaviour protectionSource;
        private IHitProtection protection;
        private int maxHealthBonus;

        public int Health { get; private set; }
        public int MaxHealth => maxHealth + maxHealthBonus;
        public bool IsAlive => Health > 0;
        public event Action<CombatHit> HitReceived;
        public event Action Defeated;

        private void Awake()
        {
            protection = protectionSource as IHitProtection ?? GetComponent<IHitProtection>();
            RestoreHealth();
        }

        public bool ReceiveHit(CombatHit hit)
        {
            if (!IsAlive || hit.Damage <= 0 || (protection != null && protection.Blocks(hit)))
                return false;

            Health = Mathf.Max(0, Health - hit.Damage);
            HitReceived?.Invoke(hit);
            if (!IsAlive)
                Defeated?.Invoke();
            return true;
        }

        // Absolute bonus makes restoration idempotent. Increasing capacity also fills the new HP.
        public void SetMaxHealthBonus(int bonus)
        {
            int previousMaximum = MaxHealth;
            maxHealthBonus = Mathf.Max(0, bonus);
            if (IsAlive)
                Health = Mathf.Clamp(Health + MaxHealth - previousMaximum, 1, MaxHealth);
        }

        public void RestoreHealth() => Health = MaxHealth;
    }
}
