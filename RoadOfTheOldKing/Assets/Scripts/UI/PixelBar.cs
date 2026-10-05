using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A meter drawn as whole art pixels for world-space documents, in the corner vitals' style: a
    // notched charcoal outline, a dark track and a fill with a light top row. Length and thickness are
    // the fill's size in art pixels; pixelSize is panel px per art pixel (2 on WorldPanel).
    [UxmlElement]
    public partial class PixelBar : VisualElement
    {
        private int length = 16, thickness = 2;
        private float pixelSize = 2f;
        private int filled = -1;

        public Color32 OutlineColor = new Color32(0x28, 0x23, 0x23, 0xFF);
        public Color32 TrackColor = new Color32(0x3A, 0x25, 0x26, 0xFF);
        public Color32 LightColor = new Color32(0xD6, 0x70, 0x60, 0xFF);
        public Color32 FillColor = new Color32(0xB2, 0x48, 0x40, 0xFF);

        [UxmlAttribute, Tooltip("Fill length in art pixels.")]
        public int Length { get => length; set { length = Mathf.Max(2, value); Resize(); } }
        [UxmlAttribute, Tooltip("Fill rows in art pixels; the top row is the light one.")]
        public int Thickness { get => thickness; set { thickness = Mathf.Max(1, value); Resize(); } }
        [UxmlAttribute, Tooltip("Panel pixels per art pixel.")]
        public float PixelSize { get => pixelSize; set { pixelSize = Mathf.Max(.5f, value); Resize(); } }

        public PixelBar()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            Resize();
        }

        // 0..1, shown in whole pixels; anything above zero shows at least one pixel.
        public void SetValue(float fraction)
        {
            fraction = Mathf.Clamp01(fraction);
            int pixels = fraction <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(fraction * length));
            if (pixels == filled) return;
            filled = pixels;
            MarkDirtyRepaint();
        }

        private void Resize()
        {
            style.width = (length + 2) * pixelSize;
            style.height = (thickness + 2) * pixelSize;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            int width = length + 2, height = thickness + 2;
            // Outline: top and bottom rows and the end columns, without the four corners (notched).
            int quads = 2 * length + 2 * thickness + length * thickness;
            var mesh = context.Allocate(quads * 4, quads * 6);
            ushort index = 0;
            for (int x = 1; x < width - 1; x++)
            {
                Quad(mesh, x, 0, OutlineColor, ref index);
                Quad(mesh, x, height - 1, OutlineColor, ref index);
            }
            for (int y = 1; y < height - 1; y++)
            {
                Quad(mesh, 0, y, OutlineColor, ref index);
                Quad(mesh, width - 1, y, OutlineColor, ref index);
            }
            int shown = Mathf.Max(0, filled);
            for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                Quad(mesh, x, y, x - 1 < shown ? (y == 1 ? LightColor : FillColor) : TrackColor, ref index);
        }

        private void Quad(MeshWriteData mesh, int x, int y, Color32 color, ref ushort index)
        {
            float left = x * pixelSize, top = y * pixelSize, s = pixelSize;
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
