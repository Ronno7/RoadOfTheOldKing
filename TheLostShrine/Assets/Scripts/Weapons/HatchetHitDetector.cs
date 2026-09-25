using System.Collections.Generic;
using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    // Physics queries and hit deduplication are independent of weapon state and visuals.
    public sealed class HatchetHitDetector
    {
        private readonly Transform owner;
        private readonly Transform weapon;
        private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private readonly List<RaycastHit2D> casts = new List<RaycastHit2D>(16);
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(8);
        private readonly HashSet<IHitReceiver> hitTargets = new HashSet<IHitReceiver>();
        private readonly System.Action<CombatHit> confirmedHit;

        public HatchetHitDetector(Transform owner, Transform weapon, System.Action<CombatHit> confirmedHit = null)
        {
            this.owner = owner;
            this.weapon = weapon;
            this.confirmedHit = confirmedHit;
        }

        public void BeginAttack() => hitTargets.Clear();

        private bool IsCandidate(Collider2D collider) => collider != null &&
            !collider.transform.IsChildOf(owner) && !collider.transform.IsChildOf(weapon);

        private void Apply(Collider2D collider, CombatHit hit, Vector2 impactPoint)
        {
            var receiver = collider.GetComponentInParent<IHitReceiver>();
            if (receiver != null && hitTargets.Add(receiver))
            {
                var resolved = new CombatHit(hit.Source, hit.Kind, hit.Damage, hit.Direction,
                    hit.Knockback, hit.StaggerDuration, hit.BreaksGuard, impactPoint);
                if (receiver.ReceiveHit(resolved)) confirmedHit?.Invoke(resolved);
            }
        }

        public void Melee(Vector2 center, Vector2 aim, float radius, float arc, CombatHit hit)
        {
            Physics2D.OverlapCircle(center, radius, filter, overlaps);
            foreach (var collider in overlaps)
            {
                if (!IsCandidate(collider))
                    continue;
                Vector2 point = collider.ClosestPoint(center);
                Vector2 direction = point - center;
                if (arc < 360f && direction.sqrMagnitude > 0.001f && Vector2.Angle(aim, direction) > arc * 0.5f)
                    continue;

                bool obstructed = false;
                Physics2D.Linecast(center, point, filter, sight);
                foreach (var blocker in sight)
                    if (IsCandidate(blocker.collider) && blocker.collider != collider &&
                        blocker.collider.GetComponentInParent<IHitReceiver>() == null)
                    {
                        obstructed = true;
                        break;
                    }
                if (!obstructed)
                    Apply(collider, new CombatHit(hit.Source, hit.Kind, hit.Damage,
                        direction.sqrMagnitude > 0.001f ? direction : aim,
                        hit.Knockback, hit.StaggerDuration, hit.BreaksGuard), point);
            }
        }

        public bool Flight(Vector2 origin, Vector2 destination, float radius, CombatHit hit,
            bool stopAtImpact, out RaycastHit2D impact)
        {
            impact = default;
            Vector2 delta = destination - origin;
            Physics2D.CircleCast(origin, radius, delta.normalized, filter, casts, delta.magnitude);
            casts.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (var cast in casts)
            {
                if (!IsCandidate(cast.collider))
                    continue;
                Apply(cast.collider, hit, cast.point);
                if (stopAtImpact)
                {
                    impact = cast;
                    return true;
                }
            }
            return false;
        }
    }
}
