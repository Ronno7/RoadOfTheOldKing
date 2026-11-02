using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Imported native frames: raised, release, four lowering poses, landed.
    public sealed class RecallReleaseArt : ScriptableObject
    {
        public Sprite[] floor, backRail, frontRail, hinge, release;
    }
}
