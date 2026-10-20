using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum SluiceStage { High, DistributorReleased, ServiceDiverted, PondLow }

    [DisallowMultipleComponent, DefaultExecutionOrder(-60)]
    public sealed class RecallSluice : MonoBehaviour, IProgressParticipant, IResetOnRest
    {
        [SerializeField] private string puzzleId = "green-lowlands/sluice";
        [SerializeField] private Transform carriage;
        [SerializeField] private Vector3 loadingPosition;
        [SerializeField] private Vector3 drawingPosition;
        [SerializeField, Min(.1f)] private float carriageSpeed = 4f;
        [SerializeField] private GameObject distributorBolt;
        [SerializeField] private GameObject serviceWater;
        [SerializeField] private GameObject highWater;
        [SerializeField] private GameObject exposedBasin;
        [SerializeField] private Transform brake;
        [SerializeField] private SluiceMechanismView mechanismView;
        private Vector3 brakeRest;
        private AxeWeapon attempt;
        private uint attemptFlight;
        private SluicePart attemptFork;
        private bool drawing;
        private bool brakeLifted;
        private SluiceFeedback actionFeedback;

        public SluiceStage Stage { get; private set; }
        public bool IsDrawing => drawing && !IsMoving;
        public bool IsMoving => carriage != null &&
            (carriage.localPosition - (drawing ? drawingPosition : loadingPosition)).sqrMagnitude > .0001f;
        public bool BrakeLifted => brakeLifted;
        public string LowWaterId => puzzleId + "/pond-low";
        public string Feedback { get; private set; } = "";
        public event System.Action Changed;

        // Presentation follows the owned, current throw, never a historical fork hit.
        public bool IsForkArmed(SluicePart fork) => attemptFork == fork && ValidAttempt() &&
            attempt.State == AxeState.Stuck;

        private void Awake()
        {
            actionFeedback = GetComponent<SluiceFeedback>();
            if (brake != null) brakeRest = brake.localPosition;
            ApplyState();
        }

        public void Respond(SluiceResponse response, Transform source)
        {
            if (actionFeedback != null) actionFeedback.Respond(response, source);
        }

        private void FixedUpdate()
        {
            if (carriage != null)
                carriage.localPosition = Vector3.MoveTowards(carriage.localPosition,
                    drawing ? drawingPosition : loadingPosition, carriageSpeed * Time.fixedDeltaTime);
            if (attempt != null && !ValidAttempt()) ClearAttempt();
        }

        private bool ValidAttempt() => attempt != null && attempt.isActiveAndEnabled &&
            attempt.Owner != null && attempt.Owner.isActiveAndEnabled && attempt.Owner.Weapon == attempt &&
            attempt.Owner.TryGetComponent<PlayerHealth>(out var health) && health.IsAlive && attempt.FlightSequence == attemptFlight &&
            (attempt.State == AxeState.Stuck || attempt.State == AxeState.Returning);

        // This entry is called by AxeWeapon only after assigning its real lodged transform.
        public void Lodge(SluicePart fork, AxeWeapon weapon)
        {
            ClearAttempt();
            if (!isActiveAndEnabled || weapon == null || weapon.State != AxeState.Stuck ||
                weapon.Owner == null || weapon.Owner.Weapon != weapon) return;
            if (!((fork == SluicePart.DistributorFork && Stage == SluiceStage.High) ||
                  (fork == SluicePart.MainFork && Stage == SluiceStage.ServiceDiverted && !drawing && !IsMoving))) return;
            attempt = weapon;
            attemptFlight = weapon.FlightSequence;
            attemptFork = fork;
            Feedback = "The blade settles into the fork.";
            Changed?.Invoke();
        }

        public bool ReceiveVane(SluicePart part, CombatHit hit)
        {
            if (!isActiveAndEnabled || hit.Kind != AttackKind.Recall || !ValidAttempt() ||
                hit.Source != attempt.Owner.gameObject || attempt.State != AxeState.Returning) return false;
            if (part == SluicePart.DistributorVane && attemptFork == SluicePart.DistributorFork && Stage == SluiceStage.High)
            {
                CompleteStage(SluiceStage.DistributorReleased, "The locking bolt slides clear.");
                return true;
            }
            if (attemptFork != SluicePart.MainFork || Stage != SluiceStage.ServiceDiverted || !IsDrawing) return false;
            if (part == SluicePart.BrakeVane)
            {
                brakeLifted = true;
                Feedback = "The brake lifts.";
                ApplyBrake();
                Changed?.Invoke();
                return true;
            }
            if (part != SluicePart.CatchVane) return false;
            if (!brakeLifted)
            {
                Feedback = "The catch strains against the brake.";
                if (mechanismView != null) mechanismView.Strain();
                Changed?.Invoke();
                return false;
            }
            CompleteStage(SluiceStage.PondLow, "The counterweight falls. Water leaves the basin.");
            return true;
        }

        public bool CanTurn(bool moveCarriage)
        {
            if (!isActiveAndEnabled || GameSession.Instance == null || GameSession.Instance.IsLoading) return false;
            return moveCarriage ? Stage == SluiceStage.ServiceDiverted && !IsMoving &&
                (attempt == null || attempt.State != AxeState.Returning) : Stage == SluiceStage.DistributorReleased;
        }

        public bool Turn(bool moveCarriage)
        {
            if (!CanTurn(moveCarriage)) return false;
            if (!moveCarriage)
                CompleteStage(SluiceStage.ServiceDiverted, "The service channel empties.");
            else
            {
                drawing = !drawing;
                Feedback = "The carriage rolls along its rails.";
                Changed?.Invoke();
            }
            return true;
        }

        private void CompleteStage(SluiceStage next, string feedback)
        {
            Stage = next;
            ClearAttempt();
            Feedback = feedback;
            // One persistent outcome owns water, collision and revealed geometry.
            // Save before the reveal; a load restores the completed configuration.
            if (GameSession.Instance != null) GameSession.Instance.SaveProgress();
            ApplyState(true);
            Changed?.Invoke();
        }

        private void ClearAttempt()
        {
            attempt = null;
            brakeLifted = false;
            ApplyBrake();
        }

        private void ApplyBrake()
        {
            if (mechanismView != null)
            {
                mechanismView.SetBrake(brakeLifted || Stage == SluiceStage.PondLow);
                return;
            }
            if (brake != null) brake.localPosition = brakeRest +
                (brakeLifted || Stage == SluiceStage.PondLow ? Vector3.up * .35f : Vector3.zero);
        }

        private static void SetWater(GameObject water, bool visible, bool animate)
        {
            if (water == null) return;
            if (water.TryGetComponent<SluiceWaterReveal>(out var reveal))
                reveal.SetVisible(visible, animate);
            else water.SetActive(visible);
        }

        private void ApplyState(bool animate = false)
        {
            if (mechanismView != null) mechanismView.Present(Stage, brakeLifted, animate);
            else if (distributorBolt != null) distributorBolt.SetActive(Stage == SluiceStage.High);
            SetWater(serviceWater, Stage < SluiceStage.ServiceDiverted, animate && Stage == SluiceStage.ServiceDiverted);
            SetWater(highWater, Stage < SluiceStage.PondLow, animate && Stage == SluiceStage.PondLow);
            if (exposedBasin != null) exposedBasin.SetActive(Stage == SluiceStage.PondLow);
            ApplyBrake();
        }

        public void CaptureProgress(ProgressState state)
        {
            if (Stage >= SluiceStage.DistributorReleased) state.Complete(puzzleId + "/distributor");
            if (Stage >= SluiceStage.ServiceDiverted) state.Complete(puzzleId + "/service");
            if (Stage == SluiceStage.PondLow) state.Complete(LowWaterId);
        }

        public void RestoreProgress(ProgressState state)
        {
            Stage = state.Has(LowWaterId) ? SluiceStage.PondLow : state.Has(puzzleId + "/service")
                ? SluiceStage.ServiceDiverted : state.Has(puzzleId + "/distributor")
                ? SluiceStage.DistributorReleased : SluiceStage.High;
            ResetOnRest();
        }

        public void ResetOnRest()
        {
            if (attempt != null) attempt.CancelAction();
            ClearAttempt();
            drawing = Stage == SluiceStage.PondLow;
            if (carriage != null) carriage.localPosition = drawing ? drawingPosition : loadingPosition;
            Feedback = "";
            ApplyState();
            if (actionFeedback != null) actionFeedback.ResetPresentation();
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            ClearAttempt();
            if (actionFeedback != null) actionFeedback.ResetPresentation();
        }
    }
}
