using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.World;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Prototype
{
    public enum PrototypeStep { Pickup, Chop, Throw, Retrieve, Bushes, Stone, UnlockRecall, RecallPractice, Enemy, Bonfire, Puzzle, ExitBonfire, Complete }

    // Scene-specific teaching sequence. Combat and unlock systems remain independent.
    public sealed class PrototypeLoopGuide : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private Damageable[] dummies;
        [SerializeField] private Breakable[] bushes;
        [SerializeField] private Breakable crackedStone;
        [SerializeField] private Damageable enemy;
        [SerializeField] private GameObject practiceExitGate;
        [SerializeField] private GameObject returnGate;
        [SerializeField] private GameObject enemyGate;
        [SerializeField] private GameObject bonfireGate;
        [SerializeField] private GameObject puzzleEntryGate;
        [SerializeField] private Bonfire firstBonfire;
        [SerializeField] private Bonfire exitBonfire;
        [SerializeField] private ThrowRecallPuzzle puzzle;
        public PrototypeStep Step { get; private set; }

        public string Instruction
        {
            get
            {
                switch (Step)
                {
                    case PrototypeStep.Pickup: return "Walk over the hatchet in front of you.";
                    case PrototypeStep.Chop: return "Aim at a plain dummy and left-click to slash. Attacks use stamina; walk to recover.";
                    case PrototypeStep.Throw: return "Step back, aim at a plain dummy, and press E to throw.";
                    case PrototypeStep.Retrieve: return "Walk over to retrieve your hatchet. Recall comes later.";
                    case PrototypeStep.Bushes: return "The east gate is open. Chop both bushes along the path.";
                    case PrototypeStep.Stone: return "Hold right-click until gold, then release beside the cracked stone.";
                    case PrototypeStep.UnlockRecall: return "Continue east to the Recall altar.";
                    case PrototypeStep.RecallPractice: return "Follow the upper path back to the dummies. Throw past them, move, then E to recall through one.";
                    case PrototypeStep.Enemy: return "Take the northwest path. Space: short dash / dodge. Avoid the marked strike, then counterattack. Watch your stamina.";
                    case PrototypeStep.Bonfire: return "Collect the sentinel's Sun Shard, then follow the east path. F at the fire heals and saves.";
                    case PrototypeStep.Puzzle: return puzzle != null && puzzle.IsArmed
                        ? "Move down to the blue floor mark. Press E to recall through the blue target and open the door."
                        : "Stand on the gold floor mark. Aim at the gold target to the right and press E to throw.";
                    case PrototypeStep.ExitBonfire: return "Puzzle solved: one Sun Shard earned. Explore the side path beyond the door, then F at the second fire to upgrade.";
                    default: return CheckpointSession.Instance != null && CheckpointSession.Instance.Upgrades.NextTier != null
                        ? "Find all 3 Sun Shards: sentinel, puzzle, and the side path beyond the door. Spend them at the second fire for one hatchet upgrade."
                        : "Upgrade chosen. Travel back to try it on the dummies or sentinel. Resting never replenishes Sun Shards.";
                }
            }
        }

        private void OnEnable()
        {
            foreach (var dummy in dummies)
                if (dummy != null)
                    dummy.HitReceived += OnDummyHit;
        }
        private void OnDisable()
        {
            foreach (var dummy in dummies)
                if (dummy != null)
                    dummy.HitReceived -= OnDummyHit;
        }

        private void Update()
        {
            if (player == null)
                return;
            if (Step == PrototypeStep.Pickup && player.Weapon != null)
                Step = PrototypeStep.Chop;
            else if (Step == PrototypeStep.Retrieve && player.Weapon != null && player.Weapon.State == HatchetState.Held)
                Step = PrototypeStep.Bushes;
            else if (Step == PrototypeStep.Bushes && System.Array.TrueForAll(bushes, b => b != null && b.IsBroken))
                Step = PrototypeStep.Stone;
            else if (Step == PrototypeStep.Stone && crackedStone != null && crackedStone.IsBroken)
                Step = PrototypeStep.UnlockRecall;
            else if (Step == PrototypeStep.UnlockRecall && player.CanRecall)
                Step = PrototypeStep.RecallPractice;
            else if (Step == PrototypeStep.Enemy && enemy != null && !enemy.IsAlive)
                Step = PrototypeStep.Bonfire;
            else if (Step == PrototypeStep.Bonfire && firstBonfire != null && firstBonfire.IsDiscovered)
                Step = PrototypeStep.Puzzle;
            else if (Step == PrototypeStep.Puzzle && puzzle != null && puzzle.IsSolved)
                Step = PrototypeStep.ExitBonfire;
            else if (Step == PrototypeStep.ExitBonfire && exitBonfire != null && exitBonfire.IsDiscovered)
                Step = PrototypeStep.Complete;

            UpdateGates();
        }

        private void UpdateGates()
        {
            SetGate(practiceExitGate, Step < PrototypeStep.Bushes);
            SetGate(returnGate, Step < PrototypeStep.RecallPractice);
            SetGate(enemyGate, Step < PrototypeStep.Enemy);
            SetGate(bonfireGate, Step < PrototypeStep.Bonfire);
            SetGate(puzzleEntryGate, Step < PrototypeStep.Puzzle);
        }

        public void CaptureProgress(ProgressState state) => state.Complete("prototype/step/" + Step);

        public void RestoreProgress(ProgressState state)
        {
            foreach (PrototypeStep step in System.Enum.GetValues(typeof(PrototypeStep)))
                if (state.Has("prototype/step/" + step) && step > Step)
                    Step = step;
            UpdateGates();
        }

        private void OnDummyHit(CombatHit hit)
        {
            if (hit.Source != player.gameObject)
                return;
            if (Step == PrototypeStep.Chop && hit.Kind == AttackKind.LightChop)
                Step = PrototypeStep.Throw;
            else if (Step == PrototypeStep.Throw && hit.Kind == AttackKind.Throw)
                Step = PrototypeStep.Retrieve;
            else if (Step == PrototypeStep.RecallPractice && hit.Kind == AttackKind.Recall)
                Step = PrototypeStep.Enemy;
        }

        private static void SetGate(GameObject gate, bool closed)
        {
            if (gate != null && gate.activeSelf != closed)
                gate.SetActive(closed);
        }
    }
}
