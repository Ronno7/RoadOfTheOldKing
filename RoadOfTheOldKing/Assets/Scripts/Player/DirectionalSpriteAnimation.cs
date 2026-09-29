using System;
using UnityEngine;

namespace TheLostShrine.Player
{
    // One animation drawn in up to eight directions, matching a PixelLab export folder.
    // Octants run counterclockwise from east: E, NE, N, NW, W, SW, S, SE.
    [CreateAssetMenu(menuName = "Road of the Old King/Directional Sprite Animation")]
    public sealed class DirectionalSpriteAnimation : ScriptableObject
    {
        public const int DirectionCount = 8;
        public static readonly string[] DirectionNames =
            { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

        [Serializable]
        public sealed class Frames
        {
            public Sprite[] sprites = Array.Empty<Sprite>();
        }

        [Tooltip("PixelLab template or custom action this was generated from, for reference.")]
        public string source;
        [Tooltip("Playback rate for time-driven states. Distance- and action-driven states ignore it.")]
        [Min(0.1f)] public float framesPerSecond = 10f;
        public bool loop = true;
        [Tooltip("Frame shown when gameplay's damage window or release begins. -1 spreads frames evenly.")]
        [Min(-1)] public int contactFrame = -1;
        [SerializeField] private Frames[] directions = NewDirections();

        public static int Octant(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return 6;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Mathf.RoundToInt(angle / 45f + DirectionCount) % DirectionCount;
        }

        public bool IsEmpty => ResolveOctant(6) < 0;

        public int FrameCount(int octant)
        {
            octant = ResolveOctant(octant);
            return octant < 0 ? 0 : directions[octant].sprites.Length;
        }

        public float Duration(int octant) => FrameCount(octant) / framesPerSecond;

        // PixelLab can export four directions only: use the nearest drawn view,
        // preferring the horizontal one on an exact tie, as the cardinal art did.
        public int ResolveOctant(int octant)
        {
            octant = ((octant % DirectionCount) + DirectionCount) % DirectionCount;
            if (Has(octant))
                return octant;
            for (int step = 1; step <= DirectionCount / 2; step++)
            {
                int a = (octant + step) % DirectionCount, b = (octant - step + DirectionCount) % DirectionCount;
                bool hasA = Has(a), hasB = Has(b);
                if (hasA && hasB)
                    return a % 4 == 0 ? a : b;
                if (hasA) return a;
                if (hasB) return b;
            }
            return -1;
        }

        public Sprite Frame(int octant, int frame)
        {
            octant = ResolveOctant(octant);
            if (octant < 0)
                return null;
            var sprites = directions[octant].sprites;
            return sprites[loop ? (frame % sprites.Length + sprites.Length) % sprites.Length
                : Mathf.Clamp(frame, 0, sprites.Length - 1)];
        }

        // Normalized time over the whole animation; looping animations wrap, others hold the last frame.
        public Sprite Sample(int octant, float normalizedTime, out int frame)
        {
            int count = FrameCount(octant);
            frame = count == 0 ? -1 : loop
                ? Mathf.FloorToInt(Mathf.Repeat(normalizedTime, 1f) * count)
                : Mathf.Min(count - 1, Mathf.FloorToInt(Mathf.Clamp01(normalizedTime) * count));
            return count == 0 ? null : Frame(octant, frame);
        }

        // Action progress where gameplay's damage/release moment falls at contactProgress:
        // frames before the contact frame fill the windup, the rest fill contact and recovery.
        public Sprite SampleAction(int octant, float progress, float contactProgress, out int frame)
        {
            int count = FrameCount(octant);
            progress = Mathf.Clamp01(progress);
            if (count == 0)
            {
                frame = -1;
                return null;
            }
            frame = contactFrame <= 0 || contactFrame >= count || contactProgress <= 0f || contactProgress >= 1f
                ? Mathf.FloorToInt(progress * count)
                : progress < contactProgress
                ? Mathf.FloorToInt(progress / contactProgress * contactFrame)
                : contactFrame + Mathf.FloorToInt((progress - contactProgress) / (1f - contactProgress) * (count - contactFrame));
            frame = Mathf.Clamp(frame, 0, count - 1);
            return Frame(octant, frame);
        }

        public void SetFrames(int octant, Sprite[] sprites)
        {
            if (directions == null || directions.Length != DirectionCount)
                directions = NewDirections();
            directions[octant].sprites = sprites ?? Array.Empty<Sprite>();
        }

        private bool Has(int octant) => directions != null && directions.Length == DirectionCount &&
            directions[octant] != null && directions[octant].sprites.Length > 0 && directions[octant].sprites[0] != null;

        private static Frames[] NewDirections()
        {
            var result = new Frames[DirectionCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = new Frames();
            return result;
        }
    }
}
