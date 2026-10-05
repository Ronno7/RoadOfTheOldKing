using System;
using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    // Indestructible practice stand: accepts the player's weapon hits for feedback and lessons.
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class ThrowPracticeStand : MonoBehaviour, IHitReceiver, IHitEventSource
    {
        public event Action<CombatHit> HitReceived;

        public bool ReceiveHit(CombatHit hit)
        {
            if (hit.Kind == AttackKind.EnemyMelee || hit.Source == null)
                return false;
            HitReceived?.Invoke(hit);
            return true;
        }
    }
}
