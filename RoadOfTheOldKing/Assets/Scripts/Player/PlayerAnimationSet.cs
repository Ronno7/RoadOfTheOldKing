using UnityEngine;

namespace TheLostShrine.Player
{
    // One slot per gameplay state. The editor importer fills a slot from the art folder of the
    // same name; any empty slot falls back as described on PlayerSpriteAnimator.
    [CreateAssetMenu(menuName = "Road of the Old King/Player Animation Set")]
    public sealed class PlayerAnimationSet : ScriptableObject
    {
        [Tooltip("Static views in each direction; the fallback for every state without its own animation.")]
        public DirectionalSpriteAnimation rotations;
        public DirectionalSpriteAnimation idle;
        [Tooltip("Reserved for the planned combat presentation state; not presented yet.")]
        public DirectionalSpriteAnimation combatIdle;

        [Header("Locomotion")]
        public DirectionalSpriteAnimation walk;
        public DirectionalSpriteAnimation run;
        public DirectionalSpriteAnimation dash;

        [Header("Combat")]
        public DirectionalSpriteAnimation lightAttack1;
        public DirectionalSpriteAnimation lightAttack2;
        public DirectionalSpriteAnimation finisher;
        public DirectionalSpriteAnimation charge;
        public DirectionalSpriteAnimation cleave;
        public DirectionalSpriteAnimation throwAim;
        public DirectionalSpriteAnimation throwRelease;
        public DirectionalSpriteAnimation catching;
        [Tooltip("Reserved for a Recall gesture; not presented yet.")]
        public DirectionalSpriteAnimation recall;

        [Header("Reactions")]
        public DirectionalSpriteAnimation hurt;
        public DirectionalSpriteAnimation death;
        [Tooltip("Reserved for returning to control after respawn; not presented yet.")]
        public DirectionalSpriteAnimation getUp;

        [Header("Items")]
        [Tooltip("Drinking a flask: sampled by the drink's progress, contact frame = the swig, where the heal lands.")]
        public DirectionalSpriteAnimation drink;

        [Header("Interaction")]
        [Tooltip("Reserved for item pickup; not presented yet.")]
        public DirectionalSpriteAnimation pickUp;
        [Tooltip("Seated loop while the bonfire menu is open, facing the fire.")]
        public DirectionalSpriteAnimation rest;
    }
}
