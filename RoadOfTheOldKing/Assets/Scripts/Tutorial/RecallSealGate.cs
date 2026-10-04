using System.Collections;
using TheLostShrine.Progression;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TheLostShrine.Tutorial
{
    // The courtyard's north seal is derived from the existing Recall unlock; no second save flag.
    public sealed class RecallSealGate : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private RecallAwakeningStone stone;
        [SerializeField] private GameObject barrier;
        [SerializeField] private Tilemap stonework;
        [SerializeField] private float openingSeconds = .8f;
        public bool IsOpen { get; private set; }
        private Coroutine opening;
        private Vector3 closedPosition;

        private void Awake() { if (barrier != null) closedPosition = barrier.transform.localPosition; }
        private void OnEnable() { if (stone != null) stone.Awakened += Open; }
        private void Start()
        {
            if (CheckpointSession.Instance != null && CheckpointSession.Instance.Progress.recallUnlocked)
                SetOpen();
        }
        private void OnDisable()
        {
            if (stone != null) stone.Awakened -= Open;
            if (opening != null) { StopCoroutine(opening); opening = null; SetOpen(); }
        }
        private void Open() { if (!IsOpen && opening == null) opening = StartCoroutine(Lower()); }
        private IEnumerator Lower()
        {
            for (float elapsed = 0; elapsed < openingSeconds; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, openingSeconds));
                barrier.transform.localPosition = closedPosition + Vector3.down * t;
                if (stonework != null) stonework.color = new Color(1, 1, 1, 1 - t);
                yield return null;
            }
            opening = null;
            SetOpen();
        }
        private void SetOpen() { IsOpen = true; if (barrier != null) barrier.SetActive(false); }
        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state) { if (state.recallUnlocked) SetOpen(); }
    }
}
