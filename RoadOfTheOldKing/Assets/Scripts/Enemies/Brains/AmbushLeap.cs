using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // A dormant enemy hidden in cover (a breakable bush) springs at the player. When they pass near the cover, cut it or
    // wake the enemy any other way, the cover rustles and the enemy leaps out over its wake time, homing on the player,
    // and bites as it lands. The bite goes through the player's own hit rules, so a dodge still avoids it. Then it fights
    // as usual, and after a rest it hides again. The Tutorial road's rats (user, 23 Nov: rats "never hurt the player,
    // maybe design some sort of scripted event... like one of the rats jumping from a bush to attack the player").
    [DisallowMultipleComponent, RequireComponent(typeof(PursuitBrain), typeof(MeleeSpriteView), typeof(Rigidbody2D))]
    [DefaultExecutionOrder(20)] // After MeleeSpriteView, so the leap's pose wins.
    public sealed class AmbushLeap : MonoBehaviour
    {
        [Tooltip("The bush it hides in. While it stands the enemy is unseen; cut, the enemy springs at once.")]
        [SerializeField] private Breakable cover;
        [Tooltip("Springs when the player comes this close to the cover (or to the enemy, once the cover is gone).")]
        [SerializeField, Min(0.5f)] private float triggerRadius = 3.5f;
        [SerializeField, Min(1)] private int biteDamage = 20;
        [Tooltip("The bite lands only if the player is still this close when it touches down.")]
        [SerializeField, Min(0.2f)] private float biteReach = 1.2f;
        [SerializeField, Min(0f)] private float biteKnockback = 2f;
        [SerializeField, Min(0f)] private float biteStagger = .15f;
        [Tooltip("It lands this far short of the player's feet.")]
        [SerializeField, Min(0f)] private float landShort = .45f;
        [Tooltip("Peak height of the leap, in world units. Its length is the brain's wake duration.")]
        [SerializeField, Min(0f)] private float arcHeight = .7f;
        [Tooltip("Seconds the cover shakes as it springs.")]
        [SerializeField, Min(0f)] private float rustleSeconds = .35f;

        private const float Pixel = 1f / 16f;
        private PursuitBrain brain;
        private MeleeSpriteView view;
        private SpriteRenderer body;
        private Rigidbody2D rb;
        private Damageable health;
        private PlayerHealth player;
        private Vector3 bodyRest;
        private Vector2 leapFrom;
        private bool leaping, hidden;
        private int leapVersion;
        private Transform rustled;
        private Vector3 rustleRest;
        private float rustleUntil;

        public bool IsHidden => hidden;
        public bool IsLeaping => leaping;

        private void Awake()
        {
            brain = GetComponent<PursuitBrain>();
            view = GetComponent<MeleeSpriteView>();
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Damageable>();
            body = view.BodyVisual;
            if (body != null) bodyRest = body.transform.localPosition;
            if (cover != null)
            {
                var art = cover.GetComponentInChildren<SpriteRenderer>(true);
                rustled = art != null ? art.transform : cover.transform;
                rustleRest = rustled.localPosition;
            }
        }

        private void OnEnable() { if (cover != null) cover.Broken += OnCoverBroken; }

        private void OnDisable()
        {
            if (cover != null) cover.Broken -= OnCoverBroken;
            if (leaping) EndLeap();
            SetHidden(false);
            StopRustle();
        }

        private bool Covered => cover != null && !cover.IsBroken && cover.gameObject.activeInHierarchy;

        // Cut while it waits: it bursts out of the wreck at whoever cut it.
        private void OnCoverBroken(CombatHit hit)
        {
            if (brain.IsDormant && !leaping) Spring(cover.transform.position);
        }

        private void LateUpdate()
        {
            if (player == null) player = FindFirstObjectByType<PlayerHealth>();
            bool covered = Covered;
            if (leaping)
            {
                // A rest or reload put it back to sleep mid-leap: drop the leap, it hides again below.
                if (brain.ResetVersion != leapVersion) EndLeap();
                else if (brain.IsWaking) { Fly(); UpdateRustle(); return; }
                else if (!brain.IsDormant) { Land(); }
                else { HoldAtCover(); UpdateRustle(); return; } // Woken; the brain rises next tick.
            }
            // Something else woke it (a hit, the player on top of it): it still leaps.
            if (brain.IsWaking) Spring(covered ? (Vector2)cover.transform.position : rb.position);
            else if (brain.IsDormant)
            {
                SetHidden(covered);
                Vector2 from = covered ? (Vector2)cover.transform.position : rb.position;
                if (player != null && player.IsAlive &&
                    ((Vector2)player.transform.position - from).sqrMagnitude <= triggerRadius * triggerRadius)
                    Spring(from);
            }
            else SetHidden(false);
            UpdateRustle();
        }

        private void Spring(Vector2 from)
        {
            if (body == null) return;
            brain.Wake();
            leaping = true;
            leapVersion = brain.ResetVersion;
            leapFrom = from;
            SetHidden(false);
            rb.simulated = false; // No body to bump or strike mid-air; the brain holds still while it rises.
            HoldAtCover();
            if (Covered && rustled != null) rustleUntil = Time.time + rustleSeconds;
        }

        private void HoldAtCover()
        {
            transform.position = leapFrom;
            rb.position = leapFrom;
            Pose(0f, Target() - leapFrom);
        }

        // Homes on where the player is now, so it lands on them unless they dodge clear.
        private Vector2 Target()
        {
            if (player == null) return leapFrom;
            Vector2 at = player.transform.position, toward = at - leapFrom;
            return toward.sqrMagnitude > landShort * landShort ? at - toward.normalized * landShort : leapFrom;
        }

        private void Fly()
        {
            float t = Mathf.Clamp01(brain.StateProgress);
            Vector2 to = Target(), p = Vector2.Lerp(leapFrom, to, t);
            transform.position = p;
            rb.position = p;
            Pose(t, to - leapFrom);
        }

        private void Pose(float t, Vector2 direction)
        {
            body.transform.localPosition = bodyRest + Vector3.up * (arcHeight * 4f * t * (1f - t));
            var run = view.Animations != null ? view.Animations.run : null;
            if (run != null && !run.IsEmpty && direction.sqrMagnitude > .0001f)
                body.sprite = run.Sample(DirectionalSpriteAnimation.Octant(direction), t, out _);
        }

        private void Land()
        {
            EndLeap();
            if (player == null || !player.IsAlive || health == null || !health.IsAlive) return;
            Vector2 toPlayer = (Vector2)player.transform.position - rb.position;
            if (toPlayer.sqrMagnitude > biteReach * biteReach) return;
            Vector2 direction = toPlayer.sqrMagnitude > .0001f ? toPlayer.normalized : Vector2.down;
            player.Health.ReceiveHit(new CombatHit(gameObject, AttackKind.EnemyMelee, biteDamage, direction, biteKnockback, biteStagger));
        }

        private void EndLeap()
        {
            leaping = false;
            if (body != null) body.transform.localPosition = bodyRest;
            rb.simulated = true;
            rb.position = transform.position;
            rb.linearVelocity = Vector2.zero;
        }

        // Out of sight in its cover: no sprite, health bar (it follows the sprite) or body to bump into.
        private void SetHidden(bool hide)
        {
            if (hide == hidden || body == null) return;
            hidden = hide;
            body.enabled = !hide;
            rb.simulated = !hide;
        }

        // A one-pixel shiver, left and right.
        private void UpdateRustle()
        {
            if (rustled == null) return;
            if (Time.time >= rustleUntil) { StopRustle(); return; }
            rustled.localPosition = rustleRest + Vector3.right * (((int)(Time.time * 24f) & 1) == 0 ? Pixel : -Pixel);
        }

        private void StopRustle()
        {
            if (rustled != null) rustled.localPosition = rustleRest;
        }
    }
}
