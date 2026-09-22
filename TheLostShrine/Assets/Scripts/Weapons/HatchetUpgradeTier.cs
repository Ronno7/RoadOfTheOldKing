using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "The Lost Shrine/Hatchet Upgrade Tier")]
    public sealed class HatchetUpgradeTier : ScriptableObject
    {
        public string id;
        [Min(1)] public int shardCost = 3;
        public HatchetUpgrade[] choices = new HatchetUpgrade[3];
    }
}
