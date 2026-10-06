using UnityEditor;

namespace RoadOfTheOldKing.EditorTools
{
    public static class GreenLowlandsPathsKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Paths";

        [MenuItem("Tools/Road of the Old King/Build Green Lowlands Paths Kit")]
        public static void Build()
        {
            TutorialPathsKitBuilder.BuildKit(Root, "GreenLowlandsPaths",
                GreenLowlandsGroundKitBuilder.Root, includeWornStone: true);
        }
    }
}
