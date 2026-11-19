using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Low cover the hero can't push through but a thrown axe clears: its colliders stop bodies, never the axe's flight,
    // aim guide or Recall (AxeHitDetector). First used for the hedge before the Tutorial's Recall stone (user, 18 Nov:
    // testers threw at melee range and never saw the axe fly back), so the throw is always made from a distance.
    // Place it only where the art reads as low: a hedge, undergrowth, a low rubble line.
    [DisallowMultipleComponent]
    public sealed class ThrowOver : MonoBehaviour { }
}
