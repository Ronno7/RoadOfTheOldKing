using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The traveling trader potters about a small patch around his spot (user, 17 Oct): short walks, pauses, a glance
    // toward the camera. He stops and turns to the player who comes near, and stays put while talking or trading.
    // Presentation and reach only: offers, prices and saves are untouched. No collider, so he never blocks the hero.
    [DisallowMultipleComponent, RequireComponent(typeof(Trader))]
    public sealed class TraderWalker : MonoBehaviour
    {
        [Tooltip("Half extents of the wandering patch around the authored position (world units).")]
        [SerializeField] private Vector2 range = new Vector2(1.75f, .75f);
        [SerializeField, Min(.1f)] private float speed = .85f;
        [Tooltip("Seconds standing between walks (random within).")]
        [SerializeField] private Vector2 pause = new Vector2(2.5f, 6f);
        [Tooltip("The player within this distance stops him and he turns to face them.")]
        [SerializeField, Min(0f)] private float noticeDistance = 2.75f;
        [Tooltip("Clearance kept from walls and props along a walk.")]
        [SerializeField, Min(.05f)] private float footRadius = .3f;

        private Vector2 home, target;
        private float waitUntil;
        private bool walking;
        private Transform player;
        private PlayerHealth playerHealth;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private readonly List<Collider2D> overlaps = new List<Collider2D>(8);

        public Vector2 Home => home;
        public Vector2 Velocity { get; private set; }
        public Vector2 Facing { get; private set; } = Vector2.down;
        // Set by the dialogue and shop while they are open.
        public bool Engaged { get; set; }
        public bool PlayerNear { get; private set; }

        private void Awake()
        {
            home = transform.position;
            target = home;
        }

        private void OnEnable() { walking = false; waitUntil = Time.time + Random.Range(pause.x, pause.y); Velocity = Vector2.zero; }

        // Back to the authored spot, standing (fixtures, scene reloads).
        public void ReturnHome()
        {
            transform.position = new Vector3(home.x, home.y, transform.position.z);
            walking = false; Velocity = Vector2.zero; Facing = Vector2.down;
        }

        private void Update()
        {
            Velocity = Vector2.zero;
            if (Time.deltaTime <= 0f) return;
            if (player == null)
            {
                var movement = FindFirstObjectByType<PlayerMovement>();
                if (movement != null) { player = movement.transform; playerHealth = movement.GetComponent<PlayerHealth>(); }
            }
            Vector2 position = transform.position;
            PlayerNear = player != null && (playerHealth == null || playerHealth.IsAlive) &&
                Vector2.Distance(player.position, position) <= noticeDistance;
            if (Engaged || PlayerNear)
            {
                walking = false;
                if (player != null) Face((Vector2)player.position - position);
                waitUntil = Mathf.Max(waitUntil, Time.time + pause.x);
                return;
            }
            if (!walking)
            {
                if (Time.time >= waitUntil) PickTarget(position);
                return;
            }
            Vector2 toTarget = target - position;
            float stepLength = speed * Time.deltaTime;
            if (toTarget.magnitude <= stepLength)
            {
                transform.position = new Vector3(target.x, target.y, transform.position.z);
                walking = false;
                waitUntil = Time.time + Random.Range(pause.x, pause.y);
                // Often a glance back toward the road (the camera) after a walk.
                if (Random.value < .5f) Facing = Vector2.down;
                return;
            }
            Vector2 direction = toTarget.normalized;
            Vector2 next = position + direction * stepLength;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            Velocity = direction * speed;
            Face(direction);
        }

        private void Face(Vector2 direction) { if (direction.sqrMagnitude > .0001f) Facing = direction.normalized; }

        // A random spot in the patch reachable in a straight, clear line; otherwise he waits and tries again later.
        private void PickTarget(Vector2 from)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var candidate = home + new Vector2(Random.Range(-range.x, range.x), Random.Range(-range.y, range.y));
                candidate = new Vector2(Mathf.Round(candidate.x * 16f) / 16f, Mathf.Round(candidate.y * 16f) / 16f);
                if ((candidate - from).sqrMagnitude < .36f || !Clear(from, candidate)) continue;
                target = candidate; walking = true;
                return;
            }
            waitUntil = Time.time + pause.x;
        }

        private bool Clear(Vector2 from, Vector2 to)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            Vector2 delta = to - from;
            Physics2D.CircleCast(from, footRadius, delta.normalized, filter, hits, delta.magnitude);
            foreach (var hit in hits) if (Blocks(hit.collider)) return false;
            Physics2D.OverlapCircle(to, footRadius, filter, overlaps);
            foreach (var collider in overlaps) if (Blocks(collider)) return false;
            return true;
        }

        private bool Blocks(Collider2D collider) =>
            collider != null && (player == null || !collider.transform.IsChildOf(player)) && !collider.transform.IsChildOf(transform);

        private void OnDrawGizmosSelected()
        {
            Vector2 center = Application.isPlaying ? home : (Vector2)transform.position;
            Gizmos.color = new Color(.9f, .75f, .35f, .6f);
            Gizmos.DrawWireCube(center, range * 2f);
        }
    }
}
