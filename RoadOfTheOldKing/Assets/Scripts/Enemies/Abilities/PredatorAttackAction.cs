using System;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Value snapshot: decisions/random rolls happen in the policy, never in a shared asset or runner.
    public struct PredatorAttackSpec
    {
        public EnemyAttackSpec attack;
        public bool lunge, feint;
        public float feintAt, lockLead, distance, snoutOffset, biteRadius, acceleration;
    }

    // A tracking snap or committed, armored leap. Feint/follow-up selection stays in the policy.
    public sealed class PredatorAttackAction : IEnemyAttackAction
    {
        private readonly GameObject owner;
        private readonly Rigidbody2D body;
        private readonly HitReaction reaction;
        private readonly PlayerHealth target;
        private readonly Func<Vector2, bool> clear;
        private readonly PredatorAttackSpec spec;
        private EnemyActionClock clock;
        private Vector2 previousPosition;
        private bool attempted;
        public bool Feinted { get; private set; }
        public bool FinishedActive { get; private set; }
        public bool IsComplete => Phase == EnemyAttackPhase.None;
        public EnemyAttackPhase Phase => clock.Phase;
        public float Progress => clock.Progress;
        public Vector2 Direction { get; private set; }
        public Vector2 Origin { get; private set; }
        public bool HasContact { get; private set; }
        public bool IsCharge => spec.lunge;
        public bool IsLocked => spec.lunge && (Phase != EnemyAttackPhase.Windup || clock.Elapsed >= spec.attack.windup - spec.lockLead);
        public float TrackProgress => Mathf.Clamp01(clock.Elapsed / Mathf.Max(.01f, spec.attack.windup - spec.lockLead));

        public PredatorAttackAction(GameObject owner, Rigidbody2D body, HitReaction reaction, PlayerHealth target,
            Func<Vector2, bool> clear, PredatorAttackSpec spec, Vector2 direction)
        {
            this.owner = owner; this.body = body; this.reaction = reaction; this.target = target;
            this.clear = clear; this.spec = spec; Direction = direction;
        }

        public bool Begin()
        {
            if (!IsComplete || target == null || !target.IsAlive) return false;
            previousPosition = Origin = body.position;
            HasContact = attempted = Feinted = FinishedActive = false;
            reaction.Armored = false;
            clock.Enter(EnemyAttackPhase.Windup, spec.attack.windup);
            return true;
        }

        public void Tick(float dt)
        {
            if (!clock.Advance(dt)) return;
            if (target == null || !target.IsAlive) { Cancel(true); return; }
            float actualSpeed = (body.position - previousPosition).magnitude / dt;
            previousPosition = body.position;
            FinishedActive = false;
            if (Phase == EnemyAttackPhase.Windup)
            {
                Brake(dt);
                Vector2 toward = (Vector2)target.transform.position - body.position;
                if (!IsLocked && toward.sqrMagnitude > .000001f) Direction = toward.normalized;
                if (spec.feint && Progress >= spec.feintAt)
                { Feinted = true; clock.Clear(); return; }
                if (clock.Expired)
                {
                    Origin = body.position;
                    reaction.Armored = spec.lunge;
                    clock.Enter(EnemyAttackPhase.Active, spec.attack.active);
                }
                return;
            }
            if (Phase == EnemyAttackPhase.Recovery)
            {
                Brake(dt);
                if (clock.Expired) clock.Clear();
                return;
            }

            bool blocked = false;
            if (spec.lunge)
            {
                float speed = spec.distance / spec.attack.active;
                body.linearVelocity = Direction * speed;
                if (!HasContact)
                    HasContact = EnemyMeleeDelivery.Bite(owner, target, body.position + Direction * spec.snoutOffset,
                        Direction, spec.biteRadius, spec.attack.damage, spec.attack.knockback, spec.attack.stagger, clear);
                blocked = clock.Elapsed > .05f && actualSpeed < speed * .25f;
            }
            else
            {
                Brake(dt);
                // A snap has one attempt at the start of its active window, even if it misses.
                var shape = target.GetComponent<Collider2D>();
                if (!attempted && shape != null && shape.enabled)
                {
                    attempted = true;
                    HasContact = EnemyMeleeDelivery.Arc(owner, target, body.position, Direction, spec.attack.reach,
                        spec.attack.arc, spec.attack.damage, spec.attack.knockback, spec.attack.stagger, clear);
                }
            }
            if (!blocked && !clock.Expired) return;
            if (spec.lunge) body.linearVelocity *= .4f;
            reaction.Armored = false;
            FinishedActive = true;
            clock.Enter(EnemyAttackPhase.Recovery, spec.attack.recovery);
        }

        public void Cancel(bool stopMovement)
        {
            clock.Clear(); reaction.Armored = false;
            if (stopMovement) body.linearVelocity = Vector2.zero;
        }
        private void Brake(float dt) => body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, spec.acceleration * 2f * dt);
    }
}
