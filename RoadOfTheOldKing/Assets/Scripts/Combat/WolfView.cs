using UnityEngine;

namespace TheLostShrine.Combat
{
    // Presentation only. The enemy owns movement, attack commitment, damage and reset.
    [DisallowMultipleComponent, RequireComponent(typeof(SimpleMeleeEnemy), typeof(Damageable))]
    public sealed class WolfView : MonoBehaviour
    {
        [SerializeField] private WolfAnimationSet animations;
        [SerializeField] private SpriteRenderer bodyVisual;
        [SerializeField] private LineRenderer attackOutline;
        private SimpleMeleeEnemy enemy;
        private Damageable health;
        private HitReaction reaction;
        private Vector2 previousPosition;
        private float gait, flashUntil, deathElapsed;
        private bool wasAlive;
        private int facing = 3, resetVersion;
        private MaterialPropertyBlock flashProperties;

        private void Awake()
        {
            flashProperties = new MaterialPropertyBlock();
            enemy = GetComponent<SimpleMeleeEnemy>();
            health = GetComponent<Damageable>();
            reaction = GetComponent<HitReaction>();
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
            previousPosition = transform.position;
            wasAlive = health.IsAlive;
            resetVersion = enemy.ResetVersion;
            gait = deathElapsed = flashUntil = 0f;
        }

        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            if (attackOutline != null) attackOutline.enabled = false;
            SetFlash(false);
        }

        private void OnHit(CombatHit hit) { if (animations != null) flashUntil = Time.time + animations.hitFlashDuration; }

        private void LateUpdate()
        {
            if (animations == null || bodyVisual == null) return;
            Vector2 position = transform.position;
            float distance = Vector2.Distance(position, previousPosition);
            previousPosition = position;
            if (resetVersion != enemy.ResetVersion || (!wasAlive && health.IsAlive))
            {
                resetVersion = enemy.ResetVersion;
                gait = deathElapsed = flashUntil = 0f;
                distance = 0f;
            }
            if (health.IsAlive) UpdateFacing(enemy.FacingDirection);
            var poses = facing == 1 ? animations.north : facing == 3 ? animations.south : animations.side;
            Sprite sprite = poses.idle;
            if (!health.IsAlive)
            {
                if (wasAlive) deathElapsed = 0f;
                deathElapsed += Time.deltaTime;
                sprite = deathElapsed < animations.collapseDuration * .5f ? poses.crouch : poses.dead;
            }
            else if (reaction != null && reaction.IsStaggered) sprite = poses.crouch;
            else if (enemy.State == MeleeEnemyState.Windup) sprite = poses.crouch;
            else if (enemy.State == MeleeEnemyState.Striking) sprite = poses.bite;
            else if (enemy.State == MeleeEnemyState.Recovering)
                sprite = enemy.StateProgress < .25f ? poses.crouch : poses.idle;
            else if ((enemy.State == MeleeEnemyState.Pursuing || enemy.State == MeleeEnemyState.Returning)
                && distance > .0001f && distance < 1f && poses.move != null && poses.move.Length > 0)
            {
                gait = Mathf.Repeat(gait + distance / animations.strideDistance, 1f);
                sprite = poses.move[Mathf.Min((int)(gait * poses.move.Length), poses.move.Length - 1)];
            }
            // FixedUpdate may not run every render frame: keep planted walking pose between ticks.
            else if ((enemy.State == MeleeEnemyState.Pursuing || enemy.State == MeleeEnemyState.Returning)
                && poses.move != null && poses.move.Length > 0)
                sprite = poses.move[Mathf.Min((int)(gait * poses.move.Length), poses.move.Length - 1)];

            bodyVisual.sprite = sprite;
            bodyVisual.flipX = facing == 2;
            bodyVisual.color = Color.white;
            SetFlash(Time.time < flashUntil);
            wasAlive = health.IsAlive;
            DrawWarning();
        }

        private void UpdateFacing(Vector2 direction)
        {
            if (direction.sqrMagnitude < .001f) return;
            bool horizontal = facing == 0 || facing == 2;
            float x = Mathf.Abs(direction.x), y = Mathf.Abs(direction.y);
            if (horizontal ? y > x * 1.2f : x > y * 1.2f) horizontal = !horizontal;
            facing = horizontal ? (direction.x >= 0f ? 0 : 2) : (direction.y >= 0f ? 1 : 3);
        }

        private void SetFlash(bool active)
        {
            if (bodyVisual == null || flashProperties == null) return;
            bodyVisual.GetPropertyBlock(flashProperties);
            flashProperties.SetFloat("_FlashAmount", active ? 1f : 0f);
            bodyVisual.SetPropertyBlock(flashProperties);
        }

        private void DrawWarning()
        {
            if (attackOutline == null) return;
            bool active = health.IsAlive && (enemy.State == MeleeEnemyState.Windup || enemy.State == MeleeEnemyState.Striking);
            attackOutline.enabled = active;
            if (!active) return;
            var color = enemy.State == MeleeEnemyState.Windup ? new Color(.85f, .58f, .30f, .45f) : new Color(.95f, .35f, .25f, .7f);
            attackOutline.startColor = attackOutline.endColor = color;
            attackOutline.sortingLayerID = bodyVisual.sortingLayerID;
            attackOutline.sortingOrder = bodyVisual.sortingOrder - 1;
            const int segments = 16;
            attackOutline.positionCount = segments + 3;
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
