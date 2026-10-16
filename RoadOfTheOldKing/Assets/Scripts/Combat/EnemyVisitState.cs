using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Living enemies resume idle at home; corpses retain their last resting position.
    internal sealed class EnemyVisitState
    {
        internal readonly int health;
        internal readonly Vector2 position;
        internal EnemyVisitState(Damageable source, Vector2 position)
        {
            health = source.Health;
            this.position = position;
        }
    }
}
