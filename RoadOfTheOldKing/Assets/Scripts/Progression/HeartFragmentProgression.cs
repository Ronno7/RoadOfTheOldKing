using System;

namespace TheLostShrine.Progression
{
    // Derive health from unique reward IDs: no second saved counter can drift out of sync.
    public static class HeartFragmentProgression
    {
        public const int FragmentsPerHeart = 3;
        public const int HealthPerHeart = 20;
        private const string Prefix = "heart/collected/";

        public static int Count(ProgressState state)
        {
            int count = 0;
            foreach (var id in state.completedIds)
                if (id != null && id.StartsWith(Prefix, StringComparison.Ordinal)) count++;
            return count;
        }
        public static int BonusHealth(ProgressState state) => Count(state) / FragmentsPerHeart * HealthPerHeart;
        public static bool IsCollected(ProgressState state, string id) => state.Has(Prefix + id);

        public static bool TryCollect(ProgressState state, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || IsCollected(state, id)) return false;
            state.Complete(Prefix + id);
            return true;
        }
    }
}
