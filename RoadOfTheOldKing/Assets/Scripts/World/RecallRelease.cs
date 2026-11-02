using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The sluice's verb in a single step: lodge the owned throw in a receiver, then route that same
    // axe's Recall through a release. The Hayfields hay-loading beam uses it to fall across the breach.
    // The outcome is saved before it plays; a load or rest settles it instantly.
    [DisallowMultipleComponent]
    public sealed class RecallRelease : MonoBehaviour, IProgressParticipant, IResetOnRest
    {
        [SerializeField] private string progressId;
        [SerializeField] private GameObject[] closedObjects = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] openedObjects = System.Array.Empty<GameObject>();
        [SerializeField] private Transform movingPart;
        [SerializeField] private float releasedAngle = -90f;
        [SerializeField, Min(.05f)] private float releaseSeconds = .5f;
        private AxeWeapon attempt;
        private uint attemptFlight;
        private Quaternion restRotation;
        private bool cached;
        private float release;

        public bool IsReleased { get; private set; }
        public bool IsMoving => IsReleased && release < 1f;
        public float ReleaseProgress => IsReleased ? release : 0f;
        public bool IsArmed => ValidAttempt() && attempt.State == AxeState.Stuck;
        public string ProgressId => progressId;
        public event System.Action Changed;

        private bool ValidAttempt() => attempt != null && attempt.isActiveAndEnabled &&
            attempt.Owner != null && attempt.Owner.isActiveAndEnabled && attempt.Owner.Weapon == attempt &&
            attempt.Owner.TryGetComponent<PlayerHealth>(out var health) && health.IsAlive &&
            attempt.FlightSequence == attemptFlight &&
            (attempt.State == AxeState.Stuck || attempt.State == AxeState.Returning);

        // Called by the receiver only after the axe has genuinely lodged in it.
        public void Lodge(AxeWeapon weapon)
        {
            attempt = null;
            if (IsReleased || !isActiveAndEnabled || weapon == null || weapon.State != AxeState.Stuck ||
                weapon.Owner == null || weapon.Owner.Weapon != weapon) return;
            attempt = weapon;
            attemptFlight = weapon.FlightSequence;
            Changed?.Invoke();
        }

        public bool ReceiveRelease(CombatHit hit)
        {
            if (IsReleased || !isActiveAndEnabled || string.IsNullOrWhiteSpace(progressId) ||
                hit.Kind != AttackKind.Recall || !ValidAttempt() ||
                hit.Source != attempt.Owner.gameObject || attempt.State != AxeState.Returning) return false;
            attempt = null;
            IsReleased = true;
            release = 0f;
            if (GameSession.Instance != null)
            {
                GameSession.Instance.Progress.Complete(progressId);
                GameSession.Instance.SaveProgress();
            }
            Changed?.Invoke();
            return true;
        }

        private void Cache()
        {
            if (cached) return;
            cached = true;
            if (movingPart != null) restRotation = movingPart.localRotation;
        }

        private void FixedUpdate()
        {
            if (attempt != null && !ValidAttempt()) attempt = null;
            if (!IsMoving) return;
            release = Mathf.MoveTowards(release, 1f, Time.fixedDeltaTime / releaseSeconds);
            Present();
        }

        private void Present()
        {
            Cache();
            // Collision stays closed until the beam has landed; restore and load settle instantly.
            bool open = IsReleased && release >= 1f;
            if (movingPart != null)
                movingPart.localRotation = restRotation * Quaternion.Euler(0f, 0f, releasedAngle * Mathf.SmoothStep(0f, 1f, IsReleased ? release : 0f));
            foreach (var item in closedObjects) if (item != null) item.SetActive(!open);
            foreach (var item in openedObjects) if (item != null) item.SetActive(open);
        }

        public void CaptureProgress(ProgressState state)
        {
            if (IsReleased && !string.IsNullOrWhiteSpace(progressId)) state.Complete(progressId);
        }

        public void RestoreProgress(ProgressState state)
        {
            IsReleased = !string.IsNullOrWhiteSpace(progressId) && state.Has(progressId);
            release = IsReleased ? 1f : 0f;
            attempt = null;
            Present();
            Changed?.Invoke();
        }

        public void ResetOnRest()
        {
            attempt = null;
            if (IsReleased) release = 1f;
            Present();
        }

        private void OnDisable()
        {
            attempt = null;
            release = IsReleased ? 1f : 0f;
            Present();
        }
    }
}
