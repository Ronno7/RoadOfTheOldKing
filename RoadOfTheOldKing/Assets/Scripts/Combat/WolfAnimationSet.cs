using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Eight-direction PixelLab wolf. Slots are filled by Road of the Old King > Art > Import Wolf Animations
    // from Assets/Art/Sprites/Enemies/Wolf/<Slot>/<direction>/frame_###.png.
    // Standard enemy set: idle, walk, bite (attack) and death. Stalk, run, bark and hurt are optional;
    // empty slots fall back (stalk -> walk, run -> walk, hit flash for hurt, a fade for a missing death).
    [CreateAssetMenu(menuName = "Road of the Old King/Wolf Animation Set")]
    public sealed class WolfAnimationSet : ScriptableObject
    {
        public DirectionalSpriteAnimation rotations, idle, stalk, walk, run, bark, bite, hurt, death;

        [Header("Locomotion")]
        [Tooltip("Ground covered by one full cycle, in world units, so paws don't slide.")]
        [Min(.1f)] public float stalkCycleLength = 1.8f;
        [Min(.1f)] public float walkCycleLength = 2.4f;
        [Min(.1f)] public float runCycleLength = 3.6f;
        [Tooltip("Measured speed (units/s) at or above which any travel shows the run cycle.")]
        [Min(.1f)] public float runAnimationSpeed = 4.8f;

        [Header("Attack")]
        [Tooltip("Share of the recovery that finishes the bite before returning to idle.")]
        [Range(0f, 1f)] public float biteRecoveryShare = .5f;

        [Header("Feedback")]
        [Min(0f)] public float hitFlashDuration = .1f;
        [Tooltip("Fade-out time for a defeated wolf when no death animation is assigned.")]
        [Min(.05f)] public float deathFadeDuration = .5f;
    }
}
