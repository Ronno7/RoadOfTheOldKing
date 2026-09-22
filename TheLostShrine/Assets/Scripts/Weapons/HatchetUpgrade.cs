using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "The Lost Shrine/Hatchet Upgrade")]
    public sealed class HatchetUpgrade : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        [Min(1f)] public float lightSpeedMultiplier = 1f;
        [Min(0f)] public float addedLightArc;
        [Min(0f)] public float addedCleaveRadius;
    }
}
