using TheLostShrine.Combat;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Player
{
    // The only writer of the body sprite. It samples gameplay state and never drives it:
    // walk/run follow distance travelled, attacks follow the weapon's action clock, the dash and
    // the flask drink follow their own progress. An empty slot falls back: run -> walk, dash -> run/walk,
    // actions and reactions -> locomotion, idle -> rotations.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerSpriteAnimator : MonoBehaviour
    {
        private static readonly string[] AttackStates = { "LightAttack1", "LightAttack2", "Finisher" };

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private PlayerAnimationSet animations;
        [Tooltip("Ground distance covered by one complete walk cycle, so feet never slide.")]
        [SerializeField, Min(0.1f)] private float walkCycleLength = 3f;
        [SerializeField, Min(0.1f)] private float runCycleLength = 4.5f;
        [Tooltip("Keys released a moment apart should still leave the player facing the diagonal.")]
        [SerializeField, Min(0f)] private float diagonalReleaseGrace = 0.1f;

        private PlayerMovement movement;
        private PlayerDash dash;
        private PlayerFlask flask;
        private PlayerBonfireInteraction bonfire;
        private PlayerCombatController combat;
        private HitReaction reaction;
        private Damageable health;
        private Rigidbody2D motor;
        private AxeWeapon weapon;
        private Vector2 previousPosition;
        private float frameDelta, stateTime, cycle, catchRemaining, heldSince;
        private int heldOctant = 6, previousOctant = 6, restingOctant = 6, catchOctant = 6;
        private bool wasWalking, wasResting;

        public string State { get; private set; } = "Idle";
        public int Octant { get; private set; } = 6;
        public int Frame { get; private set; }
        public PlayerAnimationSet Animations => animations;

        // Pixels the shown figure stands above (+) or below (-) its standing view, so carried items
        // bob with the step. Zero when either height was not measured.
        public int Lift
        {
            get
            {
                if (currentClip == null || animations == null || animations.rotations == null) return 0;
                int now = currentClip.Height(Octant, Frame), rest = animations.rotations.Height(Octant, 0);
                return now < 0 || rest < 0 ? 0 : Mathf.Clamp(now - rest, -2, 2);
            }
        }

        private DirectionalSpriteAnimation currentClip;
        // The flinch outlasts the brief stagger: it finishes unless the player moves, dashes or attacks.
        private float hurtUntil;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            dash = GetComponent<PlayerDash>();
            flask = GetComponent<PlayerFlask>();
            bonfire = GetComponent<PlayerBonfireInteraction>();
            combat = GetComponent<PlayerCombatController>();
            reaction = GetComponent<HitReaction>();
            health = GetComponent<Damageable>();
            motor = GetComponent<Rigidbody2D>();
            if (body == null || animations == null || animations.rotations == null || animations.rotations.IsEmpty)
            {
                Debug.LogError("Player sprite animation needs a body renderer and an animation set with rotations.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            cycle = catchRemaining = 0f;
            wasWalking = wasResting = false;
            hurtUntil = 0f;
            if (health != null) health.HitReceived += OnHurt;
            if (combat == null) return;
            combat.WeaponEquipped += ObserveWeapon;
            ObserveWeapon();
        }

        private void OnDisable()
        {
            if (health != null) health.HitReceived -= OnHurt;
            if (combat != null) combat.WeaponEquipped -= ObserveWeapon;
            if (weapon != null) weapon.ReturnedToHand -= OnReturned;
            weapon = null;
        }

        private void ObserveWeapon()
        {
            if (weapon != null) weapon.ReturnedToHand -= OnReturned;
            weapon = combat.Weapon;
            catchRemaining = 0f;
            if (weapon != null) weapon.ReturnedToHand += OnReturned;
        }

        private void OnHurt(CombatHit hit)
        {
            var clip = animations != null ? animations.hurt : null;
            if (clip != null && !clip.IsEmpty) hurtUntil = Time.time + clip.Duration(Octant);
        }

        private void OnReturned()
        {
            var clip = animations.catching;
            if (clip == null || clip.IsEmpty || weapon.IsThrowing) return;
            catchOctant = DirectionalSpriteAnimation.Octant(weapon.ReturnApproachDirection);
            catchRemaining = clip.Duration(catchOctant);
        }

        private void LateUpdate()
        {
            frameDelta = Time.deltaTime;
            Vector2 position = transform.position;
            Vector2 displacement = position - previousPosition;
            previousPosition = position;
            if (frameDelta <= 0f)
                return;

            float distance = displacement.magnitude;
            // A respawn or teleport must not fast-forward the feet through many strides.
            bool teleported = distance > Mathf.Max(1f, movement.SprintSpeed * frameDelta * 2f);
            bool staggered = reaction != null && reaction.IsStaggered;
            bool canMove = movement.isActiveAndEnabled && motor.simulated && !staggered;
            bool walking = canMove && !teleported && distance > 0.0001f && motor.linearVelocity.sqrMagnitude > 0.0025f;
            int facing = UpdateFacing(!walking);
            catchRemaining = walking ? 0f : Mathf.Max(0f, catchRemaining - frameDelta);

            if (health != null && !health.IsAlive && PlayTimed("Death", animations.death, facing)) return;
            bool flinching = Time.time < hurtUntil && !walking && (dash == null || !dash.IsDashing) &&
                (weapon == null || !weapon.IsAttacking);
            if ((staggered || flinching) && PlayTimed("Hurt", animations.hurt, facing)) return;
            // Rest is presentation of the existing menu/lock, never a new gameplay commitment.
            // Face the fire without changing movement facing or replaying the loop on menu pages.
            if (bonfire != null && bonfire.IsOpen && health != null && health.IsAlive && !staggered)
            {
                Vector2 toFire = bonfire.ActiveFire.transform.position - transform.position;
                int restFacing = toFire.sqrMagnitude > 0.0001f ? DirectionalSpriteAnimation.Octant(toFire) : facing;
                if (PlayTimed("Rest", animations.rest, restFacing))
                {
                    wasWalking = false;
                    catchRemaining = 0f;
                    return;
                }
            }
            // The swig (contact frame) lands with the heal; drinking blocks attacks and dodges.
            if (flask != null && flask.IsDrinking &&
                Play("Drink", animations.drink, facing, flask.Progress, true, flask.HealAt)) return;
            if (dash != null && dash.IsDashing)
            {
                int octant = DirectionalSpriteAnimation.Octant(dash.Direction);
                if (Play("Dash", animations.dash, octant, dash.Progress, true)) return;
                PlayLocomotion(octant, true, distance);
                return;
            }
            if (weapon != null && combat.CanContinueAction && TryPlayWeapon()) return;
            if (catchRemaining > 0f && PlayTimed("Catch", animations.catching, catchOctant)) return;
            PlayLocomotion(facing, walking, distance);
        }

        private bool TryPlayWeapon()
        {
            int octant = DirectionalSpriteAnimation.Octant(combat.ActionFacing);
            switch (weapon.State)
            {
                case AxeState.LightChop:
                    var attack = weapon.ComboIndex == 0 ? animations.lightAttack1
                        : weapon.ComboIndex == 1 ? animations.lightAttack2 : animations.finisher;
                    return Play(AttackStates[Mathf.Clamp(weapon.ComboIndex, 0, 2)], attack, octant,
                        weapon.AttackProgress, true, weapon.LightWindupFraction);
                case AxeState.Charging:
                    return PlayTimed("Charge", animations.charge, octant);
                case AxeState.Cleaving:
                    return Play("Cleave", animations.cleave, octant, weapon.AttackProgress, true);
            }
            if (!weapon.IsThrowing)
                return false;
            if (weapon.ThrowPhase == ThrowPhase.Release)
                return Play("Throw", animations.throwRelease, octant, weapon.ThrowPhaseProgress, true, weapon.ThrowLaunchFraction);
            // Preparation and held aim: the aim loop, or the release's wind-up frame without one.
            return PlayTimed("ThrowAim", animations.throwAim, octant) ||
                Play("ThrowAim", animations.throwRelease, octant, 0f, true);
        }

        private void PlayLocomotion(int octant, bool walking, float distance)
        {
            if (walking)
            {
                bool running = movement.IsSprinting && HasFrames(animations.run);
                var clip = running ? animations.run : animations.walk;
                if (HasFrames(clip))
                {
                    // Start from the first drawn step; keep the phase through walk/run/turn changes.
                    if (!wasWalking) cycle = 0f;
                    cycle = Mathf.Repeat(cycle + distance / (running ? runCycleLength : walkCycleLength), 1f);
                    wasWalking = true;
                    Play(running ? "Run" : "Walk", clip, octant, cycle);
                    return;
                }
            }
            wasWalking = false;
            if (!PlayTimed("Idle", animations.idle, octant))
                Play("Idle", animations.rotations, octant, 0f);
        }

        // Standing facing follows the last movement, except that a diagonal decaying to its
        // neighbouring cardinal as keys are released a moment apart keeps the diagonal.
        private int UpdateFacing(bool resting)
        {
            int octant = DirectionalSpriteAnimation.Octant(movement.FacingDirection);
            if (!resting)
            {
                if (octant != heldOctant)
                {
                    previousOctant = heldOctant;
                    heldOctant = octant;
                    heldSince = Time.time;
                }
                wasResting = false;
                return octant;
            }
            if (!wasResting)
            {
                bool releasedDiagonal = previousOctant % 2 == 1 && octant % 2 == 0 && octant == heldOctant &&
                    Mathf.Abs(Mathf.DeltaAngle(previousOctant * 45f, octant * 45f)) <= 45f &&
                    Time.time - heldSince <= diagonalReleaseGrace;
                restingOctant = releasedDiagonal ? previousOctant : octant;
            }
            else if (octant != heldOctant)
                restingOctant = octant; // Turned in place, for example by attacking.
            heldOctant = octant;
            wasResting = true;
            return restingOctant;
        }

        private static bool HasFrames(DirectionalSpriteAnimation clip) => clip != null && !clip.IsEmpty;

        private float TimeIn(string state) => state == State ? stateTime + frameDelta : 0f;

        private bool PlayTimed(string state, DirectionalSpriteAnimation clip, int octant) =>
            HasFrames(clip) && Play(state, clip, octant, TimeIn(state) / Mathf.Max(0.0001f, clip.Duration(octant)));

        private bool Play(string state, DirectionalSpriteAnimation clip, int octant, float normalized,
            bool action = false, float contactProgress = 0f)
        {
            if (!HasFrames(clip))
                return false;
            int frame;
            var sprite = action ? clip.SampleAction(octant, normalized, contactProgress, out frame)
                : clip.Sample(octant, normalized, out frame);
            if (sprite == null)
                return false;
            stateTime = TimeIn(state);
            State = state;
            Octant = octant;
            Frame = frame;
            currentClip = clip;
            body.sprite = sprite;
            body.flipX = false;
            return true;
        }
    }
}
