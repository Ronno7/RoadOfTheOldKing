using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent]
    public sealed class Breakable : MonoBehaviour, IHitReceiver
    {
        [SerializeField] private bool requiresFullCleave;
        public bool IsBroken { get; private set; }

        public void RestoreBrokenState()
        {
            IsBroken = true;
            gameObject.SetActive(false);
        }

        public bool ReceiveHit(CombatHit hit)
        {
            if (IsBroken || (requiresFullCleave && !hit.BreaksGuard))
                return false;
            IsBroken = true;
            gameObject.SetActive(false);
            return true;
        }
    }
}
