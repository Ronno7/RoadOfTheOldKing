using System;
using System.Collections.Generic;
using TheLostShrine.Weapons;

namespace TheLostShrine.Progression
{
    [Serializable]
    public sealed class UpgradeSelection
    {
        public string tierId;
        public string upgradeId;
    }

    // Purchase rules operate on save data; no dependency on UI, scene objects or storage.
    public sealed class WeaponUpgradeProgression
    {
        private readonly ProgressState state;
        private readonly HatchetUpgradeTier[] tiers;
        public WeaponUpgradeProgression(ProgressState state, HatchetUpgradeTier[] tiers)
        {
            this.state = state;
            this.tiers = tiers ?? Array.Empty<HatchetUpgradeTier>();
        }

        public HatchetUpgradeTier NextTier
        {
            get
            {
                foreach (var tier in tiers)
                    if (tier != null && !state.upgrades.Exists(s => s.tierId == tier.id))
                        return tier;
                return null;
            }
        }

        public bool TryPurchase(HatchetUpgrade choice)
        {
            var tier = NextTier;
            if (tier == null || string.IsNullOrEmpty(tier.id) || tier.shardCost < 1 ||
                choice == null || string.IsNullOrEmpty(choice.id) || tier.choices == null ||
                Array.IndexOf(tier.choices, choice) < 0 || state.sunShards < tier.shardCost)
                return false;
            state.sunShards -= tier.shardCost;
            state.upgrades.Add(new UpgradeSelection { tierId = tier.id, upgradeId = choice.id });
            return true;
        }

        public IEnumerable<HatchetUpgrade> Selected
        {
            get
            {
                foreach (var tier in tiers)
                {
                    if (tier == null || tier.choices == null) continue;
                    var selection = state.upgrades.Find(s => s.tierId == tier.id);
                    if (selection == null) continue;
                    foreach (var choice in tier.choices)
                        if (choice != null && choice.id == selection.upgradeId)
                        { yield return choice; break; }
                }
            }
        }
    }
}
