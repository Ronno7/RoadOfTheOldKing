using System;
using System.Collections.Generic;

namespace RoadOfTheOldKing.Progression
{
    // The whole save. Bump CurrentVersion and add a step to ProgressMigrations when the shape changes.
    [Serializable]
    public sealed class ProgressState
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public bool hasAxe;
        public bool recallUnlocked;
        // Last rest/travel fire and the scene that holds it; respawn and launch load that scene.
        public string checkpointId = "";
        public string scenePath = "";
        public List<FireRecord> fires = new List<FireRecord>();
        public List<string> completedIds = new List<string>();
        public int sunShards;
        public List<UpgradeSelection> upgrades = new List<UpgradeSelection>();

        public bool Has(string id) => !string.IsNullOrEmpty(id) && completedIds.Contains(id);
        public void Complete(string id)
        {
            if (!string.IsNullOrEmpty(id) && !completedIds.Contains(id))
                completedIds.Add(id);
        }

        public bool HasFire(string id) => FindFire(id) != null;
        public FireRecord FindFire(string id) => string.IsNullOrEmpty(id) ? null : fires.Find(f => f != null && f.id == id);

        // Records (or refreshes) a lit fire so travel can list it from any scene.
        public void DiscoverFire(string id, string scene, string displayName, int displayOrder)
        {
            if (string.IsNullOrEmpty(id)) return;
            var fire = FindFire(id);
            if (fire == null) fires.Add(fire = new FireRecord { id = id });
            fire.scenePath = scene;
            fire.displayName = displayName;
            fire.displayOrder = displayOrder;
        }
    }

    // A discovered bonfire. Name and order are copied so fires in unloaded scenes can be listed.
    [Serializable]
    public sealed class FireRecord
    {
        public string id = "";
        public string scenePath = "";
        public string displayName = "";
        public int displayOrder;
    }

    public interface IProgressParticipant
    {
        void CaptureProgress(ProgressState state);
        void RestoreProgress(ProgressState state);
    }

    public interface IResetOnRest { void ResetOnRest(); }

    // Where Continue resumes (Dark Souls model, user 1 Oct): the exact spot the player left, with the health
    // and flasks they had. Kept apart from ProgressState so the frequent position autosave writes this small
    // record without capturing the scene or rewriting the save. Death, rest, travel, return and New Game clear it.
    [Serializable]
    public sealed class ResumePoint
    {
        public string scenePath = "";
        public float x, y;
        public int health = -1;
        public int flasks = -1;
    }

    public interface IProgressStore
    {
        ProgressState Load();
        void Save(ProgressState state);
        // Clears the progress and the resume point.
        void Clear();
        ResumePoint LoadResume();
        void SaveResume(ResumePoint resume);
        void ClearResume();
    }
}
