using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    // A meter drawn as an arc of whole art pixels (no anti-aliasing), centred on straight up, for
    // world-space documents. The fill runs from the left end; a debt (stamina below zero) runs red from
    // the left end over a dark red track. Geometry is in art pixels; pixelSize is panel px per art pixel
    // (2 on WorldPanel). The element sizes itself to the arc's pixels, so a bottom-centre pivot places
    // its lowest pixel.
    [UxmlElement]
    public partial class PixelArc : VisualElement
    {
        private struct Cell { public int x, y; public float t; public bool outer; }

        private int radius = 18, thickness = 2;
        private float span = 110f, pixelSize = 2f;
        private float fill = 1f, debt;
        private bool flash;
        private readonly List<Cell> band = new List<Cell>();
        private readonly List<Vector2Int> outline = new List<Vector2Int>();
        private int minX, maxY, columns, rows;

        public Color32 OutlineColor = new Color32(0x28, 0x23, 0x23, 0xFF);
        public Color32 FlashColor = new Color32(0xF6, 0xE6, 0xB5, 0xFF);
        public Color32 TrackColor = new Color32(0x3B, 0x33, 0x26, 0xFF);
        public Color32 LightColor = new Color32(0xEB, 0xC3, 0x5F, 0xFF);
        public Color32 FillColor = new Color32(0xCF, 0x95, 0x35, 0xFF);
        public Color32 DebtTrackColor = new Color32(0x5C, 0x22, 0x1E, 0xFF);
        public Color32 DebtColor = new Color32(0xA8, 0x3A, 0x2E, 0xFF);

        [UxmlAttribute, Tooltip("Inner radius in art pixels.")]
        public int Radius { get => radius; set { radius = Mathf.Max(1, value); Rebuild(); } }
        [UxmlAttribute, Tooltip("Fill rows in art pixels (the outline adds one on each side).")]
        public int Thickness { get => thickness; set { thickness = Mathf.Max(1, value); Rebuild(); } }
        [UxmlAttribute, Tooltip("Arc length in degrees, centred on straight up.")]
        public float Span { get => span; set { span = Mathf.Clamp(value, 10f, 360f); Rebuild(); } }
        [UxmlAttribute, Tooltip("Panel pixels per art pixel.")]
        public float PixelSize { get => pixelSize; set { pixelSize = Mathf.Max(.5f, value); Rebuild(); } }

        public PixelArc()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            Rebuild();
        }

        // Fill and debt are 0..1; redraws only when the visible pixels change.
        public void SetValue(float fill01, float debt01, bool flashOutline)
        {
            int cells = Mathf.Max(1, CountColumns());
            float f = Mathf.Round(Mathf.Clamp01(fill01) * cells) / cells, d = Mathf.Round(Mathf.Clamp01(debt01) * cells) / cells;
            if (f == fill && d == debt && flashOutline == flash) return;
            fill = f; debt = d; flash = flashOutline;
            MarkDirtyRepaint();
        }

        private int CountColumns() => Mathf.CeilToInt((radius + thickness * .5f) * span * Mathf.Deg2Rad);

        private void Rebuild()
        {
            band.Clear(); outline.Clear();
            int reach = radius + thickness + 1;
            var inBand = new HashSet<Vector2Int>();
            for (int y = -reach; y < reach; y++)
            for (int x = -reach; x < reach; x++)
            {
                // Cell centres sit half a pixel off the centre lines, so the arc is symmetric.
                float cx = x + .5f, cy = y + .5f, d = Mathf.Sqrt(cx * cx + cy * cy);
                float angle = Mathf.Atan2(cx, cy) * Mathf.Rad2Deg; // 0 = straight up, positive = right
                if (d < radius || d >= radius + thickness || Mathf.Abs(angle) > span * .5f) continue;
                band.Add(new Cell { x = x, y = y, t = (angle + span * .5f) / span, outer = d >= radius + thickness - 1 });
                inBand.Add(new Vector2Int(x, y));
            }
            var seen = new HashSet<Vector2Int>();
            foreach (var cell in inBand)
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var n = new Vector2Int(cell.x + dx, cell.y + dy);
                    if (!inBand.Contains(n) && seen.Add(n)) outline.Add(n);
                }
            minX = -reach; columns = reach * 2;
            int top = int.MinValue, bottom = int.MaxValue;
            foreach (var n in outline) { top = Mathf.Max(top, n.y); bottom = Mathf.Min(bottom, n.y); }
            maxY = top; rows = top - bottom + 1;
            style.width = columns * pixelSize;
            style.height = rows * pixelSize;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            int quads = band.Count + outline.Count;
            if (quads == 0) return;
            var mesh = context.Allocate(quads * 4, quads * 6);
            ushort index = 0;
            foreach (var n in outline) Quad(mesh, n.x, n.y, flash ? FlashColor : OutlineColor, ref index);
            foreach (var cell in band)
            {
                Color32 color;
                if (debt > 0f) color = cell.t < debt ? DebtColor : DebtTrackColor;
                else if (cell.t < fill || fill >= 1f) color = cell.outer ? LightColor : FillColor;
                else color = TrackColor;
                Quad(mesh, cell.x, cell.y, color, ref index);
            }
        }

        private void Quad(MeshWriteData mesh, int x, int y, Color32 color, ref ushort index)
        {
            float left = (x - minX) * pixelSize, top = (maxY - y) * pixelSize, s = pixelSize;
            mesh.SetNextVertex(new Vertex { position = new Vector3(left, top, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(left + s, top, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(left + s, top + s, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex { position = new Vector3(left, top + s, Vertex.nearZ), tint = color });
            mesh.SetNextIndex(index); mesh.SetNextIndex((ushort)(index + 1)); mesh.SetNextIndex((ushort)(index + 2));
            mesh.SetNextIndex(index); mesh.SetNextIndex((ushort)(index + 2)); mesh.SetNextIndex((ushort)(index + 3));
            index += 4;
        }
    }
}
