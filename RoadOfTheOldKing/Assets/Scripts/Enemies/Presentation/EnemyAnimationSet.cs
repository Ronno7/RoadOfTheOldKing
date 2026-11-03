using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    [CreateAssetMenu(menuName = "Road of the Old King/Enemy Animation Set")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "EnemyAnimationSet")]
    public class EnemyAnimationSet : ScriptableObject
    {
        public DirectionalSpriteAnimation rotations, idle, walk, run, attack, hurt, death;
        [Tooltip("Optional second attack (the brain's close attack). Empty uses the attack clip.")]
        public DirectionalSpriteAnimation closeAttack;
        [Tooltip("Optional rise from a dormant pose (a sleeping guardian waking). Its first frame is the dormant pose.")]
        public DirectionalSpriteAnimation wake;
        [Min(.1f)] public float walkCycleLength = 2f;
        [Tooltip("Ground covered by one run cycle, in world units.")]
        [Min(.1f)] public float runCycleLength = 3.6f;
        [Tooltip("Measured speed (units/s) at or above which travel shows the run cycle. Sets without a walk run at any speed.")]
        [Min(.1f)] public float runAnimationSpeed = 4.8f;
        [Min(.05f)] public float deathFadeDuration = .5f;

        // A mid-stride frame facing a direction, for drawing the enemy where it's about to be.
        public Sprite StridePose(Vector2 direction)
        {
            var clip = run != null && !run.IsEmpty ? run : walk != null && !walk.IsEmpty ? walk : rotations;
            if (clip == null || clip.IsEmpty) return null;
            int octant = DirectionalSpriteAnimation.Octant(direction);
            return clip.Frame(octant, Mathf.Min(1, clip.FrameCount(octant) - 1));
        }
    }
}
