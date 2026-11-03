using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Instance-owned physics/health services shared by decision policies; no autonomous Update loop.
    internal sealed class EnemyBody
    {
        public Damageable Health { get; }
        public HitReaction Reaction { get; }
        public Rigidbody2D Rigidbody { get; }
        public Collider2D Collider { get; }
        public Vector2 Home { get; private set; }
        // Followed back inside this share of the leash, a returning enemy takes the player up again
        // (well inside the edge, so it doesn't flicker there).
        private const float ReengageFraction = .6f;
        // Walking home, it turns on a player this close wherever it is.
        private const float TurnOnPlayerDistance = 3f;
        // This many leashes out it gives up even beside the player, so it can't be led across the map.
        private const float HardLeashFactor = 1.5f;
        // A hit keeps it on the player this long, whatever the leash or sight say; each hit restarts it.
        private const float ProvokedChaseSeconds = 5f;
        // A walk home that covers almost no ground in this long has met a wall.
        private const float StuckSeconds = 2f;
        private const float StuckDistance = .25f;
        // Out of the fight it mends this share of its health per second, so hit and run doesn't pay.
        private const float MendPerSecond = .1f;
        private readonly Vector2 spawnHome;
        private float provokedChase, walkTimer, mendCarry;
        private Vector2 walkAnchor;
        private readonly Transform transform;
        private readonly ContactFilter2D solids = new ContactFilter2D { useTriggers = false };
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(12);
        private readonly RaycastHit2D[] slideHits = new RaycastHit2D[8];
        private Vector2 deathSlide;
        private float lastSeen;

        public EnemyBody(GameObject owner)
        {
            transform = owner.transform; Health = owner.GetComponent<Damageable>();
            Reaction = owner.GetComponent<HitReaction>(); Rigidbody = owner.GetComponent<Rigidbody2D>();
            Collider = owner.GetComponent<Collider2D>(); Home = spawnHome = Rigidbody.position;
        }

        public bool CanSee(Vector2 point, Transform target, bool ignoreDynamicBodies)
        {
            Physics2D.Linecast(Rigidbody.position, point, solids, sight);
            foreach (var hit in sight)
            {
                if (hit.collider == null || hit.transform.IsChildOf(transform) ||
                    (target != null && hit.transform.IsChildOf(target))) continue;
                if (ignoreDynamicBodies && hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                return false;
            }
            return true;
        }

        // Sight reaches half again past the notice radius, so an aware enemy keeps a player it is chasing.
        // Seeing the target records the time for LostSight.
        public bool Sees(Transform target, float noticeRadius, bool ignoreDynamicBodies)
        {
            Vector2 point = target.position;
            bool sees = Vector2.Distance(Rigidbody.position, point) <= noticeRadius * 1.5f && CanSee(point, target, ignoreDynamicBodies);
            if (sees) MarkSeen();
            return sees;
        }

        public void MarkSeen() => lastSeen = Time.time;
        public bool LostSight(float loseSightTime) => Time.time - lastSeen > loseSightTime;

        // Only the enemy's own distance from home counts, so the player stepping away doesn't shake it off (playtest, 22 Oct).
        public float DistanceFromHome => Vector2.Distance(Rigidbody.position, Home);
        public bool OutsideLeash(float leashRadius) => DistanceFromHome > leashRadius;
        public bool CanReengage(float leashRadius) => DistanceFromHome <= leashRadius * ReengageFraction;

        public void Provoke() => provokedChase = ProvokedChaseSeconds;
        public void TickChase(float dt) => provokedChase = Mathf.Max(0f, provokedChase - dt);

        // Past the leash it breaks off only once the player has got away: never with its back to a fight.
        public bool ShouldBreakOff(float leashRadius, float loseSightTime, float toPlayer, float noticeRadius)
        {
            if (provokedChase > 0f) return false;
            if (LostSight(loseSightTime)) return true;
            if (!OutsideLeash(leashRadius)) return false;
            return toPlayer > noticeRadius || OutsideLeash(leashRadius * HardLeashFactor);
        }

        // For a visible player, while walking home.
        public bool ShouldTurnBack(float leashRadius, float toPlayer, float noticeRadius) =>
            (toPlayer <= noticeRadius && CanReengage(leashRadius)) ||
            (toPlayer <= TurnOnPlayerDistance && !OutsideLeash(leashRadius * HardLeashFactor));

        // True when a wall has stopped the walk home: it settles there until the next rest, not walking on the spot.
        public bool SettleIfStuck(float dt)
        {
            walkTimer += dt;
            if (walkTimer < StuckSeconds) return false;
            walkTimer = 0f;
            bool stuck = Vector2.Distance(Rigidbody.position, walkAnchor) < StuckDistance;
            walkAnchor = Rigidbody.position;
            if (stuck) Home = walkAnchor;
            return stuck;
        }

        public void Mend(float dt)
        {
            if (!Health.IsAlive || Health.Health >= Health.MaxHealth) { mendCarry = 0f; return; }
            mendCarry += MendPerSecond * Health.MaxHealth * dt;
            int whole = Mathf.FloorToInt(mendCarry);
            if (whole <= 0) return;
            mendCarry -= whole;
            Health.Heal(whole);
        }

        public void ClearWalkHome()
        {
            walkTimer = 0f;
            walkAnchor = Rigidbody.position;
        }

        public void BeginDefeat()
        {
            deathSlide = Rigidbody.linearVelocity;
            if (Collider != null) Collider.enabled = false;
        }

        public void SlideCorpse(float dt)
        {
            if (dt <= 0f || deathSlide.sqrMagnitude < .0004f) { Rigidbody.linearVelocity = Vector2.zero; return; }
            Vector2 step = deathSlide * dt;
            float distance = step.magnitude;
            float radius = Collider is CircleCollider2D circle ? circle.radius * .8f : .3f;
            int count = Physics2D.CircleCast(Rigidbody.position, radius, step / distance, solids, slideHits, distance);
            for (int i = 0; i < count; i++)
            {
                var other = slideHits[i].collider;
                if (other == Collider || (other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)) continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, slideHits[i].distance - .02f));
                deathSlide = Vector2.zero;
            }
            Rigidbody.position += step.normalized * distance;
            Rigidbody.linearVelocity = Vector2.zero;
            deathSlide *= Mathf.Exp(-Rigidbody.linearDamping * dt);
        }

        public void Reset()
        {
            deathSlide = Vector2.zero; Health.RestoreHealth(); Reaction.Clear();
            Home = spawnHome; provokedChase = 0f;
            Rigidbody.position = Home; transform.position = Home;
            ClearWalkHome();
            if (Collider != null) Collider.enabled = true;
        }

        public object CaptureVisitState() => new EnemyVisitState(Health, Rigidbody.position);
        public void RestoreVisitHealth(EnemyVisitState snapshot)
        {
            Health.RestoreVisitHealth(snapshot.health);
            if (Health.IsAlive) return;
            Rigidbody.position = snapshot.position; transform.position = snapshot.position;
        }
    }
}
