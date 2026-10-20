using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // An instance-owned action. Begin is called only after the runner grants exclusive ownership.
    public interface IEnemyAction
    {
        bool Begin();
        void Tick(float deltaTime);
        void Cancel(bool stopMovement);
        bool IsComplete { get; }
        EnemyAttackPhase Phase { get; }
        float Progress { get; }
        Vector2 Direction { get; }
    }

    public interface IEnemyAttackAction : IEnemyAction
    {
        bool HasContact { get; }
        bool IsLocked { get; }
        bool IsCharge { get; }
        Vector2 Origin { get; }
    }
}
