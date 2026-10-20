using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Prototype
{
    // This scene is a disposable design proof, excluded from the normal build/scene flow.
    [DefaultExecutionOrder(-90)]
    public sealed class LowlandsSluiceProof : MonoBehaviour
    {
        public const string SaveKey = "RoadOfTheOldKing.Prototype.LowlandsSluice";
        [SerializeField] private RecallSluice sluice;

        private void Awake()
        {
            var session = GameSession.Instance;
            if (session == null || (session.StorageKey != SaveKey &&
                !session.StorageKey.StartsWith("RoadOfTheOldKing.Verification.Sluice.")))
            {
                Debug.LogError("Open SluiceProof from stopped Edit Mode with its isolated session.", this);
                enabled = false;
                return;
            }
            session.Progress.hasAxe = true;
            session.Progress.recallUnlocked = true;
        }

        private void OnGUI()
        {
            float left = Mathf.Max(12, Screen.width - 452);
            GUI.Box(new Rect(left, 12, 440, 78), "");
            GUI.Label(new Rect(left + 12, 18, 416, 22), "SLUICE GREYBOX  |  separate prototype save");
            GUI.Label(new Rect(left + 12, 40, 416, 22), "WASD move  /  E throw + recall  /  F interact");
            GUI.Label(new Rect(left + 12, 62, 416, 22), sluice != null ? sluice.Feedback : "");
        }
    }
}
