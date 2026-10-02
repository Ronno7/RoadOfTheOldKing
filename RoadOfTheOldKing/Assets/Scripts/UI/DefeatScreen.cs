using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    // Defeat screen (UI Toolkit). Appears shortly after death so the fall reads first, says that
    // progress is kept, and returns to the last bonfire (or the start) through CheckpointSession.
    // R or the button confirms; duplicate reloads are blocked.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class DefeatScreen : MonoBehaviour, IModalMenu
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        [Tooltip("Seconds after death before the screen appears.")]
        [SerializeField, Min(0f)] private float showDelay = .9f;

        private Damageable health;
        private UIDocument document;
        private VisualElement overlay;
        private Label message;
        private MenuList list;
        private float showAt = -1f;
        private bool shown, restarting;

        private void Awake() => health = GetComponent<Damageable>();
        private void OnEnable() { if (health != null) health.Defeated += OnDefeated; }

        private void OnDisable()
        {
            if (health != null) health.Defeated -= OnDefeated;
            MenuStack.Remove(this);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }

        private void OnDefeated() => showAt = Time.unscaledTime + showDelay;

        private void Update()
        {
            if (!shown && showAt >= 0f && Time.unscaledTime >= showAt) Show();
            var keyboard = Keyboard.current;
            if (shown && keyboard != null && keyboard.rKey.wasPressedThisFrame) Return();
        }

        private void Show()
        {
            if (document == null)
            {
                if (layout == null || panelSettings == null) { Debug.LogError("Defeat screen needs its layout and panel settings.", this); return; }
                var child = new GameObject("Defeat screen UI"); child.SetActive(false); child.transform.SetParent(transform, false);
                document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
                child.SetActive(true);
                overlay = document.rootVisualElement.Q("defeat-overlay");
                message = overlay.Q<Label>("message");
                list = new MenuList(overlay.Q("actions"), overlay.Q<Label>("description"), action => action.Execute());
            }
            shown = true;
            bool checkpoint = CheckpointSession.Instance != null && CheckpointSession.Instance.HasCheckpoint;
            message.text = checkpoint ? "You will wake at your last bonfire. Everything you earned is kept."
                : "You will start again from the beginning. Everything you earned is kept.";
            overlay.style.display = DisplayStyle.Flex;
            MenuStack.Push(this);
            list.Show(new[]
            {
                new PauseMenuAction(checkpoint ? "Return to bonfire" : "Retry", checkpoint ? "Rest at your last fire." : "Back to the start.", Return)
            });
        }

        private void Return()
        {
            if (restarting) return;
            restarting = true;
            if (CheckpointSession.Instance != null) CheckpointSession.Instance.Respawn();
            else SceneReload.Active();
        }

        // Escape does nothing here; there is nowhere else to go.
        public void Back() { }
        public void Navigate(int direction) => list?.Navigate(direction);
        public void Submit() => list?.Submit();
    }
}
