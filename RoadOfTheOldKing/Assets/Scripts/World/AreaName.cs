using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A named place, announced Dark Souls style when the player walks into it. Zones are plain world rectangles
    // (no colliders or physics contacts); where several contain the player, the smallest wins, so a landmark
    // inside a region names itself. Several components with the same name form one irregular place.
    // The last announced name survives scene loads: respawning or arriving inside the same place stays quiet.
    public sealed class AreaName : MonoBehaviour
    {
        [Tooltip("Shown to the player. Several zones may share a name.")]
        [SerializeField] private string displayName;
        [Tooltip("Zone size in world units, centred on this transform.")]
        [SerializeField] private Vector2 size = new Vector2(20f, 20f);
        [Tooltip("A place within a region: announced smaller, in the heading face, without the divider.")]
        [SerializeField] private bool landmark;

        private static readonly List<AreaName> zones = new List<AreaName>();
        private static readonly Dictionary<string, float> shownAt = new Dictionary<string, float>();
        private static string current;
        private static float nextCheck;
        private static PlayerHealth player;
        // The same name only re-announces after this long, so pacing along a boundary stays quiet.
        private const float RepeatDelay = 60f, CheckInterval = .2f;

        public string DisplayName => displayName;
        public Rect Bounds => new Rect((Vector2)transform.position - size * .5f, size);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            zones.Clear(); shownAt.Clear(); current = null; nextCheck = 0f; player = null;
        }

        private void OnEnable() => zones.Add(this);
        private void OnDisable() => zones.Remove(this);

        // Any enabled zone may drive the shared check; the clock keeps it to one pass per interval.
        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckInterval;
            if (player == null) player = FindFirstObjectByType<PlayerHealth>();
            var game = GameSession.Instance;
            if (player == null || !player.IsAlive || (game != null && game.IsLoading)) return;

            var zone = Containing(player.transform.position);
            string name = zone != null ? zone.displayName.Trim() : null;
            if (string.IsNullOrEmpty(name) || name == current) return;
            current = name;
            if (shownAt.TryGetValue(name, out float last) && Time.unscaledTime - last < RepeatDelay) return;
            shownAt[name] = Time.unscaledTime;
            HudNotifications.ShowArea(name, zone.landmark);
        }

        private static AreaName Containing(Vector2 point)
        {
            AreaName best = null; float bestArea = float.PositiveInfinity;
            foreach (var zone in zones)
            {
                if (string.IsNullOrWhiteSpace(zone.displayName) || !zone.Bounds.Contains(point)) continue;
                float area = zone.size.x * zone.size.y;
                if (area < bestArea) { best = zone; bestArea = area; }
            }
            return best;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.91f, .72f, .36f, .9f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
