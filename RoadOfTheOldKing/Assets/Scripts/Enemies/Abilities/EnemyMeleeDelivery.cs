using RoadOfTheOldKing.Combat;
using System;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Contact geometry only. Actions own timing and consume one contact even when protection rejects damage.
    internal static class EnemyMeleeDelivery
    {
        internal static bool Bite(GameObject source, PlayerHealth target, Vector2 origin, Vector2 facing,
            float radius, int damage, float knockback, float stagger, Func<Vector2, bool> clear)
        {
            var shape = target != null ? target.GetComponent<Collider2D>() : null;
            if (shape == null || !shape.enabled) return false;
            Vector2 point = shape.ClosestPoint(origin);
            if (Vector2.Distance(point, origin) > radius || !clear(point)) return false;
            target.Health.ReceiveHit(new CombatHit(source, AttackKind.EnemyMelee, damage, facing, knockback, stagger, false, point));
            return true;
        }

        internal static bool Arc(GameObject source, PlayerHealth target, Vector2 origin, Vector2 facing,
            float reach, float arc, int damage, float knockback, float stagger, Func<Vector2, bool> clear)
        {
            var collider = target != null ? target.GetComponent<Collider2D>() : null;
            if (collider == null || !collider.enabled) return false;
            Vector2 point = collider.ClosestPoint(origin), direction = point - origin;
            if (direction.magnitude > reach ||
                (direction.sqrMagnitude > .001f && Vector2.Angle(facing, direction) > arc * .5f) || !clear(point)) return false;
            target.Health.ReceiveHit(new CombatHit(source, AttackKind.EnemyMelee, damage, facing,
                knockback, stagger, false, point));
            // A defended/dodged contact still consumes the attack's one attempt.
            return true;
        }
    }
}
