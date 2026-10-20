using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A townsperson's clips: standing views, a breathing idle and a walk, each in up to eight directions.
    [CreateAssetMenu(menuName = "Road of the Old King/NPC Animation Set")]
    public sealed class NpcAnimationSet : ScriptableObject
    {
        public DirectionalSpriteAnimation rotations, idle, walk;
    }
}
