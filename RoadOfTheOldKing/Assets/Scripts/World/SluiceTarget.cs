using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum SluicePart { DistributorFork, MainFork, DistributorVane, BrakeVane, CatchVane }

    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class SluiceTarget : MonoBehaviour, IHitReceiver, IAxeLodgingReceiver, IHitEventSource
    {
        [SerializeField] private RecallSluice sluice;
        [SerializeField] private SluicePart part;
        public SluicePart Part => part;
        public event System.Action<CombatHit> HitReceived;

        public bool ReceiveHit(CombatHit hit)
        {
            if (!isActiveAndEnabled || sluice == null) return false;
            bool accepted = sluice.ReceiveVane(part, hit);
            if (accepted) HitReceived?.Invoke(hit);
            return accepted;
        }

        public void OnAxeLodged(AxeWeapon weapon)
        {
            if (isActiveAndEnabled && sluice != null &&
                (part == SluicePart.DistributorFork || part == SluicePart.MainFork))
                sluice.Lodge(part, weapon);
        }
    }
}
