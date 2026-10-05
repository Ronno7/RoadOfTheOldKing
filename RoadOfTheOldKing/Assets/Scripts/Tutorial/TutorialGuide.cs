using System;
using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    public enum TutorialCondition
    {
        Moved,          // walked `amount` units
        Sprinted,       // sprinted `amount` units
        Dodged,         // dashed `amount` times (at least once)
        HasAxe,         // owns the axe
        RecallUnlocked, // the stone awakened Recall
        Rested,         // has rested at any bonfire (a checkpoint exists)
        Milestone,      // progress contains `milestone` (lessons, rewards, exits...)
        ReachArea,      // came within `radius` of `area`
        DrankFlask,     // recovered safely after the first wolf
        Looked,         // held Alt to look around
    }

    [Serializable]
    public sealed class TutorialStep
    {
        [Tooltip("Stable id; completion is saved as tutorial/step/<id>. Never rename a shipped id.")]
        public string id;
        [TextArea(2, 4), Tooltip("Hint text. {tokens} become key names, e.g. {interact}, {throw}, {recall}.")]
        public string hint;
        public TutorialCondition condition;
        [Tooltip("Units for Moved/Sprinted, count for Dodged.")] public float amount = 1f;
        [Tooltip("Progress id for Milestone.")] public string milestone;
        [Tooltip("World position for ReachArea (layout anchors are editor-only, so positions are stored).")] public Vector2 area;
        [Min(0.5f)] public float radius = 4f;

        [Tooltip("Optional world-space teaching cue; leave empty for a screen hint.")]
        public Transform cueTarget;
        public bool cueAtPlayer;
        public Vector2 cueOffset = new Vector2(0, 2.5f);

        public TutorialStep() { }
        public TutorialStep(string id, string hint, TutorialCondition condition) { this.id = id; this.hint = hint; this.condition = condition; }
    }

    // Event-driven, non-gating guide: every step's condition is tracked from the moment the scene
    // starts (early successes count), the first incomplete step is the single on-screen hint, and each
    // completion is saved as a milestone so reloads resume where the player is.
    [DisallowMultipleComponent]
    public sealed class TutorialGuide : MonoBehaviour, IProgressParticipant
    {
        private const string StepPrefix = "tutorial/step/";
        [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();
        [Tooltip("Seconds the ✓ completed state stays before the next hint appears.")]
        [SerializeField, Min(0f)] private float completedHold = 1.2f;
        [Tooltip("Seconds of a fresh scene before the first hint appears.")]
        [SerializeField, Min(0f)] private float startDelay = 1.5f;

        private readonly HashSet<string> done = new HashSet<string>();
        private PlayerHealth player;
        private PlayerMovement movement;
        private PlayerCombatController combat;
        private PlayerDash dash;
        private PlayerFlask flask;
        private PlayerBonfireInteraction interaction;
        private UI.PauseMenuController pauseMenu;
        private Vector2 lastPosition;
        private float moved, sprinted, moveHintSeconds;
        private int dashes, drinks;
        private bool looked;
        private float safeSince = -1f;
        private RecallAwakeningStone stone;
        private Vector3? completedPosition;
        private uint lastDashSequence;
        private bool restored;
        private float showAfter;
        private string completedHint;
        private float completedUntil;

        public IReadOnlyList<TutorialStep> Steps => steps;
        public bool IsFinished => steps.Count > 0 && CurrentIndex < 0;
        public int CurrentIndex { get { for (int i = 0; i < steps.Count; i++) if (!done.Contains(steps[i].id)) return i; return -1; } }
        public bool IsDone(string stepId) => done.Contains(stepId);
        public event Action<TutorialStep> StepCompleted;

        private void Awake()
        {
            if (steps.Count == 0) steps = DefaultSteps();
        }

        private void Start()
        {
            showAfter = Time.time + startDelay;
            FindPlayer();
        }

        private void FindPlayer()
        {
            if (player != null) return;
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
            movement = player.GetComponent<PlayerMovement>();
            combat = player.GetComponent<PlayerCombatController>();
            dash = player.GetComponent<PlayerDash>();
            flask = player.GetComponent<PlayerFlask>();
            if (flask != null) flask.Healed += OnHealed;
            stone = FindFirstObjectByType<RecallAwakeningStone>();
            interaction = player.GetComponent<PlayerBonfireInteraction>();
            pauseMenu = player.GetComponent<UI.PauseMenuController>();
            lastPosition = player.transform.position;
            lastDashSequence = dash != null ? dash.Sequence : 0u;
        }

        private void Update()
        {
            FindPlayer();
            if (player == null) return;
            Track();
            for (int i = 0; i < steps.Count; i++)
                if (!done.Contains(steps[i].id) && IsMet(steps[i])) Complete(steps[i]);
            Present();
        }

        private void Track()
        {
            Vector2 position = player.transform.position;
            float step = (position - lastPosition).magnitude;
            lastPosition = position;
            // Ignore teleports (respawn, travel); count only plausible movement.
            if (player.IsAlive && step < 1f && Time.deltaTime > 0f)
            {
                moved += step;
                if (movement != null && movement.IsSprinting) sprinted += step;
            }
            if (dash != null && dash.Sequence != lastDashSequence)
            {
                lastDashSequence = dash.Sequence;
                if (dash.IsDashing) dashes++;
            }
            var look = Camera.main != null ? Camera.main.GetComponent<CameraFreelook2D>() : null;
            if (look != null && look.IsLooking) looked = true;
            if (EncounterState.InCombat) safeSince = -1f;
            else if (safeSince < 0f) safeSince = Time.time;
        }

        private void OnHealed()
        {
            var progress = GameSession.Instance?.Progress;
            if (progress != null && progress.Has("tutorial/enemy/first-wolf")) drinks++;
        }

        private bool IsMet(TutorialStep step)
        {
            var progress = GameSession.Instance != null ? GameSession.Instance.Progress : null;
            switch (step.condition)
            {
                case TutorialCondition.Moved: return moved >= step.amount && moveHintSeconds >= 6f;
                case TutorialCondition.Sprinted: return sprinted >= step.amount;
                case TutorialCondition.Dodged: return dashes >= Mathf.Max(1f, step.amount);
                case TutorialCondition.HasAxe: return combat != null && combat.Weapon != null;
                case TutorialCondition.RecallUnlocked: return combat != null && combat.CanRecall;
                case TutorialCondition.Rested: return GameSession.Instance != null && GameSession.Instance.Checkpoints.HasCheckpoint;
                case TutorialCondition.Milestone: return progress != null && progress.Has(step.milestone);
                case TutorialCondition.DrankFlask:
                    return progress != null && progress.Has("tutorial/enemy/first-wolf") &&
                        !EncounterState.InCombat && safeSince >= 0f && Time.time - safeSince >= .5f &&
                        flask != null && !flask.IsDrinking &&
                        (drinks >= 1 || player.Health.Health >= player.Health.MaxHealth || flask.Charges == 0);
                case TutorialCondition.Looked: return looked || (progress != null && progress.Has(StepPrefix + "road"));
                case TutorialCondition.ReachArea: return player.IsAlive && ((Vector2)player.transform.position - step.area).sqrMagnitude <= step.radius * step.radius;
                default: return false;
            }
        }

        private void Complete(TutorialStep step)
        {
            bool wasCurrent = CurrentIndex >= 0 && steps[CurrentIndex] == step;
            done.Add(step.id);
            GameSession.Instance?.Progress.Complete(StepPrefix + step.id);
            // Persist quietly with the next save; lessons and rewards already save their own milestones.
            // No ✓ flash for steps a loaded save already satisfies.
            if (wasCurrent && restored && Time.time >= showAfter)
            {
                completedHint = step.id == "stone" ? "Recall awakened\nThrow, then Recall {recall}" : null;
                completedPosition = player.transform.position + Vector3.up * 2.5f;
                completedUntil = Time.time + (step.id == "stone" ? 4f : step.id == "flask" ? 0f : completedHold);
            }
            StepCompleted?.Invoke(step);
        }

        private void Present()
        {
            bool blocked = !player.IsAlive || UI.MenuStack.IsAnyOpen || (interaction != null && interaction.IsOpen) ||
                (pauseMenu != null && pauseMenu.BlocksGameplay) || Time.time < showAfter ||
                // Defer teaching text while fighting; prompts and receipts still show.
                EncounterState.InCombat || (stone != null && stone.IsAwakening) || (flask != null && flask.IsDrinking);
            if (blocked) { HudNotifications.ClearHint(); return; }
            if (Time.time < completedUntil && completedHint != null)
            {
                HudNotifications.SetHint(ControlLabels.Format(completedHint), true, completedPosition);
                return;
            }
            int index = CurrentIndex;
            // Local stone teaching takes precedence over skipped optional lessons in the courtyard.
            if (stone != null && stone.IsReady && !stone.IsAwakened &&
                Vector2.Distance(player.transform.position, stone.transform.position) < 12f)
                index = steps.FindIndex(s => s.id == "stone");
            if (index < 0 || Time.time < completedUntil || (steps[index].id == "flask" && Time.time - safeSince < .5f))
            { HudNotifications.ClearHint(); return; }
            var current = steps[index];
            // The bonfire already owns a proximity-sensitive [F] Rest prompt.
            if (current.id == "rest") { HudNotifications.ClearHint(); return; }
            if (current.id == "move") moveHintSeconds += Time.deltaTime;
            Vector3? position = current.cueAtPlayer ? player.transform.position + (Vector3)current.cueOffset :
                current.cueTarget != null ? current.cueTarget.position + (Vector3)current.cueOffset : (Vector3?)null;
            string teaching = current.hint;
            if (current.id == "throw" && combat.Weapon != null && combat.Weapon.IsAway)
            {
                if (combat.Weapon.State != AxeState.Stuck) { HudNotifications.ClearHint(); return; }
                teaching = "Walk to your axe";
                position = combat.Weapon.transform.position + Vector3.up;
            }
            HudNotifications.SetHint(ControlLabels.Format(teaching), false, position);
        }

        private void OnDisable() => HudNotifications.ClearHint();
        private void OnDestroy() { if (flask != null) flask.Healed -= OnHealed; }

        public void CaptureProgress(ProgressState state) { foreach (var id in done) state.Complete(StepPrefix + id); }

        public void RestoreProgress(ProgressState state)
        {
            done.Clear();
            foreach (var step in steps) if (state.Has(StepPrefix + step.id)) done.Add(step.id);
            restored = true;
        }

        // Default route for Tutorial.unity. Tune text and order in the Inspector; ids are save data.
        // Hints stay short: a few words plus the key (user, 3 Oct).
        public static List<TutorialStep> DefaultSteps() => new List<TutorialStep>
        {
            new TutorialStep("move", "Head into the village {move}", TutorialCondition.Moved) { amount = 3f },
            new TutorialStep("take-axe", "Take your axe from the stump {interact}", TutorialCondition.HasAxe),
            new TutorialStep("throw", "Throw at a target {throw}, then pick it up", TutorialCondition.Milestone) { milestone = "tutorial/lesson/throw-retrieve" },
            new TutorialStep("melee", "Strike the dummy {attack}", TutorialCondition.Milestone) { milestone = "tutorial/lesson/melee" },
            new TutorialStep("dodge", "Dodge {dash}", TutorialCondition.Dodged),
            new TutorialStep("sprint", "Sprint {sprint}", TutorialCondition.Sprinted) { amount = 4f },
            new TutorialStep("look", "Hold {look} to look around", TutorialCondition.Looked) { cueAtPlayer = true },
            new TutorialStep("road", "Follow the road south to the bridge", TutorialCondition.ReachArea) { area = new Vector2(72f, 27.5f), radius = 5f },
            new TutorialStep("first-wolf", "A wolf guards the ruins. Dodge its lunge {dash}", TutorialCondition.Milestone) { milestone = "tutorial/enemy/first-wolf" },
            new TutorialStep("flask", "Drink a flask {heal}", TutorialCondition.DrankFlask),
            new TutorialStep("stone", "The stone seals the way\nThrow {throw}", TutorialCondition.RecallUnlocked),
            new TutorialStep("first-shard", "Follow the trail north", TutorialCondition.Milestone) { milestone = "shard/collected/tutorial/first-enemy" },
            new TutorialStep("rest", "", TutorialCondition.Rested),
            new TutorialStep("exit", "Take the road northeast", TutorialCondition.Milestone) { milestone = "tutorial/complete" },
        };
    }
}
