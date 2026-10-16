using RoadOfTheOldKing.Player;

namespace RoadOfTheOldKing.World
{
    public interface ICoinPickupSource
    {
        bool TryCollect(PlayerHealth player);
    }
}
