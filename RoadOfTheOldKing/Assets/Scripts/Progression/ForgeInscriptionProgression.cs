namespace RoadOfTheOldKing.Progression
{
    // Permanent knowledge, stored in the existing milestone list. F1..F8 are stable catalog
    // numbers independent of scene names. Count only these eight exact IDs, once each.
    public static class ForgeInscriptionProgression
    {
        public const int SiteCount = 8;
        public const int RequiredKnowledge = 6;

        public static bool IsValidSite(int site) => site >= 1 && site <= SiteCount;
        public static string KnowledgeId(int site) => IsValidSite(site) ? "forge/inscription/" + site : "";
        public static bool Knows(ProgressState state, int site) =>
            state != null && IsValidSite(site) && state.Has(KnowledgeId(site));

        public static int Count(ProgressState state)
        {
            int count = 0;
            for (int site = 1; site <= SiteCount; site++)
                if (Knows(state, site)) count++;
            return count;
        }

        // This is only the knowledge prerequisite. The future High Forge must also require
        // completed regular tiers and enforce one mutually exclusive special choice.
        public static bool HasRequiredKnowledge(ProgressState state) => Count(state) >= RequiredKnowledge;

        public static bool TryLearn(ProgressState state, int site)
        {
            if (state == null || !IsValidSite(site) || Knows(state, site)) return false;
            state.Complete(KnowledgeId(site));
            return true;
        }
    }
}
