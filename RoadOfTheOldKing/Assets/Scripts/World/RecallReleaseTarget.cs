using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum RecallReleasePart { Receiver, Release }

    // A solid part of a RecallRelease: the receiver takes the lodged throw, the release takes its Recall.
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class RecallReleaseTarget : MonoBehaviour, IHitReceiver, IAxeLodgingReceiver, IHitEventSource
    {
        [SerializeField] private RecallRelease owner;
        [SerializeField] private RecallReleasePart part;
        public RecallReleasePart Part => part;
        public event System.Action<CombatHit> HitReceived;

        public bool ReceiveHit(CombatHit hit)
        {
            if (!isActiveAndEnabled || owner == null || part != RecallReleasePart.Release) return false;
            bool accepted = owner.ReceiveRelease(hit);
            if (accepted) HitReceived?.Invoke(hit);
            return accepted;
        }

        public void OnAxeLodged(AxeWeapon weapon)
        {
            if (isActiveAndEnabled && owner != null && part == RecallReleasePart.Receiver) owner.Lodge(weapon);
        }
    }
}
