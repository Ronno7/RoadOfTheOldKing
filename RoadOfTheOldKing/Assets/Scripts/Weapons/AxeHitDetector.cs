using System.Collections.Generic;
using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    // Physics queries and hit deduplication are independent of weapon state and visuals.
    public sealed class AxeHitDetector
    {
        private readonly Transform owner;
        private readonly Transform weapon;
        private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        private readonly List<Collider2D> overlaps = new List<Collider2D>(16);
        private readonly List<RaycastHit2D> casts = new List<RaycastHit2D>(16);
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(8);
        private readonly HashSet<IHitReceiver> hitTargets = new HashSet<IHitReceiver>();
        private readonly System.Action<CombatHit> confirmedHit;

        public AxeHitDetector(Transform owner, Transform weapon, System.Action<CombatHit> confirmedHit = null)
        {
            this.owner = owner;
            this.weapon = weapon;
            this.confirmedHit = confirmedHit;
        }

        public void BeginAttack() => hitTargets.Clear();

        // Counts a target as already hit for this attack (e.g. the one a recalled axe is pulled out of).
        public void Exclude(IHitReceiver receiver) { if (receiver != null) hitTargets.Add(receiver); }

        // The aim guide uses the same swept radius and exclusions as flight, without hits.
        public float PreviewFlightDistance(Vector2 origin, Vector2 direction, float distance, float radius)
        {
            Physics2D.CircleCast(origin, radius, direction, filter, casts, distance);
            foreach (var cast in casts)
                if (IsCandidate(cast.collider)) distance = Mathf.Min(distance, cast.distance);
            return distance;
        }

        private bool IsCandidate(Collider2D collider) => collider != null &&
            !collider.transform.IsChildOf(owner) && !collider.transform.IsChildOf(weapon);

        private bool BlocksMelee(Collider2D collider) => IsCandidate(collider) &&
            collider.GetComponentInParent<IHitReceiver>() == null;

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

        public void Melee(Vector2 center, Vector2 aim, float radius, float arc, CombatHit hit, float laneWidth = 0f)
        {
            if (laneWidth > 0f)
                Physics2D.OverlapBox(center + aim * (radius * 0.5f), new Vector2(radius, laneWidth),
                    Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg, filter, overlaps);
            else
                Physics2D.OverlapCircle(center, radius, filter, overlaps);
            foreach (var collider in overlaps)
            {
                if (!IsCandidate(collider))
                    continue;
                Vector2 point = collider.ClosestPoint(center);
                Vector2 direction = point - center;
                if (laneWidth <= 0f && arc < 360f && direction.sqrMagnitude > 0.001f && Vector2.Angle(aim, direction) > arc * 0.5f)
                    continue;

                bool obstructed = false;
                Physics2D.Linecast(center, point, filter, sight);
                foreach (var blocker in sight)
                    if (blocker.collider != collider && BlocksMelee(blocker.collider))
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
