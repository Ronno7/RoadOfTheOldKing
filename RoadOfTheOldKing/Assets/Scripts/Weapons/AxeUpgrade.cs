using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Axe Upgrade")]
    public sealed class AxeUpgrade : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(1f)] public float lightSpeedMultiplier = 1f;
        [Min(0f)] public float addedLightArc;
        [Min(0f)] public float addedCleaveRadius;
        [Tooltip("Deep Notch-style bonuses strengthen only the third hit, without widening its lane.")]
        [Min(1f)] public float finisherDamageMultiplier = 1f;
    }
}
