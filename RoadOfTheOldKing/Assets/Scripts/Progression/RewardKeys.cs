namespace RoadOfTheOldKing.Progression
{
    // What a world reward grants. Values are save data (RewardChest serializes them): append only.
    public enum RewardKind { SunShard, HeartFragment, BronzeCoins, Tool }

    // The one owner of reward save keys (17 Oct refactor). Every key below is shipped save data:
    // never rename a prefix. Heart fragments keep theirs in HeartFragmentProgression.
    public static class RewardKeys
    {
        public static string ShardCollected(string rewardId) => "shard/collected/" + rewardId;
        // Written by the shard's unlock condition when its guards fall (older saves wrote it from the shard itself).
        public static string ShardAvailable(string rewardId) => "shard/available/" + rewardId;
        public static string CoinCache(string rewardId) => "coins/collected/" + rewardId;
        public static string ToolCollected(string rewardId) => "tool/collected/" + rewardId;
        public static string Purchased(string offerId) => "shop/purchased/" + offerId;

        // Whether a reward of this kind and id has been granted in this progress.
        public static bool IsGranted(ProgressState state, RewardKind kind, string rewardId, WorldTool tool = WorldTool.None)
        {
            if (state == null) return false;
            switch (kind)
            {
                case RewardKind.SunShard: return !string.IsNullOrEmpty(rewardId) && state.Has(ShardCollected(rewardId));
                case RewardKind.HeartFragment: return HeartFragmentProgression.IsCollected(state, rewardId);
                case RewardKind.BronzeCoins: return !string.IsNullOrEmpty(rewardId) && state.Has(CoinCache(rewardId));
                case RewardKind.Tool: return state.OwnsTool(tool);
                default: return false;
            }
        }
    }
}
