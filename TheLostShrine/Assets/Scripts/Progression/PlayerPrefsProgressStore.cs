using System;
using UnityEngine;

namespace TheLostShrine.Progression
{
    // One small versioned JSON record. PlayerPrefs also supports browser builds.
    public sealed class PlayerPrefsProgressStore : IProgressStore
    {
        private readonly string key;
        public PlayerPrefsProgressStore(string key) => this.key = key;

        public ProgressState Load()
        {
            if (!PlayerPrefs.HasKey(key))
                return new ProgressState();
            var state = JsonUtility.FromJson<ProgressState>(PlayerPrefs.GetString(key));
            if (state == null || state.version != 1 || state.discoveredFires == null || state.completedIds == null)
                throw new InvalidOperationException("Unrecognized prototype save.");
            return state;
        }

        public void Save(ProgressState state)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(state));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
