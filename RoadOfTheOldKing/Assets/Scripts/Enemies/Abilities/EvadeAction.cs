using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Retreat and lateral sidestep use the same bounded motion action.
    public sealed class EvadeAction : IEnemyAction
    {
        private readonly Rigidbody2D body;
        private readonly float distance, duration;
        private EnemyActionClock clock;
        public EvadeAction(Rigidbody2D body, Vector2 direction, float distance, float duration)
        { this.body = body; Direction = direction; this.distance = distance; this.duration = Mathf.Max(.05f, duration); }
        public Vector2 Direction { get; }
        public bool IsComplete => Phase == EnemyAttackPhase.None;
        public EnemyAttackPhase Phase => clock.Phase;
        public float Progress => clock.Progress;
        public bool Begin()
        {
            if (!IsComplete) return false;
            clock.Enter(EnemyAttackPhase.Active, duration);
            return true;
        }
        public void Tick(float dt)
        {
            if (!clock.Advance(dt)) return;
            body.linearVelocity = Direction * (distance / duration);
            if (clock.Expired) { body.linearVelocity *= .3f; clock.Clear(); }
        }
        public void Cancel(bool stopMovement)
        { clock.Clear(); if (stopMovement) body.linearVelocity = Vector2.zero; }
    }
}
