using System;
using System.Collections.Generic;
using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.UI;
using TheLostShrine.World;
using UnityEngine;

namespace TheLostShrine.Tutorial
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
        private PlayerBonfireInteraction interaction;
        private UI.PauseMenuController pauseMenu;
        private Vector2 lastPosition;
        private float moved, sprinted;
        private int dashes;
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
        }

        private bool IsMet(TutorialStep step)
        {
            var progress = CheckpointSession.Instance != null ? CheckpointSession.Instance.Progress : null;
            switch (step.condition)
            {
                case TutorialCondition.Moved: return moved >= step.amount;
                case TutorialCondition.Sprinted: return sprinted >= step.amount;
                case TutorialCondition.Dodged: return dashes >= Mathf.Max(1f, step.amount);
                case TutorialCondition.HasAxe: return combat != null && combat.Weapon != null;
                case TutorialCondition.RecallUnlocked: return combat != null && combat.CanRecall;
                case TutorialCondition.Rested: return CheckpointSession.Instance != null && CheckpointSession.Instance.HasCheckpoint;
                case TutorialCondition.Milestone: return progress != null && progress.Has(step.milestone);
                case TutorialCondition.ReachArea: return player.IsAlive && ((Vector2)player.transform.position - step.area).sqrMagnitude <= step.radius * step.radius;
                default: return false;
            }
        }

        private void Complete(TutorialStep step)
        {
            bool wasCurrent = CurrentIndex >= 0 && steps[CurrentIndex] == step;
            done.Add(step.id);
            CheckpointSession.Instance?.Progress.Complete(StepPrefix + step.id);
            // Persist quietly with the next save; lessons and rewards already save their own milestones.
            // No ✓ flash for steps a loaded save already satisfies.
            if (wasCurrent && restored && Time.time >= showAfter)
            {
                completedHint = step.hint;
                completedUntil = Time.time + completedHold;
            }
            StepCompleted?.Invoke(step);
        }

        private void Present()
        {
            bool blocked = !player.IsAlive || UI.MenuStack.IsAnyOpen || (interaction != null && interaction.IsOpen) ||
                (pauseMenu != null && pauseMenu.BlocksGameplay) || Time.time < showAfter ||
                // Defer teaching text while fighting; prompts and receipts still show.
                EncounterState.InCombat;
            if (blocked) { HudNotifications.ClearHint(); return; }
            if (Time.time < completedUntil && completedHint != null)
            {
                HudNotifications.SetHint(ControlLabels.Format(completedHint), true);
                return;
            }
            int index = CurrentIndex;
            HudNotifications.SetHint(index >= 0 ? ControlLabels.Format(steps[index].hint) : null);
        }

        private void OnDisable() => HudNotifications.ClearHint();

        public void CaptureProgress(ProgressState state) { foreach (var id in done) state.Complete(StepPrefix + id); }

        public void RestoreProgress(ProgressState state)
        {
            done.Clear();
            foreach (var step in steps) if (state.Has(StepPrefix + step.id)) done.Add(step.id);
            restored = true;
        }

        // Default route for Tutorial.unity. Tune text and order in the Inspector; ids are save data.
        public static List<TutorialStep> DefaultSteps() => new List<TutorialStep>
        {
            new TutorialStep("move", "Move with {move}. Head into the village.", TutorialCondition.Moved) { amount = 3f },
            new TutorialStep("take-axe", "Find your axe: it rests in the stump by the woodpile. Walk up to it and press {interact}.", TutorialCondition.HasAxe),
            new TutorialStep("throw", "Throw at a practice target: tap {throw}, or hold {throw} to aim. Walk over the axe to pick it back up.", TutorialCondition.Milestone) { milestone = "tutorial/lesson/throw-retrieve" },
            new TutorialStep("melee", "Strike the practice dummy with {attack}.", TutorialCondition.Milestone) { milestone = "tutorial/lesson/melee" },
            new TutorialStep("dodge", "Press {dash} to dodge. A well-timed dodge slips through attacks.", TutorialCondition.Dodged),
            new TutorialStep("sprint", "Hold {sprint} to run. Running and dodging spend stamina.", TutorialCondition.Sprinted) { amount = 4f },
            new TutorialStep("road", "Follow the forest road south to the old bridge.", TutorialCondition.ReachArea) { area = new Vector2(72f, 27.5f), radius = 5f },
            new TutorialStep("stone", "A carved stone stands in the ruins past the bridge. Throw your axe at it.", TutorialCondition.RecallUnlocked),
            new TutorialStep("recall-drill", "In the clearing north of the stone: throw into the far post, then move so the near post is between you and the axe, and press {recall}.", TutorialCondition.Milestone) { milestone = "tutorial/lesson/recall-drill" },
            new TutorialStep("first-shard", "Something prowls the clearing to the north. Defeat it, then press {interact} to take the Sun Shard.", TutorialCondition.Milestone) { milestone = "shard/collected/tutorial/first-enemy" },
            new TutorialStep("rest", "Rest at the roadside bonfire with {interact}. Resting heals you and saves.", TutorialCondition.Rested),
            new TutorialStep("exit", "The old road leads northeast, out to the Green Lowlands.", TutorialCondition.Milestone) { milestone = "tutorial/complete" },
        };
    }
}
