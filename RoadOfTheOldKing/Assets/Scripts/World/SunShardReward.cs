using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A Sun Shard in the world. Gating (an EncounterUnlock, a milestone), the pickup and the save record live in
    // RewardPickup, EncounterUnlock and RewardService (17 Oct refactor); this only names the reward. The Tutorial
    // shard's wolf pack moved onto an EncounterUnlock whose milestone is "shard/available/<id>", the key older saves hold.
    [DisallowMultipleComponent]
    public sealed class SunShardReward : RewardPickup
    {
        protected override RewardKind Kind => RewardKind.SunShard;
        public override string Prompt => "Take Sun Shard";
    }
}
