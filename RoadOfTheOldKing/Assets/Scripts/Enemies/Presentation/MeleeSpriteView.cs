using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Native directional art over the existing brain; missing attack/death clips stay explicit fallbacks.
    [RequireComponent(typeof(PursuitBrain), typeof(Damageable), typeof(EnemyCorpseSorting))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "MeleeEnemySpriteView")]
    public sealed class MeleeSpriteView : MonoBehaviour
    {
        [SerializeField] private EnemyAnimationSet animations;
        [SerializeField] private SpriteRenderer bodyVisual;
        public EnemyAnimationSet Animations => animations;
        private PursuitBrain enemy;
        private Damageable health;
        private Vector2 previous;
        private float gait, idleTime, deathTime, flashUntil;
        private int octant = 6, resetVersion;
        private bool wasAlive, wasStriking;
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            enemy = GetComponent<PursuitBrain>(); health = GetComponent<Damageable>();
            properties = new MaterialPropertyBlock();
        }
        private void OnEnable() { health.HitReceived += OnHit; ResetPresentation(); }
        private void OnDisable() => health.HitReceived -= OnHit;
        private void OnHit(CombatHit hit) => flashUntil = Time.time + .1f;
        private void ResetPresentation()
        {
            previous = transform.position; gait = idleTime = deathTime = flashUntil = 0;
            wasAlive = health.IsAlive; resetVersion = enemy.ResetVersion;
        }
        private static bool Has(DirectionalSpriteAnimation clip) => clip != null && !clip.IsEmpty;
        private DirectionalSpriteAnimation AttackClip => enemy.IsCloseAttack && Has(animations.closeAttack) ? animations.closeAttack : animations.attack;
        private void LateUpdate()
        {
            if (animations == null || bodyVisual == null) return;
            if (resetVersion != enemy.ResetVersion || (!wasAlive && health.IsAlive)) ResetPresentation();
            float distance = Vector2.Distance(previous, transform.position); previous = transform.position;
            if (distance > 1.5f) distance = 0;
            if (health.IsAlive) octant = DirectionalSpriteAnimation.Octant(enemy.FacingDirection);
            var clip = animations.rotations; float progress = 0;
            bool windup = enemy.State == PursuitState.Windup, strike = enemy.State == PursuitState.Striking;
            // A ground slam shakes the screen once as it lands.
            if (strike && !wasStriking && enemy.ImpactShake > 0f && Camera.main != null &&
                Camera.main.TryGetComponent(out RoadOfTheOldKing.Cameras.CameraFollow2D follow))
                follow.Kick(Vector2.down, enemy.ImpactShake);
            wasStriking = strike;
            if (!health.IsAlive) { deathTime += Time.deltaTime; if (Has(animations.death)) { clip = animations.death; progress = deathTime / clip.Duration(octant); } }
            // A dormant guardian holds the wake clip's first frame (its resting pose) and rises through it.
            else if ((enemy.IsDormant || enemy.IsWaking) && Has(animations.wake)) { clip = animations.wake; progress = enemy.IsDormant ? 0f : enemy.StateProgress; }
            // Standing its ground between attacks reads as the same fight, so it keeps the attack's stance.
            else if ((windup || strike || enemy.State == PursuitState.Recovering || (enemy.State == PursuitState.Holding && distance <= .0001f)) && Has(AttackClip))
            {
                clip = AttackClip;
                int contact = clip.contactFrame > 0 ? clip.contactFrame : Mathf.Max(1, clip.FrameCount(octant) / 2);
                int frame = enemy.State == PursuitState.Holding ? 0 : windup ? Mathf.FloorToInt(enemy.StateProgress * contact) : strike ? contact : contact + 1 + Mathf.FloorToInt(enemy.StateProgress * (clip.FrameCount(octant) - contact - 1));
                bodyVisual.sprite = clip.Frame(octant, Mathf.Clamp(frame, 0, clip.FrameCount(octant) - 1));
                clip = null;
            }
            else if (distance > .0001f && (Has(animations.walk) || Has(animations.run)))
            {
                // Run above the threshold (a charge) or whenever there's no walk; one gait phase across both.
                bool run = Has(animations.run) && (!Has(animations.walk) || distance / Mathf.Max(Time.deltaTime, .0001f) >= animations.runAnimationSpeed);
                clip = run ? animations.run : animations.walk;
                gait = Mathf.Repeat(gait + distance / (run ? animations.runCycleLength : animations.walkCycleLength), 1); progress = gait;
            }
            else if (Has(animations.idle)) { idleTime += Time.deltaTime; clip = animations.idle; progress = idleTime / clip.Duration(octant); }
            if (clip != null) bodyVisual.sprite = clip.Sample(octant, progress, out _);
            float alpha = health.IsAlive || Has(animations.death) ? 1 : 1 - Mathf.Clamp01(deathTime / animations.deathFadeDuration);
            bodyVisual.color = new Color(1, 1, 1, alpha); bodyVisual.flipX = false;
            bodyVisual.GetPropertyBlock(properties); properties.SetFloat("_FlashAmount", Time.time < flashUntil ? 1 : 0); bodyVisual.SetPropertyBlock(properties);
            wasAlive = health.IsAlive;
        }
    }
}
