using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // Limits how many enemies commit to an attack on the player at once, so a pack takes turns
    // instead of striking together. Holders release on recovery, stagger, defeat, reset or disable.
    public static class AttackTokens
    {
        public static int MaxAttackers = 1;
        private static readonly HashSet<object> holders = new HashSet<object>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { holders.Clear(); MaxAttackers = 1; }

        public static bool TryAcquire(object owner)
        {
            if (holders.Contains(owner)) return true;
            if (holders.Count >= MaxAttackers) return false;
            holders.Add(owner);
            return true;
        }

        public static void Release(object owner) => holders.Remove(owner);
        public static bool Holds(object owner) => holders.Contains(owner);
    }
}
