using System;
using System.Linq;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.Progression
{
    // Where the player is and returns to: resting, travel between fires (in any scene), scene exits,
    // respawn/restart, and placing the player when a scene loads.
    public sealed class CheckpointService
    {
        private readonly GameSession session;
        // The named arrival used in the current scene, so a restart without a checkpoint returns there.
        private string arrivalScene, arrivalId;
        // Health and flasks carried through a scene exit (session-only, never saved).
        private int carriedHealth = -1, carriedFlasks = -1;

        internal CheckpointService(GameSession session) => this.session = session;

        public bool HasCheckpoint => !string.IsNullOrEmpty(session.Progress.checkpointId);
        // Fires in the active scene, in menu order. Discovered fires in any scene are in Progress.fires.
        public Bonfire[] Fires { get; private set; } = Array.Empty<Bonfire>();

        public bool Rest(Bonfire fire)
        {
            var player = session.Player;
            if (session.IsLoading || player == null || !player.IsAlive || fire == null || !fire.CanUse(player.transform))
                return false;
            var progress = session.Progress;
            progress.DiscoverFire(fire.Id, SceneLoader.ActivePath, fire.DisplayName, fire.DisplayOrder);
            progress.checkpointId = fire.Id;
            progress.scenePath = SceneLoader.ActivePath;
            session.Capture();
            RestoreCombatArea();
            session.Save("Rested. Saved.");
            session.ClearResume();
            return true;
        }

        // Travel between discovered fires. A fire in another scene loads that scene and respawns there.
        public bool Travel(FireRecord destination, Bonfire departure)
        {
            var player = session.Player;
            var progress = session.Progress;
            if (session.IsLoading || player == null || !player.IsAlive || destination == null || departure == null ||
                destination.id == departure.Id || !departure.CanUse(player.transform) ||
                !progress.HasFire(departure.Id) || !progress.HasFire(destination.id))
                return false;
            var local = Fires.FirstOrDefault(f => f.Id == destination.id);
            if (local == null && !SceneLoader.CanLoad(destination.scenePath))
                return false;
            session.Capture();
            progress.checkpointId = destination.id;
            progress.scenePath = local != null ? SceneLoader.ActivePath : destination.scenePath;
            session.Save("Travelled. Saved.");
            session.ClearResume();
            if (local == null)
                return session.Load(destination.scenePath, null);
            RestoreCombatArea();
            MoveTo(local.SpawnPosition);
            return true;
        }

        // Scene exits: save, carry current health and flasks, and arrive at the named spawn point.
        public bool LeaveScene(string scenePath, string spawnId)
        {
            var player = session.Player;
            if (session.IsLoading || player == null || !player.IsAlive || !SceneLoader.CanLoad(scenePath))
                return false;
            session.CaptureAndSave("Progress saved.");
            // The old scene's spot is stale; the first autosave in the new scene writes a fresh one.
            session.ClearResume();
            carriedHealth = player.Health.Health;
            var flask = player.GetComponent<PlayerFlask>();
            carriedFlasks = flask != null ? flask.Charges : -1;
            if (session.Load(scenePath, spawnId)) return true;
            ClearCarried();
            return false;
        }

        public void Respawn()
        {
            if (session.Player == null || session.Player.IsAlive)
                return;
            RestartFromCheckpoint();
        }

        // Restart preserves permanent progress, unlike the explicitly destructive New Game.
        // It loads the checkpoint's scene; without a checkpoint it restarts this scene at its arrival point.
        public void RestartFromCheckpoint()
        {
            if (session.IsLoading || session.Player == null) return;
            // Permanent rewards also survive a death before the first rest.
            session.CaptureAndSave(HasCheckpoint ? "Returned to the last bonfire." : "Returned to the start.");
            session.ClearResume();
            var progress = session.Progress;
            if (HasCheckpoint && SceneLoader.CanLoad(progress.scenePath))
                session.Load(progress.scenePath, null);
            else
                session.Load(SceneLoader.ActivePath, arrivalScene == SceneLoader.ActivePath ? arrivalId : null);
        }

        internal void Forget()
        {
            arrivalScene = arrivalId = null;
            ClearCarried();
        }

        internal void FindFires() => Fires = UnityEngine.Object.FindObjectsByType<Bonfire>(FindObjectsSortMode.None)
            .OrderBy(f => f.DisplayOrder).ToArray();

        // A named arrival (spawn point, or a fire for travel) wins; otherwise the checkpoint fire if it is
        // in this scene; otherwise the player stays where the scene placed them.
        internal void PlaceArrivingPlayer(Scene scene, string arrival)
        {
            var spawn = SceneSpawnPoint.Find(arrival);
            var arrivalFire = spawn == null && !string.IsNullOrEmpty(arrival) ? Fires.FirstOrDefault(f => f.Id == arrival) : null;
            if (spawn != null || arrivalFire != null)
            {
                arrivalScene = scene.path;
                arrivalId = arrival;
                MoveTo(spawn != null ? spawn.Position : arrivalFire.SpawnPosition);
                return;
            }
            var checkpoint = Fires.FirstOrDefault(f => f.Id == session.Progress.checkpointId);
            if (checkpoint != null && session.Progress.scenePath == scene.path)
                MoveTo(checkpoint.SpawnPosition);
        }

        // Vitals to apply on the next arrival (a scene exit or a resume point); negative values are ignored.
        internal void Carry(int health, int flasks)
        {
            carriedHealth = health;
            carriedFlasks = flasks;
        }

        // After progress is restored, so heart fragments have already raised maximum health.
        internal void ApplyCarriedVitals()
        {
            var player = session.Player;
            if (player != null && carriedHealth > 0) player.Health.SetHealth(carriedHealth);
            if (player != null && carriedFlasks >= 0) player.GetComponent<PlayerFlask>()?.SetCharges(carriedFlasks);
            ClearCarried();
        }

        private void ClearCarried() => carriedHealth = carriedFlasks = -1;

        private void RestoreCombatArea()
        {
            var combat = session.Combat;
            if (combat != null && combat.Weapon != null)
                combat.Weapon.CancelAction();
            session.Player.HealAtRest();
            foreach (var resettable in GameSession.Participants<IResetOnRest>())
                resettable.ResetOnRest();
        }

        internal void MoveTo(Vector2 position)
        {
            var player = session.Player;
            var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = position;
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }
            var combat = session.Combat;
            if (combat != null && combat.Weapon != null)
                combat.Weapon.CancelAction();
            Physics2D.SyncTransforms();
            if (Camera.main != null)
                Camera.main.GetComponent<CameraFollow2D>()?.SnapToTarget();
        }

        // Authored terrain can change between development builds. Keep the already placed
        // arrival/checkpoint when an exact resume would put the body inside a new solid.
        internal bool TryResumeAt(Vector2 position)
        {
            var player = session.Player;
            if (player == null || float.IsNaN(position.x) || float.IsNaN(position.y) ||
                float.IsInfinity(position.x) || float.IsInfinity(position.y)) return false;
            var shape = player.GetComponent<BoxCollider2D>();
            if (shape == null) return false;
            Physics2D.SyncTransforms();
            var overlaps = new System.Collections.Generic.List<Collider2D>(8);
            Vector2 offset = player.transform.TransformVector(shape.offset);
            Vector2 size = Vector2.Scale(shape.size, new Vector2(
                Mathf.Abs(player.transform.lossyScale.x), Mathf.Abs(player.transform.lossyScale.y)));
            Physics2D.OverlapBox(position + offset, size, player.transform.eulerAngles.z,
                new ContactFilter2D { useTriggers = false }, overlaps);
            foreach (var hit in overlaps)
                if (hit != null && !hit.transform.IsChildOf(player.transform)) return false;
            MoveTo(position);
            return true;
        }
    }
}
