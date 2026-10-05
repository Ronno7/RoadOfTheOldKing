using System;
using System.Linq;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.Progression
{
    // Game-level session: one save for the whole game, kept across scene loads. It owns the progress
    // record, its store and scene-load restoration; CheckpointService owns rest/travel/exits/respawn,
    // RewardService owns shards, hearts and upgrades, SceneTransitions owns fades and loading, and scene
    // participants own how their state is represented. Every playable scene holds the GameSession prefab
    // so it can be played directly; the first instance persists and later copies remove themselves, so
    // the save key and upgrade tiers always come from the prefab.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        // Stable storage key across title changes; do not rename with display branding.
        [SerializeField] private string saveKey = "RoadOfTheOldKing.Save";
        [Tooltip("Older keys migrated once while saveKey is empty. New Game clears them too.")]
        [SerializeField] private string[] legacySaveKeys = Array.Empty<string>();
        [Tooltip("Scene path a new game starts in. Empty restarts the active scene.")]
        [SerializeField] private string newGameScene = "";
        [Tooltip("Title scene path, for the pause menu's Main Menu.")]
        [SerializeField] private string titleScene = "";
        [SerializeField] private AxeUpgradeTier[] upgradeTiers = Array.Empty<AxeUpgradeTier>();
        [Tooltip("Seconds between checks for the position autosave. It writes only when the player is alive, out " +
                 "of combat and has moved (or their health or flasks changed) since the last write.")]
        [SerializeField, Min(1f)] private float resumeInterval = 5f;
        [Tooltip("Minimum distance moved before the autosave writes a new resume point.")]
        [SerializeField, Min(0f)] private float resumeMinDistance = 1f;
        private IProgressStore store;
        private ResumePoint resume, pendingResume;
        private float nextResumeCheck;
        private SceneTransitions transitions;
        private bool loading;
        private bool loadFailed;
        private string pendingScene;

        public static GameSession Instance { get; private set; }
        public ProgressState Progress { get; private set; } = new ProgressState();
        public CheckpointService Checkpoints { get; private set; }
        public RewardService Rewards { get; private set; }
        // Includes a transition's fade-in: input is locked then and a new load would be refused.
        public bool IsLoading => loading || (transitions != null && transitions.IsBusy);
        // Anything worth continuing: a rest point, the axe or any milestone.
        public bool HasSave => !loadFailed && (resume != null || Checkpoints.HasCheckpoint || Progress.hasAxe || Progress.completedIds.Count > 0);
        // Continue resumes at this exact spot instead of the last fire.
        public bool HasResumePoint => !loadFailed && resume != null;
        public bool CanReturnToTitle => SceneLoader.CanLoad(titleScene) && SceneLoader.ActivePath != titleScene;
        public string Status { get; private set; } = "";
        internal PlayerHealth Player { get; private set; }
        internal PlayerCombatController Combat { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (!TryGetComponent(out transitions)) transitions = gameObject.AddComponent<SceneTransitions>();
            Checkpoints = new CheckpointService(this);
            Rewards = new RewardService(this, upgradeTiers);
            store = new PlayerPrefsProgressStore(saveKey, legacySaveKeys);
            try { Progress = store.Load(); }
            catch (Exception)
            {
                loadFailed = true;
                Status = "Save could not be read. Start a new game to replace it.";
            }
            try { resume = store.LoadResume(); }
            catch (Exception) { resume = null; }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only the scene this session asked for counts once a load is under way.
            if (mode == LoadSceneMode.Additive || (pendingScene != null && scene.path != pendingScene))
                return;
            pendingScene = null;
            loading = false;
            Player = FindFirstObjectByType<PlayerHealth>();
            Combat = Player != null ? Player.GetComponent<PlayerCombatController>() : null;
            Checkpoints.FindFires();
            if (Player == null)
            {
                Checkpoints.Forget();
                return;
            }

            Checkpoints.PlaceArrivingPlayer(scene, transitions.ArrivalId);
            // Continue from a resume point: the exact spot, with the health and flasks the player left with.
            if (pendingResume != null && pendingResume.scenePath == scene.path)
            {
                Checkpoints.MoveTo(new Vector2(pendingResume.x, pendingResume.y));
                Checkpoints.Carry(pendingResume.health, pendingResume.flasks);
            }
            pendingResume = null;
            // Dying returns to the fire, so a death clears the resume point at once (even if the game is closed
            // on the defeat screen).
            Player.Health.Defeated += ClearResume;
            // The axe belongs to the player once taken: each scene's player spawns its own copy.
            if (Combat != null && Progress.hasAxe)
                Combat.EquipOwnedAxe();
            if (Combat != null && Progress.recallUnlocked)
                Combat.UnlockRecall();
            foreach (var participant in Participants<IProgressParticipant>())
                participant.RestoreProgress(Progress);
            Rewards.ApplyUpgrades();
            Checkpoints.ApplyCarriedVitals();
            transitions.HoldArrivingPlayer(Player.GetComponent<PlayerControlLocks>());
        }

        internal static T[] Participants<T>() => FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == SceneManager.GetActiveScene()).OfType<T>().ToArray();

        internal void Capture()
        {
            // Permanent unlocks only ever turn on: a scene without the player's axe must not revoke ownership.
            if (Combat != null)
            {
                Progress.hasAxe |= Combat.Weapon != null;
                Progress.recallUnlocked |= Combat.CanRecall;
            }
            foreach (var participant in Participants<IProgressParticipant>())
                participant.CaptureProgress(Progress);
        }

        internal void Save(string successMessage)
        {
            if (loadFailed)
            {
                Status = "Progress is session-only: unreadable save. Use New Game to replace it.";
                return;
            }
            try { store.Save(Progress); Status = successMessage; }
            catch (Exception) { Status = "Progress is session-only: saving failed."; }
        }

        internal void CaptureAndSave(string successMessage)
        {
            Capture();
            Save(successMessage);
        }

        // Completed puzzles are permanent without moving the last rest/respawn point.
        public void SaveProgress() => CaptureAndSave("Progress saved.");

        public void StartNewRun()
        {
            if (loading)
                return;
            try { store.Clear(); }
            catch (Exception) { Status = "Could not clear the save."; return; }
            loadFailed = false;
            Progress = new ProgressState();
            resume = pendingResume = null;
            Rewards.OnProgressReplaced();
            Checkpoints.Forget();
            Status = "";
            Load(SceneLoader.CanLoad(newGameScene) ? newGameScene : SceneLoader.ActivePath, null);
        }

        // Title: resume at the exact spot the player left; otherwise at the checkpoint's fire, or at the start of
        // the new-game scene without one.
        public bool Continue()
        {
            if (loading) return false;
            if (HasResumePoint && SceneLoader.CanLoad(resume.scenePath))
            {
                pendingResume = resume;
                if (Load(resume.scenePath, null)) return true;
                pendingResume = null;
            }
            return Checkpoints.HasCheckpoint && SceneLoader.CanLoad(Progress.scenePath)
                ? Load(Progress.scenePath, null)
                : Load(SceneLoader.CanLoad(newGameScene) ? newGameScene : SceneLoader.ActivePath, null);
        }

        // Pause menu: save permanent progress and the exact spot, then go to the title. Continue resumes there.
        public bool ReturnToTitle()
        {
            if (loading || !CanReturnToTitle) return false;
            if (Player != null && Player.IsAlive) { CaptureAndSave("Progress saved."); RecordResume(); }
            return Load(titleScene, null);
        }

        // Position autosave. Cheap by design: no scene capture and no save rewrite, only the small resume record,
        // checked every few seconds and written only when something changed (see the fields' tooltips).
        private void Update()
        {
            if (Player == null || loading || Time.unscaledTime < nextResumeCheck) return;
            nextResumeCheck = Time.unscaledTime + resumeInterval;
            if (!EncounterState.InCombat) RecordResume(force: false);
        }

        // Moments a player may be leaving: the window or browser tab loses focus, or the application quits.
        private void OnApplicationFocus(bool focused) { if (!focused) RecordResume(); }
        private void OnApplicationQuit() => RecordResume();

        // Records where Continue resumes. Called directly by the pause menu, Main Menu and Quit.
        public void RecordResume() => RecordResume(force: true);

        private void RecordResume(bool force)
        {
            if (Player == null || !Player.IsAlive || loading || loadFailed) return;
            Vector2 position = Player.transform.position;
            var flask = Player.GetComponent<PlayerFlask>();
            var next = new ResumePoint
            {
                scenePath = SceneLoader.ActivePath, x = position.x, y = position.y,
                health = Player.Health.Health, flasks = flask != null ? flask.Charges : -1
            };
            if (!force && resume != null && resume.scenePath == next.scenePath && resume.health == next.health &&
                resume.flasks == next.flasks &&
                (new Vector2(resume.x, resume.y) - position).sqrMagnitude < resumeMinDistance * resumeMinDistance)
                return;
            resume = next;
            try { store.SaveResume(resume); }
            catch (Exception) { }
        }

        internal void ClearResume()
        {
            if (resume == null) return;
            resume = null;
            try { store.ClearResume(); }
            catch (Exception) { }
        }

        internal bool Load(string scenePath, string arrival)
        {
            var locks = Player != null ? Player.GetComponent<PlayerControlLocks>() : null;
            if (!transitions.Begin(scenePath, arrival, locks))
                return false;
            loading = true;
            pendingScene = scenePath;
            return true;
        }
    }
}
