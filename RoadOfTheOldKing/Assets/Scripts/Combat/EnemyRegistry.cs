using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // Enabled enemies, maintained by the enemies themselves, so shared systems never search the scene.
    public static class EnemyRegistry
    {
        private static readonly List<IEnemy> active = new List<IEnemy>();
        public static IReadOnlyList<IEnemy> Active => active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => active.Clear();

        public static void Register(IEnemy enemy) { if (enemy != null && !active.Contains(enemy)) active.Add(enemy); }
        public static void Unregister(IEnemy enemy) => active.Remove(enemy);
    }
}
