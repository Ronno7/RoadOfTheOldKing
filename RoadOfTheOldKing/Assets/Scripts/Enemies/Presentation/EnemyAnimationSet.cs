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
        [Tooltip("Ground covered by one run cycle, in world units.")]
        [Min(.1f)] public float runCycleLength = 3.6f;
        [Tooltip("Measured speed (units/s) at or above which travel shows the run cycle. Sets without a walk run at any speed.")]
        [Min(.1f)] public float runAnimationSpeed = 4.8f;
        [Min(.05f)] public float deathFadeDuration = .5f;
    }
}
