using RoadOfTheOldKing.Enemies;
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
        Rested,         // a fire is the respawn point: lit or rested at (a checkpoint exists)
        Milestone,      // progress contains `milestone` (lessons, rewards, exits...)
        ReachArea,      // came within `radius` of `area`
        DrankFlask,     // recovered safely after the fight named by `milestone` (empty: the first wolf)
        Looked,         // held Alt to look around
        Zoomed,         // changed the camera zoom, or read the note for a few seconds
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
        [Tooltip("Progress id for Milestone; for Rested, the fire that counts (empty: any fire); for DrankFlask, the fight " +
            "it follows (empty: the first wolf).")] public string milestone;
        [Tooltip("World position for ReachArea (layout anchors are editor-only, so positions are stored).")] public Vector2 area;
        [Min(0.5f)] public float radius = 4f;

        [Tooltip("Optional world-space teaching cue; leave empty for a screen hint.")]
        public Transform cueTarget;
        public bool cueAtPlayer;
        public Vector2 cueOffset = new Vector2(0, 2.5f);
        [Tooltip("Show only while the hero is within this many units of the cue target, with no screen fallback. 0: anywhere.")]
        [Min(0f)] public float nearRadius;
        [Tooltip("Seconds after the step becomes the hint before its text appears.")]
        [Min(0f)] public float appearDelay;
        [Tooltip("Seconds the text stays once shown, even if a fight starts (teaching text otherwise steps aside in combat).")]
        [Min(0f)] public float minShowSeconds;

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
        private float moved, sprinted, moveHintSeconds, zoomHintSeconds;
        private float? zoomStart;
        private bool zoomed;
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
        private int hintIndex = -2, shownIndex = -2;
        private float hintSince, shownUntil;

        public IReadOnlyList<TutorialStep> Steps => steps;
        public bool IsDone(string stepId) => done.Contains(stepId);

        // The step the hint teaches: the first incomplete one after the furthest route landmark reached.
        // Lessons skipped before a landmark stay incomplete but stop following the player into later
        // areas (playtest, 22 Oct); they still complete if done later.
        public int HintIndex
        {
            get
            {
                int passed = -1;
                for (int i = 0; i < steps.Count; i++)
                    if (done.Contains(steps[i].id) && IsLandmark(steps[i].condition)) passed = i;
                for (int i = passed + 1; i < steps.Count; i++) if (!done.Contains(steps[i].id)) return i;
                return -1;
            }
        }

        private static bool IsLandmark(TutorialCondition condition) =>
            condition == TutorialCondition.ReachArea || condition == TutorialCondition.RecallUnlocked || condition == TutorialCondition.Rested;

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
            var zoom = Camera.main != null ? Camera.main.GetComponent<CameraZoom2D>() : null;
            if (zoom != null)
            {
                if (zoomStart == null) zoomStart = zoom.PlayerSize;
                else if (Mathf.Abs(zoom.PlayerSize - zoomStart.Value) > .05f) zoomed = true;
            }
            if (EncounterState.InCombat) safeSince = -1f;
            else if (safeSince < 0f) safeSince = Time.time;
        }

        private void OnHealed()
        {
            var progress = GameSession.Instance?.Progress;
            var lesson = steps.Find(s => s.condition == TutorialCondition.DrankFlask);
            if (progress != null && progress.Has(FlaskFight(lesson))) drinks++;
        }

        // The fight the flask lesson follows: the road's rats since 22 Nov (user: take the flask out of the Recall courtyard).
        private static string FlaskFight(TutorialStep step) =>
            step != null && !string.IsNullOrEmpty(step.milestone) ? step.milestone : "tutorial/enemy/first-wolf";

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
                case TutorialCondition.Rested: return progress != null && !string.IsNullOrEmpty(progress.checkpointId) && (string.IsNullOrEmpty(step.milestone) || progress.checkpointId == step.milestone);
                case TutorialCondition.Milestone: return progress != null && progress.Has(step.milestone);
                case TutorialCondition.DrankFlask:
                    return progress != null && progress.Has(FlaskFight(step)) &&
                        !EncounterState.InCombat && safeSince >= 0f && Time.time - safeSince >= .5f &&
                        flask != null && !flask.IsDrinking &&
                        (drinks >= 1 || player.Health.Health >= player.Health.MaxHealth || flask.Charges == 0);
                case TutorialCondition.Looked: return looked || (progress != null && progress.Has(StepPrefix + "road"));
                // A quick note (user, 15 Nov): zooming completes it, or it steps aside for the next lesson after 6 s. Without
                // text of its own (since 23 Nov its words are on the zoom sign) there is nothing to read, so it never holds up
                // the lessons after it (review, 23 Nov: the brush and axe cues waited for a later landmark).
                case TutorialCondition.Zoomed: return zoomed || zoomHintSeconds >= 6f || string.IsNullOrEmpty(step.hint);
                case TutorialCondition.ReachArea: return player.IsAlive && ((Vector2)player.transform.position - step.area).sqrMagnitude <= step.radius * step.radius;
                default: return false;
            }
        }

        private void Complete(TutorialStep step)
        {
            bool wasCurrent = HintIndex >= 0 && steps[HintIndex] == step;
            done.Add(step.id);
            GameSession.Instance?.Progress.Complete(StepPrefix + step.id);
            // Persist quietly with the next save; lessons and rewards already save their own milestones.
            // No ✓ flash for steps a loaded save already satisfies.
            if (wasCurrent && restored && Time.time >= showAfter)
            {
                // The sealed gate teaches the Recall throw now (user, 23 Nov: "shorten ... to just "Recall Awakened!"").
                completedHint = step.id == "stone" ? "Recall Awakened!" : null;
                completedPosition = player.transform.position + Vector3.up * 2.5f;
                completedUntil = Time.time + (step.id == "stone" ? 4f : step.id == "flask" ? 0f : completedHold);
            }
        }

        private void Present()
        {
            bool blocked = !player.IsAlive || UI.MenuStack.IsAnyOpen || (interaction != null && interaction.IsOpen) ||
                (pauseMenu != null && pauseMenu.BlocksGameplay) || Time.time < showAfter ||
                (stone != null && stone.IsAwakening) || (flask != null && flask.IsDrinking);
            if (blocked) { HudNotifications.ClearHint(); return; }
            // Defer teaching text while fighting; prompts and receipts still show.
            bool fighting = EncounterState.InCombat;
            if (Time.time < completedUntil && completedHint != null)
            {
                if (fighting) HudNotifications.ClearHint();
                else HudNotifications.SetHint(completedHint, true, completedPosition);
                return;
            }
            int index = HintIndex;
            // The flask follows its fight ahead of optional lessons still open before it (sprint, free look).
            int flaskIndex = steps.FindIndex(s => s.condition == TutorialCondition.DrankFlask);
            var progress = GameSession.Instance != null ? GameSession.Instance.Progress : null;
            if (index >= 0 && flaskIndex > index && !done.Contains(steps[flaskIndex].id) && progress != null &&
                progress.Has(FlaskFight(steps[flaskIndex])))
                index = flaskIndex;
            // Local stone teaching takes precedence over skipped optional lessons in the courtyard.
            if (stone != null && stone.IsReady && !stone.IsAwakened &&
                Vector2.Distance(player.transform.position, stone.transform.position) < 12f)
                index = steps.FindIndex(s => s.id == "stone");
            if (index != hintIndex) { hintIndex = index; hintSince = Time.time; }
            if (index < 0 || Time.time < completedUntil || (steps[index].id == "flask" && Time.time - safeSince < .5f) ||
                (steps[index].condition == TutorialCondition.DrankFlask && (progress == null || !progress.Has(FlaskFight(steps[index])))))
            { HudNotifications.ClearHint(); return; }
            var current = steps[index];
            // A step that teaches its fight (minShowSeconds) may appear in it and holds its text for that long; others step
            // aside (user, 16 Nov: the wolf text "leaves too quickly").
            bool holding = index == shownIndex ? Time.time < shownUntil : current.minShowSeconds > 0f;
            if (fighting && !holding) { HudNotifications.ClearHint(); return; }
            // ...and wait a moment before appearing (the same note: it "pops up too fast").
            if (Time.time - hintSince < current.appearDelay) { HudNotifications.ClearHint(); return; }
            // The throw lesson's axe, stuck in something, waits to be picked up; its sign is back where it was thrown.
            if (current.id == "throw" && combat.Weapon != null && combat.Weapon.IsAway)
            {
                if (combat.Weapon.State != AxeState.Stuck) HudNotifications.ClearHint();
                else HudNotifications.SetHint("Walk to your axe", false, combat.Weapon.transform.position + Vector3.up);
                return;
            }
            // The bonfire owns proximity-sensitive [F] Light / [F] Rest prompts; route landmarks without text leave the
            // way for the player to find (user, 15 Nov).
            if (current.id == "rest" || string.IsNullOrEmpty(current.hint)) { HudNotifications.ClearHint(); return; }
            // Spatial only: nothing until the hero is near the target (user, 16 Nov, the axe's hint).
            if (current.nearRadius > 0f && (current.cueTarget == null ||
                Vector2.Distance(player.transform.position, current.cueTarget.position) > current.nearRadius))
            { HudNotifications.ClearHint(); return; }
            if (shownIndex != index) { shownIndex = index; shownUntil = Time.time + current.minShowSeconds; }
            if (current.id == "move") moveHintSeconds += Time.deltaTime;
            // Only time the player could read it counts toward the 6 s: behind the axe's prompt it isn't showing (18 Nov).
            if (current.condition == TutorialCondition.Zoomed && HudNotifications.HintOnScreen) zoomHintSeconds += Time.deltaTime;
            Vector3? position = current.cueAtPlayer ? player.transform.position + (Vector3)current.cueOffset :
                current.cueTarget != null ? current.cueTarget.position + (Vector3)current.cueOffset : (Vector3?)null;
            // A step with a near radius is spatial only (the axe's hint, user 16 Nov): off screen it waits, never at the bottom.
            HudNotifications.SetHint(current.hint, false, position, current.nearRadius > 0f);
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
        // Hints stay short: a few words plus the key (user, 3 Oct). Controls only: route landmarks carry no directions.
        public static List<TutorialStep> DefaultSteps() => new List<TutorialStep>
        {
            new TutorialStep("move", "Move {move}", TutorialCondition.Moved) { amount = 3f },
            new TutorialStep("zoom", "Zoom out {zoom}", TutorialCondition.Zoomed),
            new TutorialStep("dodge", "Dodge {dash}", TutorialCondition.Dodged),
            new TutorialStep("take-axe", "It's dangerous to go alone! Take this. {interact}", TutorialCondition.HasAxe) { nearRadius = 4f },
            new TutorialStep("throw", "Throw at a target {throw}, then pick it up", TutorialCondition.Milestone) { milestone = "tutorial/lesson/throw-retrieve" },
            new TutorialStep("melee", "Strike the dummy {attack}", TutorialCondition.Milestone) { milestone = "tutorial/lesson/melee" },
            new TutorialStep("sprint", "Sprint {sprint}", TutorialCondition.Sprinted) { amount = 4f },
            new TutorialStep("look", "Hold {look} to look around", TutorialCondition.Looked) { cueAtPlayer = true },
            new TutorialStep("flask", "Drink a flask {heal}", TutorialCondition.DrankFlask) { milestone = "tutorial/enemy/field-rats" },
            new TutorialStep("road", "", TutorialCondition.ReachArea) { area = new Vector2(72f, 27.5f), radius = 5f },
            new TutorialStep("first-wolf", "A wolf guards the ruins. Dodge its lunge {dash}", TutorialCondition.Milestone) { milestone = "tutorial/enemy/first-wolf", appearDelay = .75f, minShowSeconds = 4f },
            new TutorialStep("stone", "The stone seals the way\nThrow {throw}", TutorialCondition.RecallUnlocked),
            new TutorialStep("first-shard", "Take the Sun Shard {interact}", TutorialCondition.Milestone) { milestone = RewardKeys.ShardCollected("tutorial/first-enemy") },
            new TutorialStep("rest", "", TutorialCondition.Rested) { milestone = "tutorial/roadside-fire" },
            new TutorialStep("exit", "", TutorialCondition.Milestone) { milestone = "tutorial/complete" },
        };
    }
}
