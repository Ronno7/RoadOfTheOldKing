using System;
using System.Collections.Generic;

namespace TheLostShrine.Progression
{
    [Serializable]
    public sealed class ProgressState
    {
        public int version = 1;
        public bool hasHatchet;
        public bool recallUnlocked;
        public string checkpointId = "";
        public string scenePath = "";
        public List<string> discoveredFires = new List<string>();
        public List<string> completedIds = new List<string>();
        public int sunShards;
        public List<UpgradeSelection> upgrades = new List<UpgradeSelection>();

        public bool Has(string id) => !string.IsNullOrEmpty(id) && completedIds.Contains(id);
        public void Complete(string id)
        {
            if (!string.IsNullOrEmpty(id) && !completedIds.Contains(id))
                completedIds.Add(id);
        }
    }

    public interface IProgressParticipant
    {
        void CaptureProgress(ProgressState state);
        void RestoreProgress(ProgressState state);
    }

    public interface IResetOnRest { void ResetOnRest(); }

    public interface IProgressStore
    {
        ProgressState Load();
        void Save(ProgressState state);
        void Clear();
    }
}
