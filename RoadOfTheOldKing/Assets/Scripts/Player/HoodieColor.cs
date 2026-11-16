using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // The hero's hoodie colour (user, 22 Oct): chosen once per New Game, kept in the save as RRGGBB. EnemyFlash
    // recolours the body's blues to it; empty keeps the drawn blue. Recolor is the same rule on the CPU, for previews.
    public static class HoodieColor
    {
        // The hoodie's mid blue, #32628E: the colour the picker starts on.
        public static readonly Color Drawn = new Color32(0x32, 0x62, 0x8E, 255);
        private const float DrawnTop = 0x8E / 255f;
        private static readonly int ColorId = Shader.PropertyToID("_HoodieColor");

        public static string ToHex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        public static bool TryParse(string hex, out Color color)
        {
            color = Drawn;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out color);
        }

        public static void Apply(GameObject player, string hex)
        {
            var animator = player != null ? player.GetComponentInChildren<PlayerSpriteAnimator>(true) : null;
            var body = animator != null ? animator.Body : null;
            if (body == null) return;
            var block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            block.SetColor(ColorId, TryParse(hex, out var color) ? new Color(color.r, color.g, color.b, 1f) : Color.clear);
            body.SetPropertyBlock(block);
        }

        public static Color32 Recolor(Color32 pixel, Color chosen)
        {
            if (pixel.a == 0) return pixel;
            float r = pixel.r / 255f, g = pixel.g / 255f, b = pixel.b / 255f;
            float top = Mathf.Max(r, Mathf.Max(g, b)), low = Mathf.Min(r, Mathf.Min(g, b));
            if (b < top || top <= .2f || top - low <= .3f * top) return pixel;
            float shade = top / DrawnTop;
            return new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01(chosen.r * shade) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(chosen.g * shade) * 255f),
                (byte)Mathf.RoundToInt(Mathf.Clamp01(chosen.b * shade) * 255f), pixel.a);
        }
    }
}
