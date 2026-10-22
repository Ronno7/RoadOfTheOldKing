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
        private readonly IPrefsBackend prefs;

        // prefs: PlayerPrefs unless verification substitutes a backend with separate cached and durable state.
        public PlayerPrefsProgressStore(string key, string[] legacyKeys = null, IPrefsBackend prefs = null)
        {
            this.key = key;
            this.legacyKeys = legacyKeys ?? Array.Empty<string>();
            this.prefs = prefs ?? UnityPrefs.Instance;
        }

        public ProgressState Load()
        {
            if (prefs.HasKey(key))
            {
                var state = ProgressMigrations.Read(prefs.GetString(key), out bool migrated);
                if (migrated) Save(state);
                return state;
            }
            foreach (var legacy in legacyKeys)
            {
                if (string.IsNullOrEmpty(legacy) || legacy == key || !prefs.HasKey(legacy)) continue;
                var state = ProgressMigrations.Read(prefs.GetString(legacy), out _);
                Save(state);
                return state;
            }
            return new ProgressState();
        }

        public void Save(ProgressState state)
        {
            state.version = ProgressState.CurrentVersion;
            prefs.SetString(key, JsonUtility.ToJson(state));
            prefs.Save();
        }

        public void Clear()
        {
            prefs.DeleteKey(key);
            prefs.DeleteKey(ResumeKey);
            foreach (var legacy in legacyKeys)
                if (!string.IsNullOrEmpty(legacy)) prefs.DeleteKey(legacy);
            prefs.Save();
        }

        // The resume point is its own small record beside the save, written often without touching the save.
        private string ResumeKey => key + ".Resume";

        public ResumePoint LoadResume()
        {
            if (!prefs.HasKey(ResumeKey)) return null;
            var resume = JsonUtility.FromJson<ResumePoint>(prefs.GetString(ResumeKey));
            return resume != null && !string.IsNullOrEmpty(resume.scenePath) ? resume : null;
        }

        public void SaveResume(ResumePoint resume)
        {
            prefs.SetString(ResumeKey, JsonUtility.ToJson(resume));
            prefs.Save();
        }

        // Always delete and flush, even when the cached key is already gone: an earlier attempt may have removed it
        // from the cache and then failed to write that to disk, and only a new flush finishes the clear.
        public void ClearResume()
        {
            prefs.DeleteKey(ResumeKey);
            prefs.Save();
        }
    }

    // The PlayerPrefs calls the store makes. PlayerPrefs keeps a cache that Save() flushes to disk, so a failure can
    // leave the two different; verification models that with its own backend.
    public interface IPrefsBackend
    {
        bool HasKey(string key);
        string GetString(string key);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }

    internal sealed class UnityPrefs : IPrefsBackend
    {
        public static readonly UnityPrefs Instance = new UnityPrefs();
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key) => PlayerPrefs.GetString(key);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Save() => PlayerPrefs.Save();
    }
}
