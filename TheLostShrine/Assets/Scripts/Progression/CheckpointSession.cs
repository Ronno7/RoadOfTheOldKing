using System;
using System.Linq;
using TheLostShrine.Cameras;
using TheLostShrine.Player;
using TheLostShrine.Weapons;
using TheLostShrine.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLostShrine.Progression
{
    // Owns session progress; scene participants own how their state is represented.
    [DefaultExecutionOrder(-100), DisallowMultipleComponent]
    public sealed class CheckpointSession : MonoBehaviour
    {
        [SerializeField] private string saveKey = "TheLostShrine.PrototypeLoop.Save.v1";
        [SerializeField] private HatchetUpgradeTier[] upgradeTiers = Array.Empty<HatchetUpgradeTier>();
        private IProgressStore store;
        private PlayerHealth player;
        private PlayerCombatController combat;
        private bool loading;
        private bool loadFailed;
        private WeaponUpgradeProgression upgrades;

        public static CheckpointSession Instance { get; private set; }
        public ProgressState Progress { get; private set; } = new ProgressState();
        public bool HasCheckpoint => !string.IsNullOrEmpty(Progress.checkpointId);
        public string Status { get; private set; } = "";
        public Bonfire[] Fires { get; private set; } = Array.Empty<Bonfire>();
        public WeaponUpgradeProgression Upgrades => upgrades ?? (upgrades = new WeaponUpgradeProgression(Progress, upgradeTiers));

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
            store = new PlayerPrefsProgressStore(saveKey);
            try { Progress = store.Load(); }
            catch (Exception)
            {
                loadFailed = true;
                Status = "Save could not be read. Start a new run to replace it.";
            }
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
            loading = false;
            player = FindFirstObjectByType<PlayerHealth>();
            combat = player != null ? player.GetComponent<PlayerCombatController>() : null;
            Fires = FindObjectsByType<Bonfire>(FindObjectsSortMode.None)
                .OrderBy(f => f.DisplayOrder).ToArray();
            if (player == null || combat == null)
                return;

            var checkpoint = Fires.FirstOrDefault(f => f.Id == Progress.checkpointId);
            if (checkpoint != null && Progress.scenePath == scene.path)
                MoveTo(checkpoint);
            if (Progress.hasHatchet && combat.Weapon == null)
            {
                var weapon = FindFirstObjectByType<HatchetWeapon>();
                if (weapon != null)
                    combat.TryEquip(weapon);
            }
            if (Progress.recallUnlocked)
                combat.UnlockRecall();
            foreach (var participant in Participants<IProgressParticipant>())
                participant.RestoreProgress(Progress);
            ApplyUpgrades();
        }

        private static T[] Participants<T>() => FindObjectsByType<MonoBehaviour>(
            FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(c => c.gameObject.scene == SceneManager.GetActiveScene()).OfType<T>().ToArray();

        private void Capture()
        {
            if (combat != null)
            {
                Progress.hasHatchet = combat.Weapon != null;
                Progress.recallUnlocked = combat.CanRecall;
            }
            foreach (var participant in Participants<IProgressParticipant>())
                participant.CaptureProgress(Progress);
        }

        public bool Rest(Bonfire fire)
        {
            if (loading || player == null || !player.IsAlive || fire == null || !fire.CanUse(player.transform))
                return false;
            if (!Progress.discoveredFires.Contains(fire.Id))
                Progress.discoveredFires.Add(fire.Id);
            Progress.checkpointId = fire.Id;
            Progress.scenePath = SceneManager.GetActiveScene().path;
            Capture();
            RestoreCombatArea();
            Save("Rested at " + fire.DisplayName + ". HP and stamina restored; enemies reset; progress saved.");
            return true;
        }

        public bool Travel(Bonfire destination, Bonfire departure)
        {
            if (loading || player == null || !player.IsAlive || destination == null || departure == null ||
                !departure.CanUse(player.transform) || !Progress.discoveredFires.Contains(departure.Id) ||
                !Progress.discoveredFires.Contains(destination.Id) || !Fires.Contains(destination))
                return false;
            Capture();
            Progress.checkpointId = destination.Id;
            Progress.scenePath = SceneManager.GetActiveScene().path;
            RestoreCombatArea();
            MoveTo(destination);
            Save("Travelled to " + destination.DisplayName + ". Progress saved.");
            return true;
        }

        private void RestoreCombatArea()
        {
            if (combat.Weapon != null)
                combat.Weapon.CancelAction();
            player.HealAtRest();
            foreach (var resettable in Participants<IResetOnRest>())
                resettable.ResetOnRest();
        }

        private void MoveTo(Bonfire fire)
        {
            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = fire.SpawnPosition;
            if (body != null)
            {
                body.position = fire.SpawnPosition;
                body.linearVelocity = Vector2.zero;
            }
            if (combat != null && combat.Weapon != null)
                combat.Weapon.CancelAction();
            Physics2D.SyncTransforms();
            if (Camera.main != null)
                Camera.main.GetComponent<CameraFollow2D>()?.SnapToTarget();
        }

        // Completed puzzles are permanent without moving the last rest/respawn point.
        public void SaveProgress()
        {
            Capture();
            Save("Progress saved.");
        }

        public bool TryCollectShard(string rewardId)
        {
            if (loading || string.IsNullOrEmpty(rewardId) || Progress.Has("shard/collected/" + rewardId))
                return false;
            Progress.Complete("shard/collected/" + rewardId);
            Progress.sunShards++;
            Capture();
            Save("Sun Shard collected. " + Progress.sunShards + " available.");
            return true;
        }

        public bool TryPurchaseUpgrade(Bonfire fire, HatchetUpgrade choice)
        {
            if (loading || player == null || !player.IsAlive || combat.Weapon == null || fire == null ||
                !fire.AllowsUpgrades || !Fires.Contains(fire) || !fire.CanUse(player.transform) ||
                player.GetComponent<PlayerBonfireInteraction>().ActiveFire != fire || !Upgrades.TryPurchase(choice))
                return false;
            ApplyUpgrades();
            Capture();
            Save(choice.displayName + " chosen. The other choices in this tier are gone.");
            return true;
        }

        private void ApplyUpgrades()
        {
            if (combat != null && combat.Weapon != null) combat.Weapon.ApplyUpgrades(Upgrades.Selected);
        }

        private void Save(string successMessage)
        {
            if (loadFailed)
            {
                Status = "Progress is session-only: unreadable save. Use New run to replace it.";
                return;
            }
            try { store.Save(Progress); Status = successMessage; }
            catch (Exception) { Status = "Progress is session-only: saving failed."; }
        }

        public void Respawn()
        {
            if (loading || player == null || player.IsAlive)
                return;
            // Permanent rewards also survive a death before the first rest.
            Capture();
            Save(HasCheckpoint ? "Returned to the last bonfire." : "Returned to the start.");
            loading = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void StartNewRun()
        {
            if (loading)
                return;
            try { store.Clear(); }
            catch (Exception) { Status = "Could not clear the save."; return; }
            loadFailed = false;
            Progress = new ProgressState();
            upgrades = null;
            Status = "";
            loading = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
