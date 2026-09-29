using System;
using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.World
{
    // Any weapon using IHitReceiver can drive this adapter without knowing door logic.
    // Accepted hits are also published (IHitEventSource) so presentation such as WoodTargetFeedback
    // can react. Indicator and label are optional prototype placeholders.
    public sealed class AxePuzzleTarget : MonoBehaviour, IHitReceiver, IHitEventSource
    {
        [SerializeField] private ThrowRecallPuzzle puzzle;
        [SerializeField] private bool anchor;
        [SerializeField] private SpriteRenderer indicator;
        [SerializeField] private TextMesh label;
        public event Action<CombatHit> HitReceived;
        public bool IsAnchor => anchor;

        public bool ReceiveHit(CombatHit hit)
        {
            if (puzzle == null || !puzzle.ReceiveTargetHit(anchor, hit)) return false;
            HitReceived?.Invoke(hit);
            return true;
        }

        private void LateUpdate()
        {
            if (puzzle == null || (indicator == null && label == null))
                return;
            bool active = puzzle.IsSolved || (anchor && puzzle.IsArmed);
            if (indicator != null)
                indicator.color = active ? new Color(0.4f, 1f, 0.55f) : anchor ? new Color(1f, 0.75f, 0.25f) : new Color(0.35f, 0.8f, 1f);
            if (label != null)
                label.text = puzzle.IsSolved ? "ACTIVATED" : anchor ? (puzzle.IsArmed ? "ARMED" : "1 - THROW") : "2 - RECALL";
        }
    }
}
