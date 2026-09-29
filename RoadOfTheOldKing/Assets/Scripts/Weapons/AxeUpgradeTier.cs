using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Axe Upgrade Tier")]
    public sealed class AxeUpgradeTier : ScriptableObject
    {
        public string id;
        [Min(1)] public int shardCost = 3;
        public AxeUpgrade[] choices = new AxeUpgrade[3];
    }
}
