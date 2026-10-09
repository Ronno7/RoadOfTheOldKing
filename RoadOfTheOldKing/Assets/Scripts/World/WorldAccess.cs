using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    internal static class WorldAccess
    {
        public static bool IsClear(PlayerHealth player, Transform target, List<RaycastHit2D> hits)
        {
            if (player == null || target == null) return false;
            Physics2D.Linecast(player.transform.position, target.position,
                new ContactFilter2D { useTriggers = false }, hits);
            foreach (var hit in hits)
                if (hit.collider != null && !hit.collider.transform.IsChildOf(player.transform) &&
                    !hit.collider.transform.IsChildOf(target)) return false;
            return true;
        }
    }
}
