using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Prototype
{
    // Placeholder presentation reads the enemy; it does not determine damage.
    [RequireComponent(typeof(SimpleMeleeEnemy), typeof(Damageable))]
    public sealed class MeleeEnemyView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyVisual;
        [SerializeField] private Transform facingMarker;
        [SerializeField] private LineRenderer attackOutline;
        [SerializeField] private TextMesh label;
        private SimpleMeleeEnemy enemy;
        private Damageable health;
        private float flashUntil;

        private void Awake()
        {
            enemy = GetComponent<SimpleMeleeEnemy>();
            health = GetComponent<Damageable>();
        }
        private void OnEnable() => health.HitReceived += OnHit;
        private void OnDisable() => health.HitReceived -= OnHit;
        private void OnHit(CombatHit hit) => flashUntil = Time.time + 0.1f;

        private void LateUpdate()
        {
            bool windup = enemy.State == MeleeEnemyState.Windup;
            bool strike = enemy.State == MeleeEnemyState.Striking;
            bool recovery = enemy.State == MeleeEnemyState.Recovering;
            Color color = !health.IsAlive ? new Color(0.3f, 0.3f, 0.3f, 0.5f)
                : Time.time < flashUntil ? Color.white
                : windup ? new Color(1f, 0.8f, 0.2f)
                : strike ? Color.red : recovery ? new Color(0.5f, 0.65f, 0.7f)
                : new Color(0.8f, 0.3f, 0.3f);
            if (bodyVisual != null)
                bodyVisual.color = color;
            if (facingMarker != null)
                facingMarker.localPosition = enemy.FacingDirection * 0.4f;
            if (label != null)
                label.text = !health.IsAlive ? "DEFEATED" :
                    (windup ? "WIND-UP" : strike ? "STRIKE" : recovery ? "RECOVERING" : "SENTINEL") +
                    "\n" + health.Health + "/" + health.MaxHealth;
            if (attackOutline == null)
                return;
            attackOutline.enabled = windup || strike;
            if (!attackOutline.enabled)
                return;
            attackOutline.startColor = attackOutline.endColor = color;
            const int segments = 16;
            attackOutline.positionCount = segments + 3;
            attackOutline.SetPosition(0, transform.position);
            float angle = Mathf.Atan2(enemy.FacingDirection.y, enemy.FacingDirection.x) * Mathf.Rad2Deg;
            for (int i = 0; i <= segments; i++)
            {
                float a = (angle - enemy.AttackArc * 0.5f + enemy.AttackArc * i / segments) * Mathf.Deg2Rad;
                attackOutline.SetPosition(i + 1, transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * enemy.AttackReach);
            }
            attackOutline.SetPosition(segments + 2, transform.position);
        }
    }
}
