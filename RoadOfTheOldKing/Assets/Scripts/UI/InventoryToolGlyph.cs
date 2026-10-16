using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // UI-only glyphs for tools without standalone world sprites; no raster/import dependency.
    internal sealed class InventoryToolGlyph : VisualElement
    {
        private readonly bool lantern;
        private static readonly Color Ink = new Color32(39, 55, 49, 255);
        private static readonly Color Rope = new Color32(193, 156, 112, 255);
        private static readonly Color Light = new Color32(255, 240, 179, 255);
        public InventoryToolGlyph(bool isLantern)
        {
            lantern = isLantern; pickingMode = PickingMode.Ignore; generateVisualContent += Draw;
        }
        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            // Panel scaling can resolve a 48px box just below 48; keep its intended 2x glyph.
            float scale = Mathf.Floor(Mathf.Round(Mathf.Min(contentRect.width, contentRect.height)) / 24f);
            if (scale < 1) return;
            Vector2 origin = new Vector2((contentRect.width - 24 * scale) / 2, (contentRect.height - 24 * scale) / 2);
            void Box(int x, int y, int w, int h, Color color)
            {
                p.fillColor = color; p.BeginPath();
                p.MoveTo(origin + new Vector2(x, y) * scale); p.LineTo(origin + new Vector2(x + w, y) * scale);
                p.LineTo(origin + new Vector2(x + w, y + h) * scale); p.LineTo(origin + new Vector2(x, y + h) * scale);
                p.ClosePath(); p.Fill();
            }
            if (lantern)
            {
                Box(9, 2, 6, 1, Ink); Box(8, 3, 2, 5, Ink); Box(14, 3, 2, 5, Ink);
                Box(6, 8, 12, 2, Ink); Box(5, 10, 14, 9, Ink); Box(6, 19, 12, 3, Ink);
                Box(7, 10, 10, 9, new Color32(104, 146, 125, 255)); Box(9, 11, 6, 7, Light);
                Box(11, 9, 2, 11, Ink); Box(7, 20, 10, 1, Rope); Box(7, 8, 10, 1, Rope);
            }
            else
            {
                Box(6, 4, 12, 2, Ink); Box(4, 6, 16, 12, Ink); Box(6, 18, 12, 2, Ink);
                Box(6, 6, 12, 12, Rope); Box(8, 8, 8, 8, Ink); Box(10, 10, 4, 4, Rope);
                Box(6, 6, 10, 1, Light); Box(7, 15, 1, 3, Light); Box(10, 10, 3, 1, Light);
                Box(14, 13, 3, 8, Ink); Box(15, 14, 1, 8, Rope); Box(15, 21, 5, 2, Ink); Box(16, 21, 4, 1, Rope);
            }
        }
    }
}
