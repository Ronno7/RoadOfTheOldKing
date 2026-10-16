using System;
using System.Collections.Generic;
using RoadOfTheOldKing.Weapons;

namespace RoadOfTheOldKing.Progression
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
        private readonly AxeUpgradeTier[] tiers;
        public WeaponUpgradeProgression(ProgressState state, AxeUpgradeTier[] tiers)
        {
            this.state = state;
            this.tiers = tiers ?? Array.Empty<AxeUpgradeTier>();
        }

        public AxeUpgradeTier NextTier
        {
            get
            {
                foreach (var tier in tiers)
                    if (tier != null && !state.upgrades.Exists(s => s.tierId == tier.id))
                        return tier;
                return null;
            }
        }

        public bool TryPurchase(AxeUpgrade choice)
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

        public IEnumerable<AxeUpgrade> Selected
        {
            get
            {
                foreach (var tier in tiers)
                {
                    if (tier == null) continue;
                    var selection = state.upgrades.Find(s => s.tierId == tier.id);
                    if (selection == null) continue;
                    var choice = FindChoice(tier.choices, selection.upgradeId) ??
                        FindChoice(tier.retiredChoices, selection.upgradeId);
                    if (choice != null) yield return choice;
                }
            }
        }

        private static AxeUpgrade FindChoice(AxeUpgrade[] choices, string id)
        {
            if (choices == null || string.IsNullOrEmpty(id)) return null;
            foreach (var choice in choices)
                if (choice != null && choice.id == id) return choice;
            return null;
        }
    }
}
