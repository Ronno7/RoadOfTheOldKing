using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    [CreateAssetMenu(menuName = "Road of the Old King/Enemy Animation Set")]
    public sealed class EnemyAnimationSet : ScriptableObject
    {
        public DirectionalSpriteAnimation rotations, idle, walk, run, attack, hurt, death;
        [Min(.1f)] public float walkCycleLength = 2f;
        [Min(.05f)] public float deathFadeDuration = .5f;
    }
}
