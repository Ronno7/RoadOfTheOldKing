using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Cameras
{
    // A view point: while the player stands in the zone the camera widens and shifts so the player sits at a chosen
    // screen point, showing what lies ahead (a ruin below a ledge, the valley at the end of a road). Zones are plain
    // world rectangles like AreaName; where several contain the player, the smallest wins. Leaving hands the
    // player's own zoom back.
    public sealed class CameraVista : MonoBehaviour
    {
        [Tooltip("Zone size in world units, centred on this transform.")]
        [SerializeField] private Vector2 size = new Vector2(10f, 4f);
        [Tooltip("Half the visible world height while inside. May exceed the player's zoom limit.")]
        [SerializeField, Min(1f)] private float viewSize = 9.5f;
        [Tooltip("Where the player sits on screen (0..1): (0.5, 0.75) looks south, (0.5, 0.25) looks north.")]
        [SerializeField] private Vector2 playerOnScreen = new Vector2(.5f, .5f);
        [Tooltip("Seconds to ease into and out of the view.")]
        [SerializeField, Min(0f)] private float easeTime = .9f;

        private static readonly List<CameraVista> zones = new List<CameraVista>();
        private static CameraVista active;
        private static PlayerHealth player;
        private static float nextCheck;
        private const float CheckInterval = .1f;

        public Rect Bounds => new Rect((Vector2)transform.position - size * .5f, size);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            zones.Clear(); active = null; player = null; nextCheck = 0f;
        }

        private void OnEnable() => zones.Add(this);

        private void OnDisable()
        {
            zones.Remove(this);
            if (active == this) { Release(this); active = null; }
        }

        // Any enabled zone may drive the shared check; the clock keeps it to one pass per interval.
        private void Update()
        {
            if (Time.unscaledTime < nextCheck) return;
            nextCheck = Time.unscaledTime + CheckInterval;
            if (player == null) player = FindFirstObjectByType<PlayerHealth>();
            var game = GameSession.Instance;
            bool present = player != null && player.IsAlive && (game == null || !game.IsLoading);
            var zone = present ? Containing(player.transform.position) : null;
            if (zone == active) return;
            if (active != null) Release(active);
            active = zone;
            if (active != null) Apply(active);
        }

        private static CameraVista Containing(Vector2 point)
        {
            CameraVista best = null; float bestArea = float.PositiveInfinity;
            foreach (var zone in zones)
            {
                if (!zone.Bounds.Contains(point)) continue;
                float area = zone.size.x * zone.size.y;
                if (area < bestArea) { best = zone; bestArea = area; }
            }
            return best;
        }

        private static void Apply(CameraVista zone)
        {
            var view = Camera.main;
            if (view == null) return;
            // SmoothDamp's time is roughly the time to cover most of the change, so it runs shorter than the linear
            // framing blend for the two to arrive together.
            if (view.TryGetComponent(out CameraZoom2D zoom)) zoom.SetOverride(zone, zone.viewSize, zone.easeTime * .4f);
            if (view.TryGetComponent(out CameraFollow2D follow)) follow.SetFraming(zone, zone.playerOnScreen, zone.easeTime);
        }

        private static void Release(CameraVista zone)
        {
            var view = Camera.main;
            if (view == null) return;
            if (view.TryGetComponent(out CameraZoom2D zoom)) zoom.ClearOverride(zone);
            if (view.TryGetComponent(out CameraFollow2D follow)) follow.ClearFraming(zone);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.45f, .75f, .95f, .9f);
            Gizmos.DrawWireCube(transform.position, size);
        }
#endif
    }
}
