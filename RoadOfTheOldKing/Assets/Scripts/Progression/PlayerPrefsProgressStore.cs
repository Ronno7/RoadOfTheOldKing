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
            string json = PlayerPrefs.GetString(key);
            var state = JsonUtility.FromJson<ProgressState>(json);
            if (state == null || state.version != 1 || state.discoveredFires == null || state.completedIds == null)
                throw new InvalidOperationException("Unrecognized prototype save.");
            // Saves written before the axe rename store ownership as "hasHatchet"; JsonUtility
            // ignores FormerlySerializedAs, so carry it over here. The next save writes "hasAxe".
            if (!state.hasAxe && json.Contains("\"hasHatchet\":true")) state.hasAxe = true;
            // Existing version-one saves predate upgrades and start with an empty choice list.
            if (state.upgrades == null) state.upgrades = new System.Collections.Generic.List<UpgradeSelection>();
            if (state.sunShards < 0 || state.upgrades.Exists(s => s == null ||
                string.IsNullOrEmpty(s.tierId) || string.IsNullOrEmpty(s.upgradeId)))
                throw new InvalidOperationException("Invalid weapon progression.");
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
