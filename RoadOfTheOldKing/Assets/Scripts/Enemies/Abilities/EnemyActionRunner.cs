using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // The only action owner on an enemy. Policies choose actions; actions own their clocks and motion.
    // New action types implement IEnemyAction without adding enemy/species branches here.
    public sealed class EnemyActionRunner
    {
        public IEnemyAction Current { get; private set; }
        public bool IsRunning => Current != null && !Current.IsComplete;
        public EnemyAttackPhase Phase => Current?.Phase ?? EnemyAttackPhase.None;
        public float Progress => Current?.Progress ?? 0f;
        public Vector2 Direction => Current?.Direction ?? Vector2.down;
        private IEnemyAttackAction Attack => Current as IEnemyAttackAction;
        public bool HasContact => Attack != null && Attack.HasContact;
        public bool IsLocked => Attack == null || Attack.IsLocked;
        public bool IsCharge => Attack != null && Attack.IsCharge;
        public Vector2 Origin => Attack != null ? Attack.Origin : Vector2.zero;

        public bool Begin(IEnemyAction action)
        {
            if (IsRunning || action == null || !action.Begin()) return false;
            Current = action;
            return true;
        }

        public void Tick(float dt)
        {
            if (IsRunning) Current.Tick(dt);
        }

        public void Cancel(bool stopMovement = true)
        {
            Current?.Cancel(stopMovement);
            Current = null;
        }
    }
}
