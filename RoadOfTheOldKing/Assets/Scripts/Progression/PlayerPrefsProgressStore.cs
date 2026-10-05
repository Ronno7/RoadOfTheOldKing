using System;
using UnityEngine;

namespace RoadOfTheOldKing.Progression
{
    // One small versioned JSON record. PlayerPrefs also supports browser builds.
    // When the key is empty, the first legacy key holding a save is migrated into it once; the legacy
    // record is left untouched as a backup and cleared with the main save on New Game.
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private readonly string key;
        private readonly string[] legacyKeys;

        public PlayerPrefsProgressStore(string key, string[] legacyKeys = null)
        {
            this.key = key;
            this.legacyKeys = legacyKeys ?? Array.Empty<string>();
        }

        public ProgressState Load()
        {
            if (PlayerPrefs.HasKey(key))
            {
                var state = ProgressMigrations.Read(PlayerPrefs.GetString(key), out bool migrated);
                if (migrated) Save(state);
                return state;
            }
            foreach (var legacy in legacyKeys)
            {
                if (string.IsNullOrEmpty(legacy) || legacy == key || !PlayerPrefs.HasKey(legacy)) continue;
                var state = ProgressMigrations.Read(PlayerPrefs.GetString(legacy), out _);
                Save(state);
                return state;
            }
            return new ProgressState();
        }

        public void Save(ProgressState state)
        {
            state.version = ProgressState.CurrentVersion;
            PlayerPrefs.SetString(key, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.DeleteKey(ResumeKey);
            foreach (var legacy in legacyKeys)
                if (!string.IsNullOrEmpty(legacy)) PlayerPrefs.DeleteKey(legacy);
            PlayerPrefs.Save();
        }

        // The resume point is its own small record beside the save, written often without touching the save.
        private string ResumeKey => key + ".Resume";

        public ResumePoint LoadResume()
        {
            if (!PlayerPrefs.HasKey(ResumeKey)) return null;
            var resume = JsonUtility.FromJson<ResumePoint>(PlayerPrefs.GetString(ResumeKey));
            return resume != null && !string.IsNullOrEmpty(resume.scenePath) ? resume : null;
        }

        public void SaveResume(ResumePoint resume)
        {
            PlayerPrefs.SetString(ResumeKey, JsonUtility.ToJson(resume));
            PlayerPrefs.Save();
        }

        public void ClearResume()
        {
            if (!PlayerPrefs.HasKey(ResumeKey)) return;
            PlayerPrefs.DeleteKey(ResumeKey);
            PlayerPrefs.Save();
        }
    }
}
