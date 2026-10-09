using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    [DisallowMultipleComponent]
    public sealed class Breakable : MonoBehaviour, IHitReceiver
    {
        [SerializeField] private bool requiresFullCleave;
        public event System.Action<CombatHit> Broken;
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
            Broken?.Invoke(hit);
            gameObject.SetActive(false);
            return true;
        }
    }
}
