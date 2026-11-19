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
    // Each name is announced once per playthrough, the first time the player walks into it; the save remembers.
    public sealed class AreaName : MonoBehaviour
    {
        [Tooltip("Shown to the player. Several zones may share a name.")]
        [SerializeField] private string displayName;
        [Tooltip("Zone size in world units, centred on this transform.")]
        [SerializeField] private Vector2 size = new Vector2(20f, 20f);
        [Tooltip("A place within a region: announced smaller, in the heading face, without the divider.")]
        [SerializeField] private bool landmark;

        private static readonly List<AreaName> zones = new List<AreaName>();
        private static readonly HashSet<string> seenWithoutSession = new HashSet<string>();
        private static string current;
        private static float nextCheck;
        private static PlayerHealth player;
        private const float CheckInterval = .2f;

        public string DisplayName => displayName;
        public Rect Bounds => new Rect((Vector2)transform.position - size * .5f, size);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            zones.Clear(); seenWithoutSession.Clear(); current = null; nextCheck = 0f; player = null;
            HudNotifications.AnnouncementShown -= Remember;
            HudNotifications.AnnouncementShown += Remember;
        }

        private void OnEnable() => zones.Add(this);
        // Leaving the last zone behind (a scene change, the title) forgets the current place, so a new run that
        // starts in the same place still gets its announcement.
        private void OnDisable()
        {
            zones.Remove(this);
            if (zones.Count == 0) current = null;
        }

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
            if (FirstVisit(name, game)) HudNotifications.ShowArea(name, zone.landmark);
        }

        // Queued while this run hasn't seen it; remembered and saved only once its banner shows (Remember), so a scene
        // exit crossed while it waits behind another banner doesn't lose it for the save (18 Nov).
        private static bool FirstVisit(string name, GameSession game)
        {
            if (game == null) return seenWithoutSession.Add(name);
            return game.Progress.seenAreas == null || !game.Progress.seenAreas.Contains(name);
        }

        private static void Remember(Announcement shown)
        {
            if (shown.Tier == AnnouncementTier.Event) return;
            var game = GameSession.Instance;
            if (game != null && game.Progress.MarkAreaSeen(shown.Text)) game.SaveProgress();
        }

        // The region (not a landmark) a point lies in: the smallest containing one, else the largest loaded, since
        // each scene names one region (Ardan's Edge, Green Lowlands). Empty when the scene names none.
        public static string RegionAt(Vector2 point)
        {
            AreaName inside = null, any = null;
            foreach (var zone in zones)
            {
                if (zone.landmark || string.IsNullOrWhiteSpace(zone.displayName)) continue;
                float area = zone.size.x * zone.size.y;
                if (zone.Bounds.Contains(point) && (inside == null || area < inside.size.x * inside.size.y)) inside = zone;
                if (any == null || area > any.size.x * any.size.y) any = zone;
            }
            var region = inside != null ? inside : any;
            return region != null ? region.displayName.Trim() : "";
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
