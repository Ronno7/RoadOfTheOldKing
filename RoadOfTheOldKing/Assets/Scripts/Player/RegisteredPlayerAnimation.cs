using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Locomotion invokes this once before writing the body; HatchetView consumes its result.
    // Only visual state lives here. Gameplay clocks, collision and possession stay on the weapon.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerCombatController))]
    public sealed class RegisteredPlayerAnimation : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private RegisteredActionSprites forehand, throwing, catching;
        [SerializeField] private AxeSpinSprites spin;
        [SerializeField] private RegisteredAimSprites aimGaits;
        [SerializeField] private RegisteredActionSprites dashSprites;
        [SerializeField] private RegisteredActionSprites sprintSprites;
        private uint dashSequence;
        private bool dashSuppressed;
        private PlayerCombatController combat;
        private PlayerDash dash;
        private Rigidbody2D motor;
        private PlayerMovement movement;
        private HatchetWeapon weapon;
        private SpriteRenderer held;
        private readonly CatchPresentationClock catchClock = new CatchPresentationClock();
        private readonly AimStrideClock aimStride = new AimStrideClock();
        private RegisteredActionSprites.View currentView, catchView, aimView;
        private Sprite currentBody, currentWeapon;
        private uint flightSequence;
        private Vector3 releaseOffset;
        private float flightScale = 1f;
        private int flightStartCel;
        private bool flightFlipX;
        private bool justCaught, moved, caughtDuringThrow;

        public bool IsPresentingBody { get; private set; }
        public bool HasSpin => isActiveAndEnabled && spin != null;
        public int DisplayedCel { get; private set; } = -1;
        public string DisplayedAction { get; private set; } = "Locomotion";
        public AimGait SelectedAimGait => aimStride.Gait;
        public float AimStridePhase => aimStride.Phase;

        private void Awake()
        {
            combat = GetComponent<PlayerCombatController>();
            dash = GetComponent<PlayerDash>();
            motor = GetComponent<Rigidbody2D>();
            movement = GetComponent<PlayerMovement>();
            if (body == null || forehand == null || throwing == null || catching == null || spin == null ||
                !forehand.IsRegistered(out _) || !throwing.IsRegistered(out _) ||
                !catching.IsRegistered(out _) || !spin.IsRegistered(out _))
            {
                Debug.LogError("Registered player animation needs a body and four valid action assets.", this);
                enabled = false;
                return;
            }
            if (dashSprites != null && (!dashSprites.IsRegistered(out _) || dashSprites.celCount != 5 ||
                dashSprites.exposures.Length != 5))
            {
                Debug.LogError("Dash requires five registered cels: three travel and two recovery.", this);
                dashSprites = null;
            }
            if (sprintSprites != null && (!sprintSprites.IsRegistered(out _) || sprintSprites.celCount != 8 ||
                sprintSprites.contactCel != -1 || sprintSprites.possessionCel != -1))
            {
                Debug.LogError("Sprint requires eight registered cels without combat markers.", this);
                sprintSprites = null;
            }
            held = new GameObject("Drawn held axe").AddComponent<SpriteRenderer>();
            held.transform.SetParent(body.transform, false);
            held.sharedMaterial = body.sharedMaterial;
            held.enabled = false;
            if (aimGaits != null && !aimGaits.IsRegistered(out var gaitError))
            {
                Debug.LogError("Invalid aim gait artwork: " + gaitError, this);
                aimGaits = null;
            }
        }

        private void OnEnable()
        {
            if (combat == null) combat = GetComponent<PlayerCombatController>();
            combat.WeaponEquipped += ObserveWeapon;
            ObserveWeapon();
        }

        private void ObserveWeapon()
        {
            if (weapon != null) weapon.ReturnedToHand -= OnReturned;
            weapon = combat.Weapon;
            catchClock.Cancel();
            aimStride.Reset();
            caughtDuringThrow = false;
            if (weapon == null) return;
            weapon.ReturnedToHand += OnReturned;
            flightSequence = 0;
        }

        // Only use a supplied cardinal view in its own sector. Missing views never mirror anatomy.
        private static RegisteredActionSprites.View FindView(RegisteredActionSprites asset, Vector2 direction,
            RegisteredActionSprites.View previous = null)
        {
            direction.Normalize();
            if (previous != null && Vector2.Dot(previous.direction, direction) >= .64f) return previous;
            Vector2 cardinal = Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
                ? (direction.x >= 0 ? Vector2.right : Vector2.left)
                : (direction.y >= 0 ? Vector2.up : Vector2.down);
            foreach (var view in asset.views)
                if (Vector2.Dot(view.direction, cardinal) > .99f) return view;
            return null;
        }

        private bool CanShowCatch => combat.CanContinueAction && (dash == null || !dash.IsDashing) &&
            !moved && (motor == null || motor.linearVelocity.sqrMagnitude < .0025f);

        private void OnReturned()
        {
            catchClock.Cancel();
            caughtDuringThrow = weapon.IsThrowing;
            if (!isActiveAndEnabled || !CanShowCatch) return;
            catchView = FindView(catching, weapon.ReturnApproachDirection);
            if (catchView == null) return;
            catchClock.BeginReach(catching);
            catchClock.ConfirmCatch();
            justCaught = true;
        }

        // Called by the sole body writer. A confirmed grip gets one full presentation frame.
        public void PrepareFrame(float deltaTime, float travelled)
        {
            // Compatibility for isolated action fixtures; live locomotion supplies the actual vector.
            Vector2 direction = motor == null ? Vector2.zero : motor.linearVelocity.normalized;
            if (direction.sqrMagnitude < .0001f) direction = Vector2.right;
            PrepareTravelFrame(deltaTime, direction * travelled, travelled > TeleportThreshold(deltaTime));
        }

        private float TeleportThreshold(float deltaTime) => Mathf.Max(1f,
            (movement == null ? 7.2f : movement.SprintSpeed) * Mathf.Max(0, deltaTime) * 2f);

        public void PrepareTravelFrame(float deltaTime, Vector2 displacement, bool teleported)
        {
            IsPresentingBody = false;
            currentBody = currentWeapon = null;
            DisplayedCel = -1;
            DisplayedAction = "Locomotion";
            if (held != null) held.enabled = false;
            if (!isActiveAndEnabled) return;
            moved = !teleported && displacement.sqrMagnitude > .00000001f;
            if (dash != null && dash.Sequence != dashSequence)
            {
                dashSequence = dash.Sequence;
                dashSuppressed = false;
            }
            if (teleported || !combat.CanContinueAction || movement == null || !movement.isActiveAndEnabled ||
                motor == null || !motor.simulated) dashSuppressed = true;
            if (weapon == null)
            {
                TryPresentDash();
                return;
            }
            bool canAim = combat.CanContinueAction && (dash == null || !dash.IsDashing);
            if (canAim && weapon.CanCancelThrowAim)
                aimStride.Advance(weapon.ActionFacing, displacement, aimGaits == null ? 1.5f : aimGaits.strideLength, teleported);
            else aimStride.Reset();
            if (weapon.FlightSequence != flightSequence)
            {
                flightSequence = weapon.FlightSequence;
                var view = FindView(throwing, weapon.AttackDirection);
                releaseOffset = view == null ? Vector3.up : body.transform.TransformPoint(view.freePropOffset) - transform.position;
                flightScale = view == null ? 1f : view.freePropScale;
                flightStartCel = view == null ? 0 : view.spinStartCel;
                flightFlipX = view != null && view.spinFlipX;
            }
            if (!weapon.IsThrowing || weapon.CanCancelThrowAim) caughtDuringThrow = false;
            bool throwingNow = weapon.IsThrowing && !caughtDuringThrow;
            if (!CanShowCatch || (weapon.State != HatchetState.Held && weapon.State != HatchetState.Returning) || throwingNow ||
                (catchClock.IsActive && !catchClock.HasCaught && weapon.State != HatchetState.Returning))
                catchClock.Cancel();
            if (!justCaught) catchClock.Advance(Mathf.Max(0, deltaTime));
            justCaught = false;

            if (TryPresentDash()) return;
            if (!combat.CanContinueAction || (dash != null && dash.IsDashing)) return;
            if (weapon.State == HatchetState.LightChop && weapon.ComboIndex == 0)
            {
                var view = FindView(forehand, weapon.AttackDirection);
                if (view != null) Select("Forehand", view, forehand.SampleMeleeCel(weapon.AttackProgress,
                    weapon.Settings.lightWindupFraction, weapon.Settings.lightSwingEndFraction, weapon.IsImpactPaused), true);
                return;
            }
            if (throwingNow)
            {
                if (weapon.ThrowPhase == ThrowPhase.Aim && moved && aimGaits != null &&
                    movement != null && movement.isActiveAndEnabled && motor != null && motor.simulated)
                {
                    var gait = aimGaits.FindView(aimStride.Facing, aimStride.Gait);
                    if (gait != null) Select("AimWalk", gait, aimStride.SampleCel(gait.body.Length), true);
                    return;
                }
                // Unsupported moving views retain locomotion. A blocked player can use standing aim.
                if (teleported || moved || (weapon.ThrowPhase != ThrowPhase.Aim && motor != null && motor.linearVelocity.sqrMagnitude > .0025f)) return;
                aimView = FindView(throwing, weapon.CanCancelThrowAim ? aimStride.Facing : weapon.ActionFacing);
                if (aimView != null) Select("Throw", aimView, weapon.ThrowCelIndex, !weapon.IsAway);
                return;
            }
            aimView = null;
            if (weapon.State == HatchetState.Returning && CanShowCatch)
            {
                catchView = FindView(catching, weapon.ReturnApproachDirection, catchView);
                if (catchView != null && !catchClock.IsActive) catchClock.BeginReach(catching);
            }
            if (catchClock.IsActive && catchView != null)
                Select("Catch", catchView, catchClock.CelIndex, catchClock.HasCaught && weapon.State == HatchetState.Held);
        }

        private bool TryPresentDash()
        {
            if (dash == null || dashSprites == null || !dash.isActiveAndEnabled || dashSuppressed) return false;
            bool active = dash.IsDashing;
            if (!active && (moved || motor.linearVelocity.sqrMagnitude > .0025f || combat.IsAttacking || catchClock.IsActive))
            {
                dashSuppressed = true; // Never resume an old recovery after another action or step.
                return false;
            }
            float travelEnd = dashSprites.CelStart(3);
            if (!active && dash.RecoverySeconds >= dashSprites.Duration - travelEnd) return false;
            var dashView = FindView(dashSprites, dash.Direction);
            if (dashView == null) return false;
            int cel = active ? Mathf.Min(2, dashSprites.SampleAtSeconds(dash.Progress * travelEnd))
                : dashSprites.SampleAtSeconds(travelEnd + dash.RecoverySeconds);
            Select("Dash", dashView, cel, weapon != null && weapon.State == HatchetState.Held);
            return true;
        }

        public bool HasSprintView(Vector2 facing) => isActiveAndEnabled && sprintSprites != null &&
            combat.CanContinueAction && !combat.IsAttacking && (dash == null || !dash.IsDashing) &&
            FindView(sprintSprites, facing) != null;

        // Locomotion supplies its shared distance phase after higher-priority actions have yielded.
        public bool TryPresentSprint(Vector2 facing, float phase)
        {
            if (!HasSprintView(facing)) return false;
            var view = FindView(sprintSprites, facing);
            Select("Sprint", view, Mathf.FloorToInt(Mathf.Repeat(phase, 1f) * view.body.Length),
                weapon != null && weapon.State == HatchetState.Held);
            return true;
        }

        private void Select(string action, RegisteredActionSprites.View view, int cel, bool showWeapon)
        {
            if (cel < 0 || cel >= view.body.Length) return;
            currentView = view;
            currentBody = !showWeapon && view.unarmedBody != null && view.unarmedBody.Length == view.body.Length
                ? view.unarmedBody[cel] : view.body[cel];
            currentWeapon = showWeapon ? view.weapon[cel] : null;
            IsPresentingBody = true;
            DisplayedCel = cel;
            DisplayedAction = action;
        }

        public bool TryApplyBody()
        {
            if (!isActiveAndEnabled || !IsPresentingBody) return false;
            body.sprite = currentBody;
            body.flipX = body.flipY = false;
            held.sprite = currentWeapon;
            held.color = body.color;
            held.sortingLayerID = body.sortingLayerID;
            held.sortingOrder = body.sortingOrder + 1;
            held.enabled = currentWeapon != null && body.enabled;
            return true;
        }

        public bool TryApplyWeapon(Transform model, SpriteRenderer blade)
        {
            if (!isActiveAndEnabled || weapon == null || model == null || blade == null) return false;
            if (!weapon.IsAway)
            {
                if (!IsPresentingBody) return false;
                blade.enabled = false;
                return true;
            }
            int cel = spin.Sample(weapon.FlightSeconds, flightStartCel);
            Vector3 offset = Vector3.Lerp(releaseOffset, Vector3.up,
                Mathf.SmoothStep(0f, 1f, weapon.FlightSeconds / .3f));
            float scale = flightScale;
            bool flipX = flightFlipX;
            if (weapon.State == HatchetState.Stuck)
                offset = Vector3.zero; // Impact/ground retrieval stays at the collision landmark.
            if (weapon.State == HatchetState.Returning)
            {
                float distance = Vector2.Distance(weapon.transform.position, transform.position);
                float approach = 1f - Mathf.Clamp01(distance / 2f);
                var view = IsPresentingBody && DisplayedAction == "Catch" ? currentView : null;
                Vector3 targetOffset = view == null ? Vector3.up : body.transform.TransformPoint(view.freePropOffset) - transform.position;
                offset = Vector3.Lerp(Vector3.up, targetOffset, approach);
                if (view != null)
                {
                    scale = Mathf.Lerp(flightScale, view.freePropScale, approach);
                    float timeToArrival = Mathf.Max(0, distance - .1f) / weapon.Settings.recallSpeed;
                    if (timeToArrival <= .0625f)
                    {
                        cel = timeToArrival > .03125f ? (view.spinStartCel + 7) % 8 : view.spinStartCel;
                        flipX = view.spinFlipX;
                    }
                }
            }
            model.SetPositionAndRotation(weapon.transform.position + offset, Quaternion.identity);
            blade.sprite = spin.cels[cel];
            // Match the visible blade side of the authored release; body and physics never mirror.
            blade.flipX = flipX;
            var parentScale = blade.transform.parent.lossyScale;
            var size = body.transform.lossyScale * scale;
            blade.transform.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, size.z / parentScale.z);
            blade.sortingLayerID = body.sortingLayerID;
            blade.sortingOrder = body.sortingOrder + 1;
            return true;
        }

        private void OnDisable()
        {
            if (combat != null) combat.WeaponEquipped -= ObserveWeapon;
            if (weapon != null) weapon.ReturnedToHand -= OnReturned;
            catchClock.Cancel();
            aimStride.Reset();
            IsPresentingBody = false;
            if (held != null) held.enabled = false;
        }
        private void OnDestroy() { if (held != null) Destroy(held.gameObject); }
    }
}
