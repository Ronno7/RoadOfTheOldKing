using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.Progression
{
    // Reads any known save version and upgrades it step by step to ProgressState.CurrentVersion.
    // Each retired version keeps a small record of its own shape; never edit one after shipping.
    public static class ProgressMigrations
    {
        [Serializable] private sealed class VersionProbe { public int version; }

        // Version 1 (0.4.7 and earlier): one save per scene key, fires as a bare id list.
        [Serializable]
        private sealed class SaveV1
        {
            public int version;
            public bool hasAxe;
            public bool hasHatchet;
            public bool recallUnlocked;
            public string checkpointId = "";
            public string scenePath = "";
            public List<string> discoveredFires;
            public List<string> completedIds;
            public int sunShards;
            public List<UpgradeSelection> upgrades;
        }

        public static ProgressState Read(string json, out bool migrated)
        {
            var probe = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<VersionProbe>(json);
            if (probe == null) throw new InvalidOperationException("Unreadable save.");
            migrated = probe.version != ProgressState.CurrentVersion;
            ProgressState state;
            switch (probe.version)
            {
                case 1: state = FromV1(JsonUtility.FromJson<SaveV1>(json)); break;
                case ProgressState.CurrentVersion: state = JsonUtility.FromJson<ProgressState>(json); break;
                default: throw new InvalidOperationException("Unknown save version " + probe.version + ".");
            }
            Validate(state);
            return state;
        }

        private static ProgressState FromV1(SaveV1 old)
        {
            if (old == null || old.discoveredFires == null || old.completedIds == null)
                throw new InvalidOperationException("Unrecognized version 1 save.");
            var state = new ProgressState
            {
                // Saves written before the axe rename store ownership as "hasHatchet".
                hasAxe = old.hasAxe || old.hasHatchet,
                recallUnlocked = old.recallUnlocked,
                checkpointId = old.checkpointId ?? "",
                scenePath = old.scenePath ?? "",
                completedIds = old.completedIds,
                sunShards = old.sunShards,
                // Saves from before upgrades have no choice list.
                upgrades = old.upgrades ?? new List<UpgradeSelection>(),
            };
            // A version 1 save belonged to one scene, and fires are only discovered by resting,
            // so every discovered fire lives in the checkpoint's scene. Names refresh on the next rest.
            foreach (var id in old.discoveredFires)
                state.DiscoverFire(id, state.scenePath, "", 0);
            return state;
        }

        private static void Validate(ProgressState state)
        {
            if (state == null || state.fires == null || state.completedIds == null || state.upgrades == null)
                throw new InvalidOperationException("Incomplete save.");
            if (state.sunShards < 0 || state.upgrades.Exists(s => s == null ||
                string.IsNullOrEmpty(s.tierId) || string.IsNullOrEmpty(s.upgradeId)))
                throw new InvalidOperationException("Invalid weapon progression.");
            state.fires.RemoveAll(f => f == null || string.IsNullOrEmpty(f.id));
            state.checkpointId ??= "";
            state.scenePath ??= "";
        }
    }
}
