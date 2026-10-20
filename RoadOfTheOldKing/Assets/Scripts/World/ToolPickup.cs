using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A contextual tool in the world; gating, pickup and saving come from RewardPickup (17 Oct refactor).
    // Owning the tool is its record, so a second copy elsewhere is already taken.
    public sealed class ToolPickup : RewardPickup
    {
        [SerializeField] private WorldTool tool;
        public WorldTool Tool => tool;
        protected override RewardKind Kind => RewardKind.Tool;
        protected override WorldTool GrantedTool => tool;
        public override string Prompt => "Take " + WorldToolNames.Display(tool).ToLowerInvariant();
        // Tools always need a clear reach, as before the refactor.
        public override bool CanCollect(PlayerHealth player) => tool != WorldTool.None && base.CanCollect(player) && HasClearAccess(player);
    }
}
