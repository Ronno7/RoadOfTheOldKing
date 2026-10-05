using System;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.World;

namespace RoadOfTheOldKing.Progression
{
    // Sun Shards, heart fragments and upgrade purchases: small operations over the session's progress
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
