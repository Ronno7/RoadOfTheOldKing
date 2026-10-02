using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // Presentation only. WolfAI owns movement, attack commitment, damage and reset; this samples its
    // state: locomotion follows distance travelled (stalk while circling, walk to reposition, run to
    // chase), the bite follows the windup clock so the jaws open on the leap or snap, death plays once
    // and holds as the corpse. Draws the attack telegraph: a lane for the lunge, an arc for the snap.
    [DisallowMultipleComponent, RequireComponent(typeof(WolfAI), typeof(Damageable))]
    public sealed class WolfView : MonoBehaviour
    {
        [SerializeField] private WolfAnimationSet animations;
        [SerializeField] private SpriteRenderer bodyVisual;
        [SerializeField] private LineRenderer attackOutline;
        [Tooltip("Degrees past an octant's edge before facing switches, so circling doesn't flicker.")]
        [SerializeField, Range(0f, 20f)] private float facingHysteresis = 8f;
        private WolfAI wolf;
        private Damageable health;
        private Vector2 previousPosition;
        private float gait, speed, idleTime, hurtTime, deathTime, flashUntil;
        private bool wasAlive;
        private int octant = 6, resetVersion;
        private MaterialPropertyBlock flashProperties;

        public string State { get; private set; } = "Idle";
        public int Octant => octant;

        private void Awake()
        {
            flashProperties = new MaterialPropertyBlock();
            wolf = GetComponent<WolfAI>();
            health = GetComponent<Damageable>();
            if (attackOutline != null)
            {
                var properties = new MaterialPropertyBlock();
                properties.SetTexture("_MainTex", Texture2D.whiteTexture);
                attackOutline.SetPropertyBlock(properties);
            }
        }

        private void OnEnable()
        {
            health.HitReceived += OnHit;
            ResetPresentation();
        }

        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            if (attackOutline != null) attackOutline.enabled = false;
            SetFlash(false);
        }

        private void ResetPresentation()
        {
            previousPosition = transform.position;
            wasAlive = health.IsAlive;
            resetVersion = wolf.ResetVersion;
            gait = speed = idleTime = hurtTime = deathTime = flashUntil = 0f;
        }

        private void OnHit(CombatHit hit)
        {
            hurtTime = 0f;
            if (animations != null) flashUntil = Time.time + animations.hitFlashDuration;
        }

        private void LateUpdate()
        {
            if (animations == null || bodyVisual == null) return;
            if (resetVersion != wolf.ResetVersion || (!wasAlive && health.IsAlive)) ResetPresentation();

            float dt = Time.deltaTime;
            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            if (distance > 1.5f) distance = 0f; // teleport or reset
            // Physics may not step every render frame; smooth the measured speed.
            if (dt > 0f) speed = Mathf.Lerp(speed, distance / dt, 1f - Mathf.Exp(-12f * dt));
            hurtTime += dt;

            if (health.IsAlive) UpdateFacing(wolf.FacingDirection);
            bodyVisual.sprite = SelectSprite(distance, dt);
            bodyVisual.flipX = false;
            // Without death art, fade the body out instead of leaving a standing corpse.
            float alpha = health.IsAlive || Has(animations.death) ? 1f : 1f - Mathf.Clamp01(deathTime / animations.deathFadeDuration);
            bodyVisual.color = new Color(1f, 1f, 1f, alpha);
            SetFlash(Time.time < flashUntil);
            wasAlive = health.IsAlive;
            DrawTelegraph();
        }

        private Sprite SelectSprite(float distance, float dt)
        {
            switch (wolf.State)
            {
                case WolfState.Defeated:
                    if (wasAlive) deathTime = 0f;
                    deathTime += dt;
                    return Play("Death", animations.death, deathTime);
                case WolfState.Windup:
                    // Lunge: lowered stalk frames while tracking (held through delayed windups), the
                    // deep crouch once the lane locks. Snap: the whole windup maps onto the frames.
                    if (wolf.IsLungeAttack)
                        return Bite(wolf.IsLaneLocked ? Contact() - 1 : Mathf.Min(Contact() - 2, Mathf.FloorToInt(wolf.TrackProgress * (Contact() - 1))));
                    return Bite(Mathf.FloorToInt(wolf.StateProgress * Contact()));
                case WolfState.Evade:
                    return Bite(Mathf.Max(0, Contact() - 2)); // a low defensive hop
                case WolfState.Lunge:
                case WolfState.Snap:
                    return Bite(Contact());
                case WolfState.Recover:
                    float share = Mathf.Max(.01f, animations.biteRecoveryShare);
                    if (Has(animations.bite) && wolf.StateProgress < share)
                        return Bite(Contact() + 1 + Mathf.FloorToInt(wolf.StateProgress / share * (animations.bite.FrameCount(octant) - Contact() - 1)));
                    return Idle(dt);
                case WolfState.Staggered:
                    if (Has(animations.hurt) && hurtTime < animations.hurt.Duration(octant))
                        return Play("Hurt", animations.hurt, hurtTime);
                    return Idle(dt);
                case WolfState.Chase:
                    return Locomotion("Run", animations.run, animations.runCycleLength, distance, dt);
                case WolfState.Stalk:
                    // Rushing an unarmed player happens at a run.
                    if (speed >= animations.runAnimationSpeed)
                        return Locomotion("Run", animations.run, animations.runCycleLength, distance, dt);
                    return Locomotion("Stalk", animations.stalk, animations.stalkCycleLength, distance, dt);
                case WolfState.Reposition:
                case WolfState.Return:
                    return Locomotion("Walk", animations.walk, animations.walkCycleLength, distance, dt);
                default:
                    return Idle(dt);
            }
        }

        private int Contact()
        {
            var bite = animations.bite;
            int count = Has(bite) ? bite.FrameCount(octant) : 1;
            return bite != null && bite.contactFrame > 0 && bite.contactFrame < count ? bite.contactFrame : Mathf.RoundToInt(count * .6f);
        }

        private Sprite Bite(int frame)
        {
            if (!Has(animations.bite)) return Idle(0f);
            State = "Bite";
            return animations.bite.Frame(octant, Mathf.Clamp(frame, 0, animations.bite.FrameCount(octant) - 1));
        }

        // Distance-driven gait; missing clips fall back stalk/run -> walk; standing still shows idle.
        private Sprite Locomotion(string state, DirectionalSpriteAnimation clip, float cycleLength, float distance, float dt)
        {
            if (!Has(clip)) { clip = animations.walk; cycleLength = animations.walkCycleLength; state = "Walk"; }
            if (!Has(clip) || speed < .2f) return Idle(dt);
            gait = Mathf.Repeat(gait + distance / cycleLength, 1f);
            State = state;
            return clip.Sample(octant, gait, out _);
        }

        private Sprite Idle(float dt)
        {
            idleTime += dt;
            if (Has(animations.idle)) return Play("Idle", animations.idle, idleTime);
            State = "Idle";
            return animations.rotations != null ? animations.rotations.Frame(octant, 0) : bodyVisual.sprite;
        }

        // Time-driven playback; non-looping clips hold their last frame.
        private Sprite Play(string state, DirectionalSpriteAnimation clip, float time)
        {
            State = state;
            if (!Has(clip))
                return animations.rotations != null ? animations.rotations.Frame(octant, 0) : bodyVisual.sprite;
            float clipDuration = clip.Duration(octant);
            return clip.Sample(octant, clipDuration > 0f ? time / clipDuration : 0f, out _);
        }

        private static bool Has(DirectionalSpriteAnimation clip) => clip != null && !clip.IsEmpty;

        private void UpdateFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < .001f) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float fromCurrent = Mathf.Abs(Mathf.DeltaAngle(angle, octant * 45f));
            if (fromCurrent > 22.5f + facingHysteresis) octant = DirectionalSpriteAnimation.Octant(direction);
        }

        private void SetFlash(bool active)
        {
            if (bodyVisual == null || flashProperties == null) return;
            bodyVisual.GetPropertyBlock(flashProperties);
            flashProperties.SetFloat("_FlashAmount", active ? 1f : 0f);
            bodyVisual.SetPropertyBlock(flashProperties);
        }

        // Windup: faint while still tracking, firmer once the lane locks; active: red.
        private void DrawTelegraph()
        {
            if (attackOutline == null) return;
            var state = wolf.State;
            // Feints telegraph exactly like a real lunge until they break off.
            bool windup = state == WolfState.Windup;
            bool active = state == WolfState.Lunge || state == WolfState.Snap;
            attackOutline.enabled = health.IsAlive && (windup || active);
            if (!attackOutline.enabled) return;
            Color color = active ? new Color(.95f, .35f, .25f, .7f)
                : wolf.IsLaneLocked || !wolf.IsLungeAttack ? new Color(.92f, .6f, .3f, .55f) : new Color(.85f, .58f, .30f, .3f);
            attackOutline.startColor = attackOutline.endColor = color;
            // One world pixel while tracking, two once committed: the lock is the cue to dodge.
            const float Pixel = 1f / 16f;
            attackOutline.widthMultiplier = 1f;
            attackOutline.startWidth = attackOutline.endWidth = active || wolf.IsLaneLocked || !wolf.IsLungeAttack ? 2f * Pixel : Pixel;
            attackOutline.sortingLayerID = bodyVisual.sortingLayerID;
            attackOutline.sortingOrder = bodyVisual.sortingOrder - 1;
            Vector2 dir = wolf.AttackDirection;
            float z = transform.position.z;
            if (wolf.IsLungeAttack)
            {
                Vector2 origin = wolf.AttackOrigin;
                Vector2 side = new Vector2(-dir.y, dir.x) * (wolf.LaneWidth * .5f);
                Vector2 end = origin + dir * wolf.LaneLength;
                attackOutline.positionCount = 5;
                attackOutline.SetPosition(0, new Vector3(origin.x + side.x, origin.y + side.y, z));
                attackOutline.SetPosition(1, new Vector3(end.x + side.x, end.y + side.y, z));
                attackOutline.SetPosition(2, new Vector3(end.x - side.x, end.y - side.y, z));
                attackOutline.SetPosition(3, new Vector3(origin.x - side.x, origin.y - side.y, z));
                attackOutline.SetPosition(4, new Vector3(origin.x + side.x, origin.y + side.y, z));
                return;
            }
            const int segments = 16;
            attackOutline.positionCount = segments + 3;
            attackOutline.SetPosition(0, transform.position);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= segments; i++)
            {
                float a = (angle - wolf.SnapArc * .5f + wolf.SnapArc * i / segments) * Mathf.Deg2Rad;
                attackOutline.SetPosition(i + 1, transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * wolf.SnapReach);
            }
            attackOutline.SetPosition(segments + 2, transform.position);
        }
    }
}
