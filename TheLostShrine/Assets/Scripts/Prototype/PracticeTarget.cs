using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Prototype
{
    // Tutorial-only feedback and reset behavior; enemy logic can use Damageable independently.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class PracticeTarget : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyVisual;
        [SerializeField] private Transform healthFill;
        [SerializeField] private TextMesh label;
        [SerializeField] private string displayName = "DUMMY";
        [SerializeField, Min(0.5f)] private float resetDelay = 4f;
        private Damageable health;
        private ShieldProtection shield;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Vector3 home;
        private Color originalColor;
        private float flashUntil;
        private float resetAt = -1f;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            shield = GetComponent<ShieldProtection>();
            reaction = GetComponent<HitReaction>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            home = transform.position;
            if (bodyVisual != null)
                originalColor = bodyVisual.color;
        }

        private void OnEnable()
        {
            health.HitReceived += OnHit;
            health.Defeated += OnDefeated;
            if (shield != null)
                shield.Blocked += OnBlocked;
        }

        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            health.Defeated -= OnDefeated;
            if (shield != null)
                shield.Blocked -= OnBlocked;
        }

        private void OnHit(CombatHit hit) => flashUntil = Time.time + 0.12f;
        private void OnBlocked() => flashUntil = Time.time + 0.12f;
        private void OnDefeated()
        {
            resetAt = Time.time + resetDelay;
            if (bodyCollider != null)
                bodyCollider.enabled = false;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void Update()
        {
            if (resetAt >= 0f && Time.time >= resetAt)
                ResetTarget();
            if (bodyVisual != null)
                bodyVisual.color = !health.IsAlive ? new Color(0.25f, 0.25f, 0.25f, 0.5f)
                    : Time.time < flashUntil ? Color.white
                    : reaction != null && reaction.IsStaggered ? new Color(1f, 0.6f, 0.2f) : originalColor;
            if (healthFill != null)
                healthFill.localScale = new Vector3((float)health.Health / health.MaxHealth, 1f, 1f);
            if (label != null)
                label.text = !health.IsAlive ? "RESETTING..." : displayName + (shield != null && shield.IsIntact ? " / GUARDED" : "")
                    + "\n" + health.Health + "/" + health.MaxHealth;
        }

        public void ResetTarget()
        {
            resetAt = -1f;
            health.RestoreHealth();
            if (shield != null)
                shield.Restore();
            if (body != null)
            {
                body.position = home;
                body.linearVelocity = Vector2.zero;
            }
            transform.position = home;
            if (bodyCollider != null)
                bodyCollider.enabled = true;
        }
    }
}
