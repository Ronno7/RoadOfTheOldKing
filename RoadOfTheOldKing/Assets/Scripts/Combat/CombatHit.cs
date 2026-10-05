using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    public enum AttackKind { LightChop, ChargedCleave, Throw, Recall, EnemyMelee }

    public readonly struct CombatHit
    {
        public readonly GameObject Source;
        public readonly AttackKind Kind;
        public readonly int Damage;
        public readonly Vector2 Direction;
        public readonly float Knockback;
        public readonly float StaggerDuration;
        public readonly bool BreaksGuard;
        public readonly Vector2? ImpactPoint;

        public CombatHit(GameObject source, AttackKind kind, int damage, Vector2 direction,
            float knockback, float staggerDuration, bool breaksGuard = false, Vector2? impactPoint = null)
        {
            Source = source;
            Kind = kind;
            Damage = damage;
            Direction = direction.normalized;
            Knockback = knockback;
            StaggerDuration = staggerDuration;
            BreaksGuard = breaksGuard;
            ImpactPoint = impactPoint;
        }
    }

    public interface IHitReceiver { bool ReceiveHit(CombatHit hit); }
    // Accepted hits only; presentation subscribes without depending on health.
    public interface IHitEventSource { event System.Action<CombatHit> HitReceived; }
    public interface IHitProtection { bool Blocks(CombatHit hit); }
}
