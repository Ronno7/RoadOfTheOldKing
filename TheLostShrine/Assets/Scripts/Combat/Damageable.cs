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

        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
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

        public void RestoreHealth() => Health = maxHealth;
    }
}
