using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Axe Upgrade Tier")]
    public sealed class AxeUpgradeTier : ScriptableObject
    {
        public string id;
        [Min(1)] public int shardCost = 3;
        public AxeUpgrade[] choices = new AxeUpgrade[3];
        [Tooltip("Previously sold choices, kept only to restore existing saved selections. These are not offered for purchase.")]
        public AxeUpgrade[] retiredChoices = System.Array.Empty<AxeUpgrade>();
    }
}
