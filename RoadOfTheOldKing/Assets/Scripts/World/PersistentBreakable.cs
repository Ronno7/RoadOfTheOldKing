using TheLostShrine.Combat;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    // Only route obstacles receive this adapter; future loot props can reset on rest.
    [RequireComponent(typeof(Breakable))]
    public sealed class PersistentBreakable : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string progressId;
        public void CaptureProgress(ProgressState state)
        {
            if (GetComponent<Breakable>().IsBroken)
                state.Complete(progressId);
        }
        public void RestoreProgress(ProgressState state)
        {
            if (state.Has(progressId))
                GetComponent<Breakable>().RestoreBrokenState();
        }
    }
}
