using System;
using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent]
    public sealed class ShieldProtection : MonoBehaviour, IHitProtection
    {
        [SerializeField] private GameObject shieldVisual;
        public bool IsIntact { get; private set; } = true;
        public event Action Blocked;
        public event Action Broken;

        public bool Blocks(CombatHit hit)
        {
            if (!IsIntact)
                return false;
            if (hit.BreaksGuard)
            {
                IsIntact = false;
                if (shieldVisual != null)
                    shieldVisual.SetActive(false);
                Broken?.Invoke();
                return false;
            }
            Blocked?.Invoke();
            return true;
        }

        public void Restore()
        {
            IsIntact = true;
            if (shieldVisual != null)
                shieldVisual.SetActive(true);
        }
    }
}
