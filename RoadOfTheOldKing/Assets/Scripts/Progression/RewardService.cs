using System;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.World;

namespace RoadOfTheOldKing.Progression
{
    // Shards, hearts, coins, owned tools and purchases: small operations over the session's progress
    // that save and then raise presentation events. The rules live in HeartFragmentProgression and
    // WeaponUpgradeProgression.
    public sealed class RewardService
    {
        private readonly GameSession session;
        private readonly AxeUpgradeTier[] tiers;
        private WeaponUpgradeProgression upgrades;

        internal RewardService(GameSession session, AxeUpgradeTier[] tiers)
        {
            this.session = session;
            this.tiers = tiers ?? Array.Empty<AxeUpgradeTier>();
        }

        public WeaponUpgradeProgression Upgrades => upgrades ??= new WeaponUpgradeProgression(session.Progress, tiers);
        // Presentation hooks, raised only after the progress change succeeded.
        public event Action<int> ShardCollected;                 // new balance
        public event Action<int, bool> HeartFragmentCollected;   // total fragments, completed a heart

        public event Action<int, int> CoinsCollected;             // amount, new balance
        public event Action<WorldTool> ToolCollected;

        private bool CanAward => !session.IsLoading && session.Player != null && session.Player.IsAlive;

        public CoinSourceRecord RevealCoinSource(string sourceId, int guaranteedCoins)
        {
            if (!CanAward || string.IsNullOrWhiteSpace(sourceId) || guaranteedCoins < 0) return null;
            var old = session.Progress.FindCoinSource(sourceId);
            if (old != null) return old;
            // Only an unseen source consumes RNG. Empty rolls are durable too.
            float sample = guaranteedCoins > 0 ? 0 : UnityEngine.Random.value;
            int amount = guaranteedCoins > 0 ? guaranteedCoins : sample < .80f ? 0 : sample < .95f ? 1 : 5;
            var record = new CoinSourceRecord { id = sourceId, amount = amount, collected = amount == 0 };
            session.Progress.coinSources.Add(record);
            session.CaptureAndSave("Progress saved.");
            return record;
        }

        public bool TryCollectCoinSource(string sourceId)
        {
            var progress = session.Progress;
            var record = progress.FindCoinSource(sourceId);
            if (!CanAward || record == null || record.collected || record.amount <= 0 ||
                progress.bronzeCoins > int.MaxValue - record.amount) return false;
            record.collected = true;
            progress.bronzeCoins += record.amount;
            session.CaptureAndSave("Bronze Coins +" + record.amount);
            CoinsCollected?.Invoke(record.amount, progress.bronzeCoins);
            return true;
        }

        public bool TryCollectCoinCache(string rewardId, int amount)
        {
            var progress = session.Progress;
            if (!CanAward || string.IsNullOrWhiteSpace(rewardId) || amount <= 0 ||
                progress.Has("coins/collected/" + rewardId) || progress.bronzeCoins > int.MaxValue - amount) return false;
            progress.Complete("coins/collected/" + rewardId);
            progress.bronzeCoins += amount;
            session.CaptureAndSave("Bronze Coins +" + amount);
            CoinsCollected?.Invoke(amount, progress.bronzeCoins);
            return true;
        }

        public bool TryCollectTool(string rewardId, WorldTool tool)
        {
            var progress = session.Progress;
            if (!CanAward || string.IsNullOrWhiteSpace(rewardId) || tool == WorldTool.None ||
                !Enum.IsDefined(typeof(WorldTool), tool) || progress.OwnsTool(tool)) return false;
            progress.ownedTools.Add(tool);
            progress.Complete("tool/collected/" + rewardId);
            session.CaptureAndSave(WorldToolNames.Display(tool) + " acquired.");
            ToolCollected?.Invoke(tool);
            return true;
        }

        public bool TryPurchase(ShopOffer offer, PlayerHealth player)
        {
            if (!CanAward || player != session.Player || offer == null || !offer.CanCollect(player)) return false;
            var progress = session.Progress;
            if (offer.Price <= 0 || progress.bronzeCoins < offer.Price) return false;
            // Validate and grant before the debit; all fields are then written in one save.
            if (offer.Reward == ShopReward.Tool)
            {
                if (offer.Tool == WorldTool.None || !Enum.IsDefined(typeof(WorldTool), offer.Tool) || progress.OwnsTool(offer.Tool)) return false;
                progress.ownedTools.Add(offer.Tool);
            }
            else if (!HeartFragmentProgression.TryCollect(progress, offer.RewardId)) return false;
            progress.bronzeCoins -= offer.Price;
            progress.Complete("shop/purchased/" + offer.OfferId);
            if (offer.Reward == ShopReward.HeartFragment) player.RestoreProgress(progress);
            session.CaptureAndSave("Purchased " + offer.ItemName + ".");
            if (offer.Reward == ShopReward.Tool) ToolCollected?.Invoke(offer.Tool);
            else HeartFragmentCollected?.Invoke(HeartFragmentProgression.Count(progress),
                HeartFragmentProgression.Count(progress) % HeartFragmentProgression.FragmentsPerHeart == 0);
            return true;
        }

        // The upgrade view wraps a ProgressState, so it is rebuilt when the session replaces it.
        internal void OnProgressReplaced() => upgrades = null;

        internal void ApplyUpgrades()
        {
            var combat = session.Combat;
            if (combat != null && combat.Weapon != null) combat.Weapon.ApplyUpgrades(Upgrades.Selected);
        }

        public bool TryCollectShard(string rewardId)
        {
            var progress = session.Progress;
            if (session.IsLoading || string.IsNullOrEmpty(rewardId) || progress.Has("shard/collected/" + rewardId))
                return false;
            progress.Complete("shard/collected/" + rewardId);
            progress.sunShards++;
            session.CaptureAndSave("Sun Shard +1");
            ShardCollected?.Invoke(progress.sunShards);
            return true;
        }

        public bool TryPurchaseUpgrade(Bonfire fire, AxeUpgrade choice)
        {
            var player = session.Player;
            var combat = session.Combat;
            if (session.IsLoading || player == null || !player.IsAlive || combat == null || combat.Weapon == null ||
                fire == null || !fire.AllowsUpgrades || Array.IndexOf(session.Checkpoints.Fires, fire) < 0 ||
                !fire.CanUse(player.transform) ||
                player.GetComponent<PlayerBonfireInteraction>().ActiveFire != fire || !Upgrades.TryPurchase(choice))
                return false;
            ApplyUpgrades();
            session.CaptureAndSave(choice.displayName + " chosen. The other choices in this tier are gone.");
            return true;
        }

        public bool TryCollectHeartFragment(string rewardId)
        {
            var player = session.Player;
            if (session.IsLoading || player == null || !player.IsAlive ||
                !HeartFragmentProgression.TryCollect(session.Progress, rewardId))
                return false;
            player.RestoreProgress(session.Progress);
            int count = HeartFragmentProgression.Count(session.Progress);
            bool completed = count % HeartFragmentProgression.FragmentsPerHeart == 0;
            session.CaptureAndSave(completed
                ? "Heart complete! Maximum HP +20."
                : "Heart fragment collected: " + count % HeartFragmentProgression.FragmentsPerHeart + " / 3.");
            HeartFragmentCollected?.Invoke(count, completed);
            return true;
        }
    }
}
