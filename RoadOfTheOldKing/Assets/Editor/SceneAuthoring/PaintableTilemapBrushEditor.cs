using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;

namespace RoadOfTheOldKing.EditorTools
{
    // Every tree, rock and building prefab carries its own small tilemaps, which would fill the Tile Palette's
    // Active Tilemap list with hundreds of maps nobody paints. List only the scene's own maps. Prefab Mode still
    // shows a prefab's maps, because they aren't instances there.
    [CustomEditor(typeof(GridBrush))]
    public sealed class PaintableTilemapBrushEditor : GridBrushEditor
    {
        public override GameObject[] validTargets =>
            base.validTargets?.Where(target => !PrefabUtility.IsPartOfPrefabInstance(target)).ToArray();
    }
}
