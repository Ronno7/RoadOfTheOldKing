using System.Collections.Generic;
using RoadOfTheOldKing.Enemies;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Controller aim assist (user, 18 Nov: controller only): a committed swing or throw bends toward the live
    // enemy nearest the aim line, within a cone and a range. Enemies only, so puzzle throws and breakables keep
    // the exact aim. Only enemies in plain view: a wall the axe couldn't pass leaves the aim alone (review, 23 Nov).
    // Without a candidate the aim is returned unchanged.
    internal static class AimAssist
    {
        private static readonly ContactFilter2D Solid = new ContactFilter2D { useTriggers = false };
        private static readonly List<RaycastHit2D> Sight = new List<RaycastHit2D>(8);

        public static Vector2 Toward(Transform owner, Vector2 aim, float range, float maxAngle)
        {
            if (owner == null || aim.sqrMagnitude < .0001f || range <= 0f || maxAngle <= 0f) return aim;
            Vector2 origin = owner.position;
            aim = aim.normalized;
            Vector2 best = aim;
            float bestScore = float.MaxValue;
            foreach (var enemy in EnemyRegistry.Active)
            {
                if (!(enemy is Component body) || body == null || enemy.IsDefeated) continue;
                var hurtbox = body.GetComponentInChildren<EnemyHurtbox>();
                if (hurtbox != null && !hurtbox.CanReceiveHits) continue;
                Vector2 point = hurtbox != null && hurtbox.TryGetComponent(out Collider2D shape)
                    ? (Vector2)shape.bounds.center : (Vector2)body.transform.position;
                Vector2 to = point - origin;
                float distance = to.magnitude;
                if (distance < .01f || distance > range) continue;
                float angle = Vector2.Angle(aim, to);
                if (angle > maxAngle) continue;
                if (!InSight(origin, point, owner, body.transform)) continue;
                // Nearest the aim line first, then nearest the hero.
                float score = angle / maxAngle + distance / range;
                if (score < bestScore) { bestScore = score; best = to / distance; }
            }
            return best;
        }

        // The axe's own rule for a wall: any solid collider but the hero's, the axe's, the enemy's and low cover it flies
        // over (World.ThrowOver).
        private static bool InSight(Vector2 origin, Vector2 point, Transform owner, Transform enemy)
        {
            Physics2D.Linecast(origin, point, Solid, Sight);
            foreach (var hit in Sight)
            {
                var c = hit.collider;
                if (c == null || c.transform.IsChildOf(owner) || c.transform.IsChildOf(enemy) ||
                    c.GetComponentInParent<RoadOfTheOldKing.Weapons.AxeWeapon>() != null ||
                    c.TryGetComponent<RoadOfTheOldKing.World.ThrowOver>(out _)) continue;
                return false;
            }
            return true;
        }
    }
}
