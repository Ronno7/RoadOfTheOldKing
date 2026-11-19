using RoadOfTheOldKing.Enemies;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Controller aim assist (user, 18 Nov: controller only): a committed swing or throw bends toward the live
    // enemy nearest the aim line, within a cone and a range. Enemies only, so puzzle throws and breakables keep
    // the exact aim. Without a candidate the aim is returned unchanged.
    internal static class AimAssist
    {
        public static Vector2 Toward(Vector2 origin, Vector2 aim, float range, float maxAngle)
        {
            if (aim.sqrMagnitude < .0001f || range <= 0f || maxAngle <= 0f) return aim;
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
                // Nearest the aim line first, then nearest the hero.
                float score = angle / maxAngle + distance / range;
                if (score < bestScore) { bestScore = score; best = to / distance; }
            }
            return best;
        }
    }
}
