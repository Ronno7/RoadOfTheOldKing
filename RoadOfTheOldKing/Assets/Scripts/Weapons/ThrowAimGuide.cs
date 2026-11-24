using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // The aimed throw's guide (user, 23 Nov: "look into more thoughful design of the throw aim since its just a white line
    // atm"; chose chalk dashes after trying three in the playground: "go with A, have it pulse out forward at the same speed
    // that the axe flies"). Chalk dashes in the enemy warnings' chalk, drawn pixel by pixel on the art grid so diagonals
    // step like drawn pixel art; a brighter pulse runs out along them at the axe's own flight speed; a chalk X where the
    // throw will meet something, or a tick across the line at full range. Starts past the axe held out in front.
    public sealed class ThrowAimGuide : MonoBehaviour
    {
        private const float Px = 1f / 16f;
        private const float Start = 1.2f;        // past the held axe
        private const float PulseGap = .35f;     // seconds between pulses
        private const int PulseLength = 9;       // pixels the pulse spans: a dash and a half
        private const int PulseFront = 4;        // its leading pixels, where the dashes stretch into their gaps
        private const int Order = 598;           // just under the aim chevron (600) on the Player layer
        private static readonly Color Chalk = new Color32(236, 222, 190, 190), Bright = new Color32(248, 238, 212, 255),
            Shade = new Color32(112, 94, 104, 140);
        private static Sprite pixel;

        private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
        private readonly List<Vector2Int> cells = new List<Vector2Int>(256);
        private Material material;
        private int used;

        // For checks: what the guide shows now.
        public bool Showing => used > 0;
        public float Reach { get; private set; }
        public bool Blocked { get; private set; }

        public static ThrowAimGuide Create(Material material)
        {
            var guide = new GameObject("Throw aim guide").AddComponent<ThrowAimGuide>();
            guide.material = material;
            return guide;
        }

        public void Hide()
        {
            for (int i = 0; i < used; i++) pool[i].enabled = false;
            used = 0;
        }

        // origin: the hero; distance: where the axe will stop; blocked: it stops against something; speed: flight, units/s.
        public void Draw(Vector2 origin, Vector2 direction, float distance, bool blocked, float speed)
        {
            Hide();
            Reach = distance; Blocked = blocked;
            if (distance <= 0f) return;
            float from = Mathf.Min(Start, distance);
            Vector2 start = origin + direction * from, end = origin + direction * distance;
            Raster(start, end);

            // The pulse leaves the start as the axe would and reaches the end when the axe would, then rests a moment.
            float travel = (distance - from) / Mathf.Max(.1f, speed);
            float t = Mathf.Repeat(Time.time, travel + PulseGap);
            int head = travel > 0f && t <= travel ? Mathf.RoundToInt(t / travel * (cells.Count - 1)) : -1;
            bool arrived = t > travel && t < travel + .12f;

            if (distance > from)
            {
                for (int i = 0; i < cells.Count; i++)
                {
                    int behind = head - i;
                    if (head >= 0 && behind >= 0 && behind < PulseLength)
                    {
                        // The pulse is the chalk line itself lighting up as it passes, 1 px like the rest; at its front the
                        // dashes stretch a pixel into their gaps so it reads as it travels (user, 24 Nov: "make the pulse
                        // slightly increased in scale so you can see it travel along the line easier", then "looks like a blob
                        // of white instead of the chalk lines themselves", "the pulse should travel along the chalk line, not
                        // be a seperate blob").
                        bool front = behind < PulseFront;
                        if (front ? i % 6 == 4 : i % 6 >= 3) continue;
                        Put(cells[i] + Vector2Int.down, Shade, Order);
                        Put(cells[i], Bright, Order + 2);
                        continue;
                    }
                    if (i % 6 >= 3) continue;
                    Put(cells[i] + Vector2Int.down, Shade, Order);
                    Put(cells[i], Chalk, Order + 1);
                }
            }

            // The mark at the end pops a pixel larger as the pulse lands.
            var c = Cell(end);
            Color mark = arrived ? Bright : Chalk;
            int size = arrived ? 3 : 2;
            if (blocked)
                for (int k = -size; k <= size; k++)
                {
                    Put(c + new Vector2Int(k, k), mark, Order + 1);
                    Put(c + new Vector2Int(k, -k), mark, Order + 1);
                }
            else
            {
                var side = new Vector2(-direction.y, direction.x);
                for (int k = -size; k <= size; k++) Put(Cell(end + side * (k * Px)), mark, Order + 1);
            }
        }

        // Bresenham on the art-pixel grid.
        private void Raster(Vector2 a, Vector2 b)
        {
            cells.Clear();
            Vector2Int p = Cell(a), q = Cell(b);
            int dx = Mathf.Abs(q.x - p.x), dy = -Mathf.Abs(q.y - p.y), sx = p.x < q.x ? 1 : -1, sy = p.y < q.y ? 1 : -1, err = dx + dy;
            for (int guard = 0; guard < 1024; guard++)
            {
                cells.Add(p);
                if (p == q) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; p.x += sx; }
                if (e2 <= dx) { err += dx; p.y += sy; }
            }
        }

        private static Vector2Int Cell(Vector2 world) => new Vector2Int(Mathf.FloorToInt(world.x / Px), Mathf.FloorToInt(world.y / Px));

        private void Put(Vector2Int cell, Color colour, int order)
        {
            if (used == pool.Count) pool.Add(NewRenderer());
            var r = pool[used++];
            r.color = colour;
            r.sortingOrder = order;
            r.transform.position = new Vector3((cell.x + .5f) * Px, (cell.y + .5f) * Px, 0f);
            r.enabled = true;
        }

        private SpriteRenderer NewRenderer()
        {
            var go = new GameObject("Pixel");
            go.transform.SetParent(transform, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Pixel;
            if (material != null) r.sharedMaterial = material;
            r.sortingLayerID = SortingLayer.NameToID("Player");
            r.enabled = false;
            return r;
        }

        private static Sprite Pixel
        {
            get
            {
                if (pixel != null) return pixel;
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixel(0, 0, Color.white); tex.Apply();
                return pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 16f);
            }
        }
    }
}
