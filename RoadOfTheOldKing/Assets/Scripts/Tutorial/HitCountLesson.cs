using System;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    // Completes after a number of accepted player hits of the chosen kinds on one target
    // (for example three melee strikes on the practice dummy), then saves a milestone.
    [DisallowMultipleComponent]
    public sealed class HitCountLesson : MonoBehaviour, IProgressParticipant
    {
        [Flags] public enum Kinds { LightChop = 1, ChargedCleave = 2, Throw = 4, Recall = 8 }

        [SerializeField] private string progressId = "tutorial/lesson/melee";
        [Tooltip("Any component implementing IHitEventSource: a Damageable dummy, a practice stand...")]
        [SerializeField] private MonoBehaviour target;
        [SerializeField] private Kinds countedKinds = Kinds.LightChop | Kinds.ChargedCleave;
        [SerializeField, Min(1)] private int requiredHits = 3;
        [SerializeField, TextArea] private string completionNotice = "Combo {attack} {attack} {attack} · Cleave: hold {cleave}";
        private IHitEventSource source;
        private int hits;

        public bool IsComplete { get; private set; }
        public int Hits => hits;
        public event Action Completed;

        private void Awake() => source = target as IHitEventSource ?? (target != null ? target.GetComponent<IHitEventSource>() : null);
        private void OnEnable() { if (source != null) source.HitReceived += OnHit; }
        private void OnDisable() { if (source != null) source.HitReceived -= OnHit; }

        private void OnHit(CombatHit hit)
        {
            if (IsComplete || hit.Source == null || hit.Source.GetComponent<Player.PlayerCombatController>() == null) return;
            if (!Counts(hit.Kind) || ++hits < requiredHits) return;
            IsComplete = true;
            GameSession.Instance?.SaveProgress();
            if (!string.IsNullOrEmpty(completionNotice)) TutorialNotice.Show(completionNotice, 6f);
            Completed?.Invoke();
        }

        private bool Counts(AttackKind kind) =>
            kind == AttackKind.LightChop ? (countedKinds & Kinds.LightChop) != 0 :
            kind == AttackKind.ChargedCleave ? (countedKinds & Kinds.ChargedCleave) != 0 :
            kind == AttackKind.Throw ? (countedKinds & Kinds.Throw) != 0 :
            kind == AttackKind.Recall && (countedKinds & Kinds.Recall) != 0;

        public void CaptureProgress(ProgressState state) { if (IsComplete) state.Complete(progressId); }
        public void RestoreProgress(ProgressState state) { IsComplete = state.Has(progressId); hits = 0; }
    }
}
