using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    public enum EnemyAttackPhase { None, Windup, Active, Recovery }

    // One phase transition per physics tick, preserving the authored windup/active/recovery beats.
    internal struct EnemyActionClock
    {
        public EnemyAttackPhase Phase { get; private set; }
        public float Elapsed { get; private set; }
        private float duration;
        public float Progress => Mathf.Clamp01(Elapsed / Mathf.Max(.001f, duration));
        public bool Expired => Elapsed >= duration;
        public void Enter(EnemyAttackPhase phase, float seconds)
        { Phase = phase; duration = seconds; Elapsed = 0f; }
        public bool Advance(float dt)
        {
            if (Phase == EnemyAttackPhase.None || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return false;
            Elapsed += dt;
            return true;
        }
        public void Clear() => Enter(EnemyAttackPhase.None, 0f);
    }
}
