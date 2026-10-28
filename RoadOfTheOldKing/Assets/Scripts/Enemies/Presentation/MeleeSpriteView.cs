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
        [SerializeField] private LineRenderer attackOutline;
        private PursuitBrain enemy;
        private Damageable health;
        private Vector2 previous;
        private float gait, idleTime, deathTime, flashUntil;
        private int octant = 6, resetVersion;
        private bool wasAlive;
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            enemy = GetComponent<PursuitBrain>(); health = GetComponent<Damageable>();
            properties = new MaterialPropertyBlock();
        }
        private void OnEnable() { health.HitReceived += OnHit; ResetPresentation(); }
        private void OnDisable() { health.HitReceived -= OnHit; if (attackOutline != null) attackOutline.enabled = false; }
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
            if (!health.IsAlive) { deathTime += Time.deltaTime; if (Has(animations.death)) { clip = animations.death; progress = deathTime / clip.Duration(octant); } }
            else if ((windup || strike || enemy.State == PursuitState.Recovering) && Has(AttackClip))
            {
                clip = AttackClip;
                int contact = clip.contactFrame > 0 ? clip.contactFrame : Mathf.Max(1, clip.FrameCount(octant) / 2);
                int frame = windup ? Mathf.FloorToInt(enemy.StateProgress * contact) : strike ? contact : contact + 1 + Mathf.FloorToInt(enemy.StateProgress * (clip.FrameCount(octant) - contact - 1));
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
            if (attackOutline == null) return;
            attackOutline.enabled = health.IsAlive && (windup || strike);
            if (!attackOutline.enabled) return;
            attackOutline.startColor = attackOutline.endColor = strike ? new Color(1, .3f, .15f, .9f) : new Color(1, .75f, .2f, .65f);
            // Locked direction: the honest dodge cue for both charge lanes and arcs.
            if (windup && enemy.IsLaneLocked) attackOutline.startColor = attackOutline.endColor = new Color(1, .5f, .15f, .85f);
            if (enemy.IsChargeAttack)
            {
                // The lane is as wide as the arc actually reaches sideways, so it never overstates the danger.
                float halfWidth = enemy.AttackReach * Mathf.Sin(Mathf.Min(enemy.AttackArc, 180f) * .5f * Mathf.Deg2Rad);
                Vector2 direction = enemy.FacingDirection, side = new Vector2(-direction.y, direction.x) * halfWidth;
                Vector2 start = enemy.AttackOrigin, end = start + direction * (enemy.ChargeDistance + enemy.AttackReach);
                attackOutline.positionCount = 5;
                attackOutline.SetPosition(0, start - side); attackOutline.SetPosition(1, end - side);
                attackOutline.SetPosition(2, end + side); attackOutline.SetPosition(3, start + side);
                attackOutline.SetPosition(4, start - side);
                return;
            }
            const int segments = 16; attackOutline.positionCount = segments + 3;
            attackOutline.SetPosition(0, transform.position);
            float angle = Mathf.Atan2(enemy.FacingDirection.y, enemy.FacingDirection.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= segments; i++)
            {
                float a = (angle - enemy.AttackArc * .5f + enemy.AttackArc * i / segments) * Mathf.Deg2Rad;
                attackOutline.SetPosition(i + 1, transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * enemy.AttackReach);
            }
            attackOutline.SetPosition(segments + 2, transform.position);
        }
    }
}
