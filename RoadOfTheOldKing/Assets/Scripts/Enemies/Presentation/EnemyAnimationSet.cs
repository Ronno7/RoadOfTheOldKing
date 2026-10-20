using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    [CreateAssetMenu(menuName = "Road of the Old King/Enemy Animation Set")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "EnemyAnimationSet")]
    public class EnemyAnimationSet : ScriptableObject
    {
        public DirectionalSpriteAnimation rotations, idle, walk, run, attack, hurt, death;
        [Min(.1f)] public float walkCycleLength = 2f;
        [Min(.05f)] public float deathFadeDuration = .5f;
    }
}
