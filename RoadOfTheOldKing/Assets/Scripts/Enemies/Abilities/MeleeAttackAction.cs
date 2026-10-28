using System;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // One action and movement owner per enemy. No MonoBehaviour Update or shared runtime state.
    internal sealed class MeleeAttackAction : IEnemyAttackAction
    {
        private readonly GameObject owner;
        private readonly Rigidbody2D body;
        private readonly Collider2D collider;
        private readonly Func<Vector2, bool> clear;
        private readonly HitReaction reaction;
        private readonly ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        private readonly RaycastHit2D[] hits = new RaycastHit2D[16];
        private PlayerHealth target;
        private EnemyActionClock clock;
        private readonly EnemyAttackSpec settings;
        private readonly Vector2 initialDirection;
        private EnemyAttackSpec spec;
        private bool armored;
        public EnemyAttackPhase Phase => clock.Phase;
        public bool IsComplete => !IsRunning;
        public bool IsRunning => Phase != EnemyAttackPhase.None;
        public bool HasContact { get; private set; }
        public Vector2 Direction { get; private set; }
        public Vector2 Origin { get; private set; }
        public bool IsCharge => spec.chargeSpeed > 0;
        public bool IsLocked => Phase != EnemyAttackPhase.Windup || clock.Elapsed >= spec.trackingTime;
        public float Progress => clock.Progress;

        public MeleeAttackAction(GameObject owner, Rigidbody2D body, Collider2D collider, HitReaction reaction, Func<Vector2, bool> clear,
            EnemyAttackSpec settings, PlayerHealth victim, Vector2 direction)
        {
            this.owner = owner; this.body = body; this.collider = collider; this.reaction = reaction; this.clear = clear;
            this.settings = settings; target = victim; initialDirection = direction;
        }

        public bool Begin()
        {
            if (IsRunning || target == null || !target.IsAlive) return false;
            spec = settings;
            spec.windup = Mathf.Max(.05f, spec.windup); spec.active = Mathf.Max(.02f, spec.active);
            spec.recovery = Mathf.Max(.05f, spec.recovery); spec.chargeSpeed = Mathf.Max(0, spec.chargeSpeed);
            spec.trackingTime = Mathf.Clamp(spec.trackingTime, 0, spec.windup - .05f);
            Direction = initialDirection.sqrMagnitude > .001f ? initialDirection.normalized : Vector2.down;
            Origin = body.position; HasContact = false; Enter(EnemyAttackPhase.Windup);
            return true;
        }

        public void Tick(float dt)
        {
            if (!clock.Advance(dt)) return;
            if (target == null || !target.IsAlive) { Cancel(); return; }
            if (Phase == EnemyAttackPhase.Windup)
            {
                body.linearVelocity = Vector2.zero;
                if (!IsLocked)
                {
                    Vector2 toward = (Vector2)target.transform.position - body.position;
                    if (toward.sqrMagnitude > .001f) Direction = toward.normalized;
                }
                if (IsLocked && spec.armoredWhenLocked) SetArmor(true);
                if (clock.Elapsed >= spec.windup) { Origin = body.position; Enter(EnemyAttackPhase.Active); }
                return;
            }
            if (Phase == EnemyAttackPhase.Recovery)
            {
                body.linearVelocity = Vector2.zero;
                if (clock.Elapsed >= spec.recovery) Cancel();
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
            if (clock.Elapsed >= spec.active || (IsCharge && (blocked || HasContact))) Enter(EnemyAttackPhase.Recovery);
        }

        public void Cancel(bool stopMovement = true)
        {
            clock.Clear(); target = null; HasContact = false; SetArmor(false);
            if (stopMovement) body.linearVelocity = Vector2.zero;
        }
        private void Enter(EnemyAttackPhase phase)
        {
            clock.Enter(phase, phase == EnemyAttackPhase.Windup ? spec.windup : phase == EnemyAttackPhase.Active ? spec.active : spec.recovery);
            body.linearVelocity = Vector2.zero;
            // Recovery is the opening: armour never carries into it.
            if (phase == EnemyAttackPhase.Recovery) SetArmor(false);
        }

        // Only clears armour this action set, so another owner's armour is left alone.
        private void SetArmor(bool on)
        {
            if (reaction == null || armored == on) return;
            armored = on; reaction.Armored = on;
        }
    }
}
