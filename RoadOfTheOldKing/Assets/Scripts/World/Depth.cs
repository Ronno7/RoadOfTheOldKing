using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // One depth for everything that stands in the world (user, 23 Nov: "go with codex's cleaner long term approach";
    // DeveloperNotes/DesignRoom/2026-11-23-depth-sorting.md): the Standing sorting layer, ordered by where each thing
    // meets the ground. Rows are one art pixel deep, farther north drawn first, and each row has 8 slots (16 rows a unit
    // times 8 slots keeps every scene, all within 130 units of the origin, inside Unity's 16-bit order range). Static art
    // takes a slot from its x and a fixed z (TieDepth), so two things on one row always draw in the same order (a tie left
    // to the renderer flipped as the hero walked). Moving bodies (the hero, enemies) take the middle slot, and their
    // attachments (carried axe, grip fold, dust, spear) sit a slot or two either side, inside their own row.
    public static class Depth
    {
        public const string LayerName = "Standing";
        public const int RowsPerUnit = 16, Slots = 8, BodySlot = 4;
        // Grass sits just in front of a body on its own row: leaves rooted at the hero's feet cover his feet.
        public const int GrassSlot = BodySlot + 1;

        public static int Layer => SortingLayer.NameToID(LayerName);

        public static int Row(float ground) => -Mathf.RoundToInt(ground * RowsPerUnit);

        public static int Order(float ground, int slot) =>
            Mathf.Clamp(Row(ground) * Slots + Mathf.Clamp(slot, 0, Slots - 1), short.MinValue, short.MaxValue);

        // A half-unit-wide slot that repeats every four units.
        public static int SlotAt(float x) => Mathf.Clamp((int)(Mathf.Repeat(x, 4f) * 2f), 0, Slots - 1);

        public static void Apply(Renderer art, float ground, int slot)
        {
            int layer = Layer, order = Order(ground, slot);
            if (art.sortingLayerID != layer) art.sortingLayerID = layer;
            if (art.sortingOrder != order) art.sortingOrder = order;
        }

        // The last tiebreak for static art: two things on one row whose x slots happen to match (one in 16) would be left
        // to the renderer, which can swap them from frame to frame; a fixed z from where each stands and what it is keeps
        // them in one order. Under a hundredth of a unit, far inside the camera's range.
        public static float TieDepth(float x, string name)
        {
            int hash = 17;
            unchecked { foreach (char c in name) hash = hash * 31 + c; }
            return Mathf.Repeat(x * 7.31f, 1f) * .009f + (hash & 1023) * 1e-6f;
        }

        public static void Settle(Transform art, float x, string name)
        {
            var position = art.position;
            float z = TieDepth(x, name);
            if (Mathf.Abs(position.z - z) > 1e-7f) art.position = new Vector3(position.x, position.y, z);
        }
    }
}
