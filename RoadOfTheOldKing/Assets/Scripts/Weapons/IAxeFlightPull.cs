using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // Bends an owned axe's outward flight toward a point while it is inside some area.
    public interface IAxeFlightPull
    {
        bool TryPull(AxeWeapon axe, Vector2 position, out Vector2 target);
    }
}
