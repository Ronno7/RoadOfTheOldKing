using System.Collections;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    // The courtyard's north seal. With a seal release, Recall's first use opens it: throw the axe into the gate,
    // then Recall it out (user, 22 Nov: new players didn't notice their new ability). Without one, it opens with
    // the Recall unlock as before.
    public sealed class RecallSealGate : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private RecallAwakeningStone stone;
        [Tooltip("Opens the gate when released; saves its own progress id.")]
        [SerializeField] private RoadOfTheOldKing.World.RecallRelease seal;
        [Tooltip("Saves from before the seal release that are already past the gate open it (a later Tutorial step).")]
        [SerializeField] private string pastGateMilestone = "tutorial/step/pack";
        [Tooltip("The gate: its art and collision sink and fade together.")]
        [SerializeField] private GameObject barrier;
        [SerializeField] private SpriteRenderer groundShadow;
        [SerializeField] private float openingSeconds = .8f;
        public bool IsOpen { get; private set; }
        private Coroutine opening;
        private Vector3 closedPosition;
        private SpriteRenderer[] art = System.Array.Empty<SpriteRenderer>();

        private void Awake()
        {
            if (barrier == null) return;
            closedPosition = barrier.transform.localPosition;
            art = barrier.GetComponentsInChildren<SpriteRenderer>(true);
        }
        private void OnEnable()
        {
            if (seal != null) seal.Released += Open;
            else if (stone != null) stone.Awakened += Open;
        }
        private void Start()
        {
            if (GameSession.Instance != null && Opened(GameSession.Instance.Progress))
                SetOpen();
        }
        private bool Opened(ProgressState state) => seal == null ? state.recallUnlocked :
            state.Has(seal.ProgressId) || (state.recallUnlocked && !string.IsNullOrEmpty(pastGateMilestone) && state.Has(pastGateMilestone));
        private void OnDisable()
        {
            if (seal != null) seal.Released -= Open;
            else if (stone != null) stone.Awakened -= Open;
            if (opening != null) { StopCoroutine(opening); opening = null; SetOpen(); }
        }
        private void Open() { if (!IsOpen && opening == null) opening = StartCoroutine(Lower()); }
        private IEnumerator Lower()
        {
            for (float elapsed = 0; elapsed < openingSeconds; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, openingSeconds));
                barrier.transform.localPosition = closedPosition + Vector3.down * t;
                foreach (var r in art) r.color = new Color(1, 1, 1, 1 - t);
                // The gate sinks; its ground contact stays fixed and fades with the gate.
                if (groundShadow != null) groundShadow.color = new Color(1, 1, 1, 1 - t);
                yield return null;
            }
            opening = null;
            SetOpen();
        }
        private void SetOpen()
        {
            IsOpen = true;
            if (barrier != null) barrier.SetActive(false);
            if (groundShadow != null) groundShadow.enabled = false;
        }
        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state) { if (Opened(state)) SetOpen(); }
    }
}
