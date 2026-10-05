using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class RecallUnlockPickup : WorldPickup, IProgressParticipant
    {
        [SerializeField] private SpriteRenderer crystal;
        [SerializeField] private TextMesh label;
        private bool activated;

        public override string Prompt => "Awaken Recall";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            !activated && player.GetComponent<PlayerCombatController>() != null &&
            !player.GetComponent<PlayerCombatController>().CanRecall;

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            player.GetComponent<PlayerCombatController>().UnlockRecall();
            ShowActivated();
            GameSession.Instance?.SaveProgress();
            return true;
        }

        private void ShowActivated()
        {
            activated = true;
            if (crystal != null)
                crystal.color = new Color(0.7f, 1f, 0.8f);
            if (label != null)
                label.text = "RECALL AWAKENED";
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state)
        {
            if (state.recallUnlocked)
                ShowActivated();
        }
    }
}
