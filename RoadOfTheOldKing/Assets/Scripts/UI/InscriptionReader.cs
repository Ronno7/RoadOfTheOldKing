using System;
using System.Collections;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A brief, rereadable modal. Uses the same menu input, pause and control leases as other
    // menus; it neither heals nor changes the checkpoint. No hidden-site checklist.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth), typeof(PlayerControlLocks))]
    public sealed class InscriptionReader : MonoBehaviour, IModalMenu
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        private PlayerHealth player;
        private PlayerCombatController combat;
        private PlayerControlLocks locks;
        private UIDocument document;
        private VisualElement overlay;
        private Label heading, body, knowledge;
        private Button closeButton;
        private ForgeInscription source;
        private IDisposable pause;
        private Coroutine restoring;

        public bool IsOpen => pause != null;
        public bool CanOpen => isActiveAndEnabled && !IsOpen && restoring == null &&
            player != null && player.IsAlive && locks != null && !locks.IsLocked &&
            !MenuStack.IsAnyOpen && Time.timeScale > 0f && combat != null &&
            combat.CanStartAttack && !combat.IsAttacking && combat.Weapon?.IsAway != true;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            combat = GetComponent<PlayerCombatController>();
            locks = GetComponent<PlayerControlLocks>();
        }

        private bool Build()
        {
            if (document != null) return true;
            if (layout == null || panelSettings == null)
            { Debug.LogError("Inscription reader needs its layout and panel settings.", this); return false; }
            var child = new GameObject("Inscription UI");
            child.SetActive(false);
            child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;
            child.SetActive(true);
            overlay = document.rootVisualElement.Q("inscription-overlay");
            heading = overlay.Q<Label>("heading");
            body = overlay.Q<Label>("body");
            knowledge = overlay.Q<Label>("knowledge");
            closeButton = overlay.Q<Button>("close");
            closeButton.clicked += Close;
            return true;
        }

        public bool TryOpen(ForgeInscription inscription)
        {
            if (!CanOpen || inscription == null || !inscription.CanCollect(player) || !Build()) return false;
            var session = GameSession.Instance;
            bool known = ForgeInscriptionProgression.Knows(session.Progress, inscription.SiteNumber);
            if (!known && !session.Rewards.TryLearnInscription(inscription, player)) return false;
            source = inscription;
            heading.text = inscription.Heading;
            body.text = inscription.Text;
            int count = ForgeInscriptionProgression.Count(session.Progress);
            knowledge.text = (known ? "Remembered" : "Knowledge gained") + " · " + count +
                (count == 1 ? " inscription" : " inscriptions");
            overlay.style.display = DisplayStyle.Flex;
            locks.Lock(this);
            pause = SimulationPause.Acquire();
            MenuStack.Push(this);
            closeButton.Focus();
            return true;
        }

        private void Update()
        {
            if (IsOpen && (player == null || !player.IsAlive || source == null ||
                !source.isActiveAndEnabled || GameSession.Instance == null || GameSession.Instance.IsLoading))
                Close();
        }

        public void Back() => Close();
        public void Navigate(int direction) { if (IsOpen) closeButton.Focus(); }
        public void Submit() => Close();

        public void Close()
        {
            if (!IsOpen) return;
            Hide();
            // A mouse click or closing key must not become an attack in the same frame.
            restoring = StartCoroutine(UnlockNextFrame());
        }

        private IEnumerator UnlockNextFrame()
        {
            yield return null;
            if (locks != null) locks.Unlock(this);
            restoring = null;
        }

        private void Hide()
        {
            MenuStack.Remove(this);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
            pause?.Dispose();
            pause = null;
            source = null;
        }

        private void OnDisable()
        {
            if (restoring != null) { StopCoroutine(restoring); restoring = null; }
            Hide();
            if (locks != null) locks.Unlock(this);
        }

        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
