using UnityEngine;

namespace TheLostShrine.Combat
{
    [CreateAssetMenu(menuName = "Road of the Old King/Wolf Animation Set")]
    public sealed class WolfAnimationSet : ScriptableObject
    {
        [System.Serializable]
        public sealed class Poses
        {
            public Sprite idle, crouch, bite, dead;
            public Sprite[] move;
        }

        public Poses side = new Poses(), south = new Poses(), north = new Poses();
        [Min(.1f)] public float strideDistance = .9f;
        [Min(.02f)] public float collapseDuration = .2f;
        [Min(0f)] public float hitFlashDuration = .1f;
    }
}
