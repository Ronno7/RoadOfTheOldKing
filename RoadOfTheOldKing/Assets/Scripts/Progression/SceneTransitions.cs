using System.Collections;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.Progression
{
    // Scene changes: fade out with the world frozen and input locked, load, let GameSession place the
    // player at the arrival, then fade in with input still locked. Lives on the persistent session.
    // Without panel settings the load is instant.
    [DisallowMultipleComponent]
    public sealed class SceneTransitions : MonoBehaviour
    {
        [SerializeField] private PanelSettings panelSettings;
        [SerializeField, Min(0f)] private float fadeOutSeconds = .3f;
        [SerializeField, Min(0f)] private float fadeInSeconds = .35f;
        [SerializeField] private Color curtainColor = new Color(.05f, .045f, .04f);

        private VisualElement curtain;
        private PlayerControlLocks lockedPlayer;

        public bool IsBusy { get; private set; }
        // Spawn point or fire id for the scene being loaded; GameSession reads it on arrival.
        public string ArrivalId { get; private set; }

        // Built on first use: GameSession can start a covered launch load before this component's Awake.
        private void EnsureCurtain()
        {
            if (curtain != null || panelSettings == null) return;
            var child = new GameObject("Transition curtain");
            child.SetActive(false);
            child.transform.SetParent(transform, false);
            var document = child.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            child.SetActive(true);
            curtain = new VisualElement { name = "transition-curtain", pickingMode = PickingMode.Ignore };
            curtain.style.position = Position.Absolute;
            curtain.style.left = curtain.style.top = curtain.style.right = curtain.style.bottom = 0;
            curtain.style.backgroundColor = curtainColor;
            document.rootVisualElement.Add(curtain);
        }

        // covered: start fully black (used at launch, before the first scene has been seen).
        public bool Begin(string scenePath, string arrivalId, PlayerControlLocks player, bool covered = false)
        {
            if (IsBusy || !SceneLoader.CanLoad(scenePath)) return false;
            StartCoroutine(Run(scenePath, arrivalId, player, covered));
            return true;
        }

        // Called by GameSession once the arriving player exists, so input stays locked through the fade-in.
        public void HoldArrivingPlayer(PlayerControlLocks player) { if (IsBusy) Hold(player); }

        private IEnumerator Run(string scenePath, string arrivalId, PlayerControlLocks player, bool covered)
        {
            IsBusy = true;
            Hold(player);
            var pause = SimulationPause.Acquire();
            try
            {
                if (covered) SetCover(1f);
                else yield return Fade(0f, 1f, fadeOutSeconds);
                ArrivalId = arrivalId;
                var load = SceneLoader.LoadAsync(scenePath);
                while (load != null && !load.isDone) yield return null;
            }
            finally
            {
                ArrivalId = null;
                pause.Dispose();
            }
            yield return Fade(1f, 0f, fadeInSeconds);
            Hold(null);
            IsBusy = false;
        }

        private void Hold(PlayerControlLocks player)
        {
            if (lockedPlayer != null) lockedPlayer.Unlock(this);
            lockedPlayer = player;
            if (lockedPlayer != null) lockedPlayer.Lock(this);
        }

        private IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetCover(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            SetCover(to);
        }

        private void SetCover(float amount)
        {
            if (amount > 0f) EnsureCurtain();
            if (curtain == null) return;
            curtain.style.opacity = amount;
            curtain.style.display = amount > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnDisable()
        {
            Hold(null);
            SetCover(0f);
            IsBusy = false;
        }
    }
}
