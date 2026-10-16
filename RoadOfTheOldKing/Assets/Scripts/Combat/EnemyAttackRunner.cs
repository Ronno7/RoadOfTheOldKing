using System;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    public enum EnemyAttackPhase { None, Windup, Active, Recovery }

    // One action and movement owner per enemy. No MonoBehaviour Update or shared runtime state.
    public sealed class EnemyAttackRunner
    {
        private readonly GameObject owner;
        private readonly Rigidbody2D body;
        private readonly Collider2D collider;
        private readonly Func<Vector2, bool> clear;
        private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private PlayerHealth target;
        private float elapsed;
        private EnemyAttackSpec spec;
        public EnemyAttackPhase Phase { get; private set; }
        public bool IsRunning => Phase != EnemyAttackPhase.None;
        public bool HasContact { get; private set; }
        public Vector2 Direction { get; private set; }
        public Vector2 Origin { get; private set; }
        public bool IsCharge => spec.chargeSpeed > 0;
        public bool IsLocked => Phase != EnemyAttackPhase.Windup || elapsed >= spec.trackingTime;
        public float Progress => Mathf.Clamp01(elapsed / Mathf.Max(.001f, Duration));
        private float Duration => Phase == EnemyAttackPhase.Windup ? spec.windup : Phase == EnemyAttackPhase.Active ? spec.active : spec.recovery;

        public EnemyAttackRunner(GameObject owner, Rigidbody2D body, Collider2D collider, Func<Vector2, bool> clear)
        { this.owner = owner; this.body = body; this.collider = collider; this.clear = clear; }

        public bool Begin(EnemyAttackSpec settings, PlayerHealth victim, Vector2 direction)
        {
            if (IsRunning || victim == null || !victim.IsAlive) return false;
            spec = settings;
            spec.windup = Mathf.Max(.05f, spec.windup); spec.active = Mathf.Max(.02f, spec.active);
            spec.recovery = Mathf.Max(.05f, spec.recovery); spec.chargeSpeed = Mathf.Max(0, spec.chargeSpeed);
            spec.trackingTime = Mathf.Clamp(spec.trackingTime, 0, spec.windup - .05f);
            target = victim; Direction = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.down;
            Origin = body.position; HasContact = false; Enter(EnemyAttackPhase.Windup);
            return true;
        }

        public void Tick(float dt)
        {
            if (!IsRunning || dt <= 0) return;
            if (target == null || !target.IsAlive) { Cancel(); return; }
            elapsed += dt;
            if (Phase == EnemyAttackPhase.Windup)
            {
                body.linearVelocity = Vector2.zero;
                if (!IsLocked)
                {
                    Vector2 toward = (Vector2)target.transform.position - body.position;
                    if (toward.sqrMagnitude > .001f) Direction = toward.normalized;
                }
                if (elapsed >= spec.windup) { Origin = body.position; Enter(EnemyAttackPhase.Active); }
                return;
            }
            if (Phase == EnemyAttackPhase.Recovery)
            {
                body.linearVelocity = Vector2.zero;
                if (elapsed >= spec.recovery) Cancel();
                return;
            }

            Vector2 next = body.position;
            bool blocked = false;
            if (IsCharge)
            {
                float distance = spec.chargeSpeed * dt, allowed = distance;
                int count = collider.Cast(Direction, filter, hits, distance + .02f);
                for (int i = 0; i < count; i++)
                {
                    if (hits[i].collider == null || hits[i].transform.IsChildOf(owner.transform)) continue;
                    allowed = Mathf.Min(allowed, Mathf.Max(0, hits[i].distance - .02f));
                    blocked = true;
                }
                next += Direction * allowed;
                body.linearVelocity = Direction * (allowed / dt);
            }
            else body.linearVelocity = Vector2.zero;
            if (!HasContact)
                HasContact = EnemyMeleeDelivery.Arc(owner, target, next, Direction, spec.reach, spec.arc,
                    spec.damage, spec.knockback, spec.stagger, clear);
            if (elapsed >= spec.active || (IsCharge && (blocked || HasContact))) Enter(EnemyAttackPhase.Recovery);
        }

        public void Cancel(bool stopMovement = true)
        {
            Phase = EnemyAttackPhase.None; elapsed = 0; target = null; HasContact = false;
            if (stopMovement) body.linearVelocity = Vector2.zero;
        }
        private void Enter(EnemyAttackPhase phase)
        { Phase = phase; elapsed = 0; body.linearVelocity = Vector2.zero; }
    }
}
