using System;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Axe Spin Sprites")]
    public sealed class AxeSpinSprites : ScriptableObject
    {
        public Sprite[] cels = Array.Empty<Sprite>();
        public float rotationsPerSecond = 4f;
        public const int CanvasSize = 256, PixelsPerUnit = 128;

        // The presenter supplies elapsed flight time, preserving phase through Recall.
        public int Sample(double seconds, int startCel = 0)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 ||
                !(rotationsPerSecond > 0) || float.IsInfinity(rotationsPerSecond) || cels == null || cels.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            double phase = (seconds * rotationsPerSecond) % 1.0;
            int step = (int)Math.Floor(phase*cels.Length + 0.000001) % cels.Length;
            return ((step + startCel) % cels.Length + cels.Length) % cels.Length;
        }

        public bool IsRegistered(out string error)
        {
            if (cels == null || cels.Length != 8 || !(rotationsPerSecond > 0) || float.IsInfinity(rotationsPerSecond))
            { error = "Spin requires eight cels and a finite positive rotation rate."; return false; }
            foreach (var cel in cels)
                if (cel == null || cel.rect.size != Vector2.one*CanvasSize || cel.pixelsPerUnit != PixelsPerUnit ||
                    (cel.pivot-Vector2.one*(CanvasSize/2f)).sqrMagnitude > .01f)
                { error = "Spin cels must be 256x256, 128 PPU, centered on their authored balance point."; return false; }
            error = null; return true;
        }
    }
}
