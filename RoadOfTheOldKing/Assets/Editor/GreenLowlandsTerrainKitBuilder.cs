using UnityEditor;

namespace RoadOfTheOldKing.EditorTools
{
    public static class GreenLowlandsTerrainKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Terrain";

        [MenuItem("Tools/Road of the Old King/Build Green Lowlands Terrain Kit")]
        public static void Build()
        {
            // Use the shared blank zone template without rebuilding Tutorial assets.
            TutorialTerrainKitBuilder.BuildKit(Root, "GreenLowlandsTerrain");
        }
    }
}
