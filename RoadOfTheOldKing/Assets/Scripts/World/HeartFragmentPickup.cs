using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A heart fragment in the world; gating, pickup and saving come from RewardPickup (17 Oct refactor).
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class HeartFragmentPickup : RewardPickup
    {
        protected override RewardKind Kind => RewardKind.HeartFragment;
        public override string Prompt => "Take fragment";
        protected override void OnRefresh(bool shown) => GetComponent<Collider2D>().enabled = !IsCollected;
    }
}
