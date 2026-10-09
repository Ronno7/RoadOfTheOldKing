using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public sealed class ToolPickup : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private WorldTool tool;
        [SerializeField] private GameObject visual;
        public WorldTool Tool => tool;
        public override string Prompt => "Take " + WorldToolNames.Display(tool).ToLowerInvariant();
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) && HasClearAccess(player) &&
            GameSession.Instance != null && !GameSession.Instance.IsLoading &&
            !GameSession.Instance.Progress.OwnsTool(tool) && tool != WorldTool.None && !string.IsNullOrWhiteSpace(rewardId);
        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player) || !GameSession.Instance.Rewards.TryCollectTool(rewardId, tool)) return false;
            RestoreProgress(GameSession.Instance.Progress);
            return true;
        }
        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state) { if (visual != null) visual.SetActive(!state.OwnsTool(tool)); }
    }
}
