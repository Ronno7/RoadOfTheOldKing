using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Shared "in combat" signal for presentation (HUD now; camera and player posture later).
    // In combat while any living registered enemy is aware of the player, plus a grace period after
    // the last one loses the player or dies. Multiple pursuers never fight over one flag.
    public static class EncounterState
    {
        public const float GraceSeconds = 3f;
        private static float lastThreatAt = float.NegativeInfinity;
        private static int frame = -1;
        private static bool engaged;

        public static bool InCombat { get { Update(); return engaged || Time.time - lastThreatAt < GraceSeconds; } }
        public static bool HasActiveThreat { get { Update(); return engaged; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { lastThreatAt = float.NegativeInfinity; frame = -1; engaged = false; }

        private static void Update()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            engaged = false;
            foreach (var enemy in EnemyRegistry.Active)
                if (enemy.IsAware && !enemy.IsDefeated) { engaged = true; break; }
            if (engaged) lastThreatAt = Time.time;
        }

        // A hit on the player counts as a threat even from something that is not a registered enemy.
        public static void ReportThreat() => lastThreatAt = Time.time;
    }
}
