#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Linq;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.UI
{
    // Shared by the F4 panel and editor automation. Never edits prefabs/settings assets.
    // Commands use real gameplay events; enemy defeats can therefore award/save milestones.
    public sealed class DevRuntimeCommands : MonoBehaviour
    {
        public static DevRuntimeCommands Instance { get; private set; }
        public PlayerHealth Player { get; private set; }
        public PlayerMovement Movement { get; private set; }
        public PlayerStamina Stamina { get; private set; }
        public PlayerFlask Flask { get; private set; }
        public PlayerCombatController Combat { get; private set; }
        public bool HasPlayer => Player != null;
        public bool Alive => HasPlayer && Player.IsAlive;
        public bool GodMode => HasPlayer && Player.DebugGodMode;
        public bool InfiniteStamina => Stamina != null && Stamina.DebugInfinite;
        public bool NoCollision { get; private set; }
        public float SpeedMultiplier => Movement != null ? Movement.DebugSpeedMultiplier : 1f;
        public string LastResult { get; private set; } = "Ready.";
        public bool LastSucceeded { get; private set; } = true;
        public Vector2? Marker { get; private set; }
        public event Action SceneChanged;
        private Rigidbody2D body;
        private readonly List<Collider2D> ghostColliders = new List<Collider2D>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private Vector2 collisionEntry;
        private float originalTimeScale;
        private bool timeChanged;
        private float searchAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;

        private void Awake() { Instance = this; Bind(); }
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreOverrides();
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Player == null && Time.unscaledTime >= searchAt)
            {
                searchAt = Time.unscaledTime + .5f;
                Bind();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive) return;
            RestoreOverrides();
            Player = null; Marker = null;
            Bind();
            SceneChanged?.Invoke();
            Report(true, "Scene loaded. Runtime overrides reset.");
        }

        private void Bind()
        {
            Player = FindFirstObjectByType<PlayerHealth>();
            Movement = Player != null ? Player.GetComponent<PlayerMovement>() : null;
            Stamina = Player != null ? Player.GetComponent<PlayerStamina>() : null;
            Flask = Player != null ? Player.GetComponent<PlayerFlask>() : null;
            Combat = Player != null ? Player.GetComponent<PlayerCombatController>() : null;
            body = Player != null ? Player.GetComponent<Rigidbody2D>() : null;
        }

        private bool Report(bool success, string message)
        {
            LastSucceeded = success; LastResult = message;
            return success;
        }
        private bool RequireAlive() => GameSession.Instance != null && GameSession.Instance.IsLoading
            ? Report(false, "Wait for the scene to finish loading.")
            : Alive || Report(false, "No living player. Use Respawn.");
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private bool RequireSession() => RequireAlive() && (GameSession.Instance != null &&
            GameSession.Instance.Player == Player || Report(false, "No active player session."));

        public bool GiveAxe()
        {
            if (!RequireSession()) return false;
            if (Combat == null || !Combat.EquipOwnedAxe()) return Report(false, "Player has no configured axe prefab.");
            var session = GameSession.Instance;
            session.Progress.hasAxe = true;
            session.Rewards.ApplyUpgrades();
            foreach (var pickup in FindObjectsByType<AxePickup>(FindObjectsSortMode.None))
                pickup.RestoreProgress(session.Progress);
            session.SaveProgress();
            return Report(true, "Axe equipped. " + session.Status);
        }

        public bool UnlockRecall()
        {
            if (!RequireSession() || Combat == null) return false;
            if (Combat.Weapon == null && !GiveAxe()) return false;
            Combat.UnlockRecall();
            var session = GameSession.Instance;
            session.Progress.recallUnlocked = true;
            foreach (var pickup in FindObjectsByType<RecallUnlockPickup>(FindObjectsSortMode.None))
                pickup.RestoreProgress(session.Progress);
            session.SaveProgress();
            return Report(true, "Recall unlocked. " + session.Status);
        }

        public bool GiveCoins(int amount) => GiveResource(amount, "Bronze Coins", r => r.DebugGiveCoins(amount));
        public bool GiveShards(int amount) => GiveResource(amount, "Sun Shards", r => r.DebugGiveShards(amount));
        public bool GiveHeartFragments(int amount) => GiveResource(amount, "Heart fragments", r => r.DebugGiveHeartFragments(amount));
        private bool GiveResource(int amount, string name, Func<RewardService, bool> grant)
        {
            if (!RequireSession()) return false;
            var session = GameSession.Instance;
            bool success = grant(session.Rewards);
            return Report(success, success ? $"{name} +{amount}. {session.Status}" :
                "Enter a positive quantity within the balance limit (heart fragments: 1–99).");
        }

        public bool GiveTool(WorldTool tool)
        {
            if (!RequireSession()) return false;
            var session = GameSession.Instance;
            if (tool == WorldTool.None || !Enum.IsDefined(typeof(WorldTool), tool)) return Report(false, "Unknown tool.");
            if (session.Progress.OwnsTool(tool)) return Report(true, WorldToolNames.Display(tool) + " already owned.");
            if (!session.Rewards.TryCollectTool("dev/" + tool, tool)) return Report(false, "Could not grant tool.");
            foreach (var pickup in FindObjectsByType<ToolPickup>(FindObjectsSortMode.None))
                pickup.RestoreProgress(session.Progress);
            return Report(true, WorldToolNames.Display(tool) + " granted. " + session.Status);
        }

        public DevTravelDestination[] GetDestinations()
        {
            string current = SceneManager.GetActiveScene().path;
            var source = Resources.Load<TextAsset>("DevTools/TravelDestinations");
            var catalog = source != null ? JsonUtility.FromJson<DevTravelCatalog>(source.text) : new DevTravelCatalog();
            return catalog.destinations.Where(d => d.scenePath != current && SceneLoader.CanLoad(d.scenePath))
                .Concat(DevTravelCatalog.FromScene(SceneManager.GetActiveScene()))
                .OrderBy(d => d.scenePath == current ? 0 : 1).ThenBy(d => d.scenePath).ThenBy(d => d.label).ToArray();
        }

        public bool TravelTo(DevTravelDestination destination)
        {
            if (!RequireSession()) return false;
            if (destination == null || string.IsNullOrEmpty(destination.id) || !SceneLoader.CanLoad(destination.scenePath))
                return Report(false, "Destination is unavailable.");
            if (destination.scenePath == SceneManager.GetActiveScene().path)
            {
                if (destination.bonfire)
                    return TeleportToBonfire(GetBonfires().FirstOrDefault(f => f.Id == destination.id));
                var point = SceneSpawnPoint.Find(destination.id);
                if (point == null) return Report(false, "Spawn point no longer exists. Reopen Travel.");
                if (!Teleport(point.Position)) return false;
                Movement?.SetFacingDirection(point.ArrivalFacing);
                return Report(true, "Teleported to " + destination.label + ".");
            }
            DevToolsPanel.Instance?.Close();
            bool loaded = GameSession.Instance.Checkpoints.LeaveScene(destination.scenePath, destination.id);
            return Report(loaded, loaded ? "Travelling to " + destination.label + "." : "Could not load destination.");
        }

        public bool DamagePlayer(int amount, bool bypassProtection = true)
        {
            if (!RequireAlive()) return false;
            if (amount <= 0) return Report(false, "Damage must be greater than zero.");
            var hit = new CombatHit(gameObject, AttackKind.EnemyMelee, amount, Vector2.down, 3f, .2f);
            bool accepted = bypassProtection ? Player.Health.DebugReceiveHit(hit) : Player.Health.ReceiveHit(hit);
            return Report(accepted, accepted ? $"Dealt {amount} damage to player." : "Hit blocked by god mode or invulnerability.");
        }
        public bool KillPlayer() => RequireAlive() && DamagePlayer(Player.Health.Health);
        public bool HealPlayer()
        {
            if (!RequireAlive()) return false;
            Player.Health.Heal(Player.Health.MaxHealth);
            return Report(true, "Health restored.");
        }
        public bool RefillFlasks()
        {
            if (!RequireAlive() || Flask == null) return false;
            Flask.Refill(); return Report(true, "Flasks refilled; any drink cancelled.");
        }
        public bool RestorePlayer()
        {
            if (!RequireAlive()) return false;
            Combat?.Weapon?.CancelAction();
            Player.HealAtRest();
            return Report(true, "Health, stamina, flasks and dodge restored. Enemies unchanged.");
        }
        public bool SetStamina(float value)
        {
            if (!RequireAlive() || Stamina == null) return false;
            if (!Finite(value)) return Report(false, "Enter a finite stamina value.");
            Stamina.DebugSetCurrent(value);
            return Report(true, $"Stamina set to {Stamina.Current:0.#}.");
        }
        public bool SetGodMode(bool value)
        {
            if (!HasPlayer) return Report(false, "No player in this scene.");
            Player.DebugGodMode = value;
            return Report(true, value ? "God mode on. Explicit debug damage still works." : "God mode off.");
        }
        public bool SetInfiniteStamina(bool value)
        {
            if (Stamina == null) return Report(false, "No stamina component.");
            Stamina.DebugInfinite = value;
            if (value) Stamina.Restore();
            return Report(true, value ? "Infinite stamina on." : "Infinite stamina off.");
        }
        public bool SetSpeed(float multiplier)
        {
            if (Movement == null) return Report(false, "No movement component.");
            if (!Finite(multiplier)) return Report(false, "Enter a finite speed.");
            Movement.DebugSpeedMultiplier = Mathf.Clamp(multiplier, .25f, 4f);
            return Report(true, $"Movement {SpeedMultiplier:0.##}x. Dodge distance unchanged.");
        }
        public bool SetGameSpeed(float value)
        {
            if (!Finite(value)) return Report(false, "Enter a finite game speed.");
            if (!timeChanged) { originalTimeScale = SimulationPause.UnpausedTimeScale; timeChanged = true; }
            SimulationPause.SetTimeScale(Mathf.Clamp(value, .1f, 3f));
            return Report(true, $"Game speed {SimulationPause.UnpausedTimeScale:0.##}x" + (SimulationPause.IsPaused ? " when resumed." : "."));
        }

        public bool SetCollision(bool enabled)
        {
            if (!RequireAlive()) return false;
            if (enabled == !NoCollision) return Report(true, enabled ? "Collision already on." : "Collision already off.");
            if (!enabled)
            {
                collisionEntry = Player.transform.position;
                ghostColliders.Clear();
                foreach (var collider in Player.GetComponentsInChildren<Collider2D>())
                    if (collider.enabled && !collider.isTrigger && collider.attachedRigidbody == body)
                    { ghostColliders.Add(collider); collider.isTrigger = true; }
                NoCollision = true;
                return Report(true, "Collision off. God mode is separate.");
            }
            Physics2D.SyncTransforms();
            bool obstructed = ghostColliders.Any(c => c != null && OverlapsSolid(c));
            if (obstructed) MovePlayer(collisionEntry);
            RestoreCollision();
            return Report(true, obstructed ? "Collision restored at its starting position (current position was obstructed)." : "Collision on.");
        }
        private bool OverlapsSolid(Collider2D collider)
        {
            overlaps.Clear();
            collider.Overlap(new ContactFilter2D { useTriggers = false }, overlaps);
            return overlaps.Any(c => c != null && c.attachedRigidbody != body && !c.isTrigger &&
                !Physics2D.GetIgnoreLayerCollision(collider.gameObject.layer, c.gameObject.layer) && !Physics2D.GetIgnoreCollision(collider, c));
        }
        private void RestoreCollision()
        {
            foreach (var collider in ghostColliders) if (collider != null) collider.isTrigger = false;
            ghostColliders.Clear(); NoCollision = false;
        }

        public Bonfire[] GetBonfires() => FindObjectsByType<Bonfire>(FindObjectsSortMode.None)
            .OrderBy(f => f.DisplayOrder).ThenBy(f => f.DisplayName).ToArray();
        public bool TeleportToBonfire(Bonfire fire)
        {
            if (fire == null) return Report(false, "Bonfire no longer exists.");
            if (!Teleport(fire.SpawnPosition)) return false;
            return Report(true, $"Teleported to {fire.DisplayName}. Checkpoint unchanged.");
        }
        public bool SetMarker()
        {
            if (!RequireAlive()) return false;
            Marker = Player.transform.position;
            return Report(true, $"Marker set at {Marker.Value.x:0.0}, {Marker.Value.y:0.0}.");
        }
        public bool TeleportToMarker() => Marker.HasValue ? Teleport(Marker.Value) : Report(false, "Set a return marker first.");
        public bool Teleport(Vector2 position)
        {
            if (!RequireAlive()) return false;
            if (!Finite(position.x) || !Finite(position.y)) return Report(false, "Enter finite coordinates.");
            MovePlayer(position);
            return Report(true, $"Teleported to {position.x:0.0}, {position.y:0.0}. No rest or save.");
        }
        private void MovePlayer(Vector2 position)
        {
            Player.GetComponent<PlayerBonfireInteraction>()?.Close();
            Combat?.Weapon?.CancelAction();
            Player.GetComponent<PlayerDash>()?.Cancel();
            Player.GetComponent<HitReaction>()?.Clear();
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = position; }
            Player.transform.position = position;
            Physics2D.SyncTransforms();
            Camera.main?.GetComponent<CameraFollow2D>()?.SnapToTarget();
        }
        public int NearbyCount(float radius) => NearbyEnemies(radius).Count(e => !e.IsDefeated);
        private IEnemy[] NearbyEnemies(float radius)
        {
            if (!HasPlayer || !Finite(radius) || radius <= 0f) return Array.Empty<IEnemy>();
            float squared = Mathf.Clamp(radius, 1f, 100f); squared *= squared;
            return EnemyRegistry.Active.Where(e => e is Component c && c != null &&
                ((Vector2)(c.transform.position - Player.transform.position)).sqrMagnitude <= squared).ToArray();
        }
        public bool KillNearby(float radius)
        {
            if (!RequireAlive()) return false;
            int count = 0;
            foreach (var enemy in NearbyEnemies(radius))
            {
                var target = ((Component)enemy).GetComponent<Damageable>();
                if (target != null && target.IsAlive && target.DebugReceiveHit(new CombatHit(Player.gameObject,
                    AttackKind.LightChop, target.Health, Vector2.zero, 0f, 0f))) count++;
            }
            return Report(true, $"Defeated {count} nearby enemies. Normal rewards/milestones apply.");
        }
        public bool ResetEnemies()
        {
            int count = 0;
            foreach (var enemy in EnemyRegistry.Active.ToArray())
                if (enemy is IResetOnRest reset) { reset.ResetOnRest(); count++; }
            return Report(true, $"Reset {count} enemies. Saved rewards and milestones unchanged.");
        }
        public bool RetrieveAxe()
        {
            if (!RequireAlive() || Combat == null || Combat.Weapon == null) return Report(false, "No owned axe.");
            bool returning = Combat.Weapon.TryRecall();
            return Report(returning, returning ? "Returning axe (free scripted Recall)." : "Axe is already held or returning.");
        }
        public bool Respawn()
        {
            if (GameSession.Instance == null || !HasPlayer) return Report(false, "No player session in this scene.");
            if (GameSession.Instance.IsLoading) return Report(false, "Wait for the scene to finish loading.");
            DevToolsPanel.Instance?.Close();
            RestoreOverrides();
            GameSession.Instance.Checkpoints.RestartFromCheckpoint();
            return Report(true, "Reloading at checkpoint; permanent progress kept.");
        }
        public bool ResetOverrides()
        {
            if (NoCollision && Alive) SetCollision(true);
            RestoreOverrides();
            return Report(true, "God mode, infinite stamina, collision, movement and time overrides reset.");
        }
        private void RestoreOverrides()
        {
            if (Player != null) Player.DebugGodMode = false;
            if (Stamina != null) Stamina.DebugInfinite = false;
            if (Movement != null) Movement.DebugSpeedMultiplier = 1f;
            RestoreCollision();
            if (timeChanged) SimulationPause.SetTimeScale(originalTimeScale);
            timeChanged = false;
        }
    }
}
#endif
