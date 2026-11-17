using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.Audio
{
    // A looping sound that comes from a place: this transform, or the nearest painted cell of the given tilemaps,
    // so a river is heard from wherever it runs. Like the game's other sounds it is 2D: loudness falls with distance
    // from the camera in the map plane (not camera Z), and it pans toward its side.
    [DisallowMultipleComponent]
    public sealed class AmbientEmitter : MonoBehaviour
    {
        [SerializeField] private AudioClip loop;
        [SerializeField, Range(0f, 1f)] private float volume = .5f;
        [Tooltip("Full volume within this many units of the camera.")]
        [SerializeField, Min(0f)] private float near = 2f;
        [Tooltip("Silent beyond this many units.")]
        [SerializeField, Min(.1f)] private float far = 14f;
        [SerializeField, Range(0f, 1f)] private float pan = .5f;
        [Tooltip("Optional: the sound comes from the nearest painted cell of these maps. Inactive or hidden maps are skipped (drained water).")]
        [SerializeField] private Tilemap[] areas;
        [Tooltip("Optional: silent while this renderer is off, e.g. an unlit bonfire's flame.")]
        [SerializeField] private Renderer onlyWhileShown;

        private const float RefreshSeconds = .15f, GainPerSecond = 2f, PanPerSecond = 4f;
        private readonly List<Vector2> cells = new List<Vector2>();
        private AudioSource source;
        private float target, targetPan, nextRefresh;
        private bool dirty = true;
        private int activeMask = -1;

        private void OnEnable()
        {
            if (loop == null) return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.clip = loop;
                AudioChannels.Route(source, AudioChannel.Ambience);
            }
            source.volume = 0f;
            source.timeSamples = Random.Range(0, Mathf.Max(1, loop.samples));
            source.Play();
            dirty = true;
            Tilemap.tilemapTileChanged += OnTilesChanged;
        }

        private void OnDisable()
        {
            Tilemap.tilemapTileChanged -= OnTilesChanged;
            if (source != null) source.Stop();
        }

        private void OnTilesChanged(Tilemap map, Tilemap.SyncTile[] changed)
        {
            if (areas != null && System.Array.IndexOf(areas, map) >= 0) dirty = true;
        }

        private void Update()
        {
            if (source == null) return;
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshSeconds;
                Aim();
            }
            source.volume = Mathf.MoveTowards(source.volume, target, GainPerSecond * Time.unscaledDeltaTime);
            source.panStereo = Mathf.MoveTowards(source.panStereo, targetPan, PanPerSecond * Time.unscaledDeltaTime);
        }

        private void Aim()
        {
            target = 0f;
            var view = Camera.main;
            if (view == null || (onlyWhileShown != null && !onlyWhileShown.enabled)) return;
            Vector2 ear = view.transform.position;
            if (!Nearest(ear, out var from)) return;
            float closeness = Mathf.InverseLerp(far, near, Vector2.Distance(ear, from));
            target = volume * closeness * closeness;
            targetPan = Mathf.Clamp((from.x - ear.x) / far, -1f, 1f) * pan;
        }

        private bool Nearest(Vector2 ear, out Vector2 point)
        {
            point = transform.position;
            if (areas == null || areas.Length == 0) return true;
            int mask = 0;
            for (int i = 0; i < areas.Length && i < 31; i++) if (Shown(areas[i])) mask |= 1 << i;
            if (dirty || mask != activeMask) Rebuild(mask);
            float best = float.MaxValue;
            foreach (var cell in cells)
            {
                float d = (cell - ear).sqrMagnitude;
                if (d < best) { best = d; point = cell; }
            }
            return cells.Count > 0;
        }

        private void Rebuild(int mask)
        {
            cells.Clear();
            for (int i = 0; i < areas.Length && i < 31; i++)
            {
                if ((mask & (1 << i)) == 0) continue;
                var map = areas[i];
                foreach (var p in map.cellBounds.allPositionsWithin)
                    if (map.HasTile(p)) cells.Add(map.GetCellCenterWorld(p));
            }
            activeMask = mask;
            dirty = false;
        }

        private static bool Shown(Tilemap map)
        {
            if (map == null || !map.isActiveAndEnabled) return false;
            return !map.TryGetComponent<TilemapRenderer>(out var renderer) || renderer.enabled;
        }
    }
}
