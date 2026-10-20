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
        public Vector2 Home { get; }
        private readonly Transform transform;
        private readonly ContactFilter2D solids = new ContactFilter2D { useTriggers = false };
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(12);
        private readonly RaycastHit2D[] slideHits = new RaycastHit2D[8];
        private Vector2 deathSlide;

        public EnemyBody(GameObject owner)
        {
            transform = owner.transform; Health = owner.GetComponent<Damageable>();
            Reaction = owner.GetComponent<HitReaction>(); Rigidbody = owner.GetComponent<Rigidbody2D>();
            Collider = owner.GetComponent<Collider2D>(); Home = Rigidbody.position;
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
            Rigidbody.position = Home; transform.position = Home;
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
