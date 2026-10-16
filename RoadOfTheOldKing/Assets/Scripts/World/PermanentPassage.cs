using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A physically placed lever opens a persistent passage. There is no remote hit receiver:
    // reaching the operating side, rather than possessing a tool, earns the connection.
    [DisallowMultipleComponent]
    public sealed class PermanentPassage : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string progressId;
        [SerializeField] private string prompt = "Release gate";
        [SerializeField] private GameObject[] closedObjects = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] openedObjects = System.Array.Empty<GameObject>();
        [SerializeField] private Transform movingPart;
        [SerializeField] private Vector3 openingTravel = Vector3.up;
        [SerializeField, Min(.05f)] private float openingSeconds = .7f;
        private Vector3 restPosition;
        private bool cached;
        private float opening;

        public bool IsOpen { get; private set; }
        public bool IsMoving => IsOpen && opening < 1f;
        public string ProgressId => progressId;
        public override string Prompt => prompt;

        public override bool CanCollect(PlayerHealth player)
        {
            var session = GameSession.Instance;
            return !IsOpen && !string.IsNullOrWhiteSpace(progressId) &&
                base.CanCollect(player) && session != null && !session.IsLoading &&
                session.Player == player && HasClearAccess(player);
        }

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            Cache();
            IsOpen = true;
            GameSession.Instance.Progress.Complete(progressId);
            GameSession.Instance.SaveProgress();
            return true;
        }

        private void Cache()
        {
            if (cached) return;
            cached = true;
            if (movingPart != null) restPosition = movingPart.localPosition;
        }

        private void Update()
        {
            if (!IsMoving) return;
            opening = Mathf.MoveTowards(opening, 1f, Time.deltaTime / openingSeconds);
            Present();
        }

        private void Present()
        {
            Cache();
            // Keep collision closed until the whole stroke finishes; restore/load settles instantly.
            bool clear = IsOpen && opening >= 1f;
            if (movingPart != null)
                movingPart.localPosition = restPosition + openingTravel * Mathf.SmoothStep(0f, 1f, opening);
            foreach (var item in closedObjects) if (item != null) item.SetActive(!clear);
            foreach (var item in openedObjects) if (item != null) item.SetActive(clear);
        }

        public void CaptureProgress(ProgressState state)
        {
            if (IsOpen && !string.IsNullOrWhiteSpace(progressId)) state.Complete(progressId);
        }

        public void RestoreProgress(ProgressState state)
        {
            IsOpen = !string.IsNullOrWhiteSpace(progressId) && state.Has(progressId);
            opening = IsOpen ? 1f : 0f;
            Present();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            opening = IsOpen ? 1f : 0f;
            Present();
        }
    }
}
