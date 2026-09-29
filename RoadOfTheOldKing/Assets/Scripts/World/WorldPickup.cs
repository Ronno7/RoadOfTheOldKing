using System.Collections.Generic;
using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.World
{
    // Discovery is separate from collection so a future pickup animation can commit
    // through TryCollect at its contact marker, rechecking availability and distance.
    public abstract class WorldPickup : MonoBehaviour
    {
        private static readonly HashSet<WorldPickup> active = new HashSet<WorldPickup>();
        public static IEnumerable<WorldPickup> Active => active;
        public abstract string Prompt { get; }
        protected virtual float PickupDistance => 1.5f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => active.Clear();
        protected virtual void OnEnable() => active.Add(this);
        protected virtual void OnDisable() => active.Remove(this);

        public virtual bool CanCollect(PlayerHealth player) => isActiveAndEnabled &&
            player != null && player.IsAlive &&
            Vector2.Distance(player.transform.position, transform.position) <= PickupDistance;
        public abstract bool TryCollect(PlayerHealth player);
    }
}
