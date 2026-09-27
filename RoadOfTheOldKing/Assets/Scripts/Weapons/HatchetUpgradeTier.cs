using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Hatchet Upgrade Tier")]
    public sealed class HatchetUpgradeTier : ScriptableObject
    {
        public string id;
        [Min(1)] public int shardCost = 3;
        public HatchetUpgrade[] choices = new HatchetUpgrade[3];
    }
}
