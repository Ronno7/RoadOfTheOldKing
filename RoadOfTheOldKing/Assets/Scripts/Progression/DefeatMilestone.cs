using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Progression
{
    // Records a progress milestone (and saves) the first time an enemy is defeated, so later beats can
    // depend on the fight without a reward attached (e.g. the Tutorial's lone wolf wakes the Recall stone).
    [DisallowMultipleComponent]
    public sealed class DefeatMilestone : MonoBehaviour
    {
        [SerializeField] private Damageable source;
        [Tooltip("Progress id; save data, so never rename a shipped id.")]
        [SerializeField] private string milestone;

        public string Milestone => milestone;

        private void Awake()
        {
            if (source == null) source = GetComponent<Damageable>();
        }

        private void OnEnable() { if (source != null) source.Defeated += OnDefeated; }
        private void OnDisable() { if (source != null) source.Defeated -= OnDefeated; }

        private void OnDefeated()
        {
            var session = CheckpointSession.Instance;
            if (session == null || string.IsNullOrEmpty(milestone) || session.Progress.Has(milestone)) return;
            session.Progress.Complete(milestone);
            session.SaveProgress();
        }
    }
}
