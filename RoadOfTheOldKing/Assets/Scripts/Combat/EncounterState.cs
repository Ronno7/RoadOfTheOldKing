using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // Shared "in combat" signal for presentation (HUD now; camera and player posture later).
    // In combat while any living enemy is aware of the player, plus a grace period after the last
    // one loses the player or dies. Multiple pursuers never fight over one flag.
    public static class EncounterState
    {
        public const float GraceSeconds = 3f;
        private static readonly List<SimpleMeleeEnemy> enemies = new List<SimpleMeleeEnemy>();
        private static float lastThreatAt = float.NegativeInfinity;
        private static float refreshAt;
        private static int frame = -1;
        private static bool engaged;

        public static bool InCombat { get { Update(); return engaged || Time.time - lastThreatAt < GraceSeconds; } }
        public static bool HasActiveThreat { get { Update(); return engaged; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { enemies.Clear(); lastThreatAt = float.NegativeInfinity; refreshAt = 0f; frame = -1; engaged = false; }

        private static void Update()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            // Enemy lists are small; refresh occasionally rather than per widget.
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 1f;
                enemies.Clear();
                enemies.AddRange(Object.FindObjectsByType<SimpleMeleeEnemy>(FindObjectsSortMode.None));
            }
            engaged = false;
            foreach (var enemy in enemies)
                if (enemy != null && enemy.isActiveAndEnabled && enemy.IsAware && enemy.State != MeleeEnemyState.Defeated)
                    engaged = true;
            if (engaged) lastThreatAt = Time.time;
        }

        // A hit on the player counts as a threat even from something the list does not track.
        public static void ReportThreat() => lastThreatAt = Time.time;
    }
}
