using UnityEditor;

namespace RoadOfTheOldKing.EditorTools
{
    public static class GreenLowlandsGroundKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Ground";

        [MenuItem("Tools/Road of the Old King/Build Green Lowlands Ground Kit")]
        public static void Build()
        {
            TutorialGroundKitBuilder.BuildKit(Root, "GreenLowlandsGround", quietGrassWeight: 8);
        }
    }
}
