using System;
using System.Collections;
using System.Collections.Generic;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A conversation (user, 17 Oct: more engaging than a popup shop): lines type out in a low kit box while the
    // world keeps moving and the camera leans in a little, then optional choices. A/Enter finishes a line, then
    // turns it; B/Escape finishes, skips ahead, or leaves at the choices. The speaker owns the content and calls
    // Open/Say; choices run their own actions. Holds a control lease like the other menus.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class DialogueBox : MonoBehaviour, IModalMenu
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        [Tooltip("Typing speed of a line, in characters per second (real time).")]
        [SerializeField, Min(1f)] private float charactersPerSecond = 48f;
        [Tooltip("Camera size while talking (half the visible height).")]
        [SerializeField, Min(.5f)] private float closeUpSize = 4.75f;

        private PlayerHealth player;
        private PlayerControlLocks locks;
        private UIDocument document;
        private VisualElement overlay, choicesRoot;
        private Label speaker, line, hint;
        private MenuList list;
        private readonly List<string> lines = new List<string>();
        private IReadOnlyList<PauseMenuAction> choices = Array.Empty<PauseMenuAction>();
        private int lineIndex;
        private float lineStartedAt;
        private Action closed;
        private Coroutine releasing;
        private int hintState = -1;
        // The last line/count written to the label: the partial string is rebuilt only when either changes.
        private string shownLine;
        private int shownCount = -1;

        public bool IsOpen { get; private set; }
        public bool CanOpen => isActiveAndEnabled && !IsOpen && releasing == null && player != null && player.IsAlive &&
            locks != null && !locks.IsLocked && !MenuStack.IsAnyOpen && Time.timeScale > 0 &&
            GameSession.Instance != null && !GameSession.Instance.IsLoading;
        private bool Typing => lineIndex < lines.Count && Visible(lines[lineIndex]) < lines[lineIndex].Length;
        private bool AtChoices => lineIndex >= lines.Count - 1 && !Typing && choices.Count > 0;

        private void Awake() { player = GetComponent<PlayerHealth>(); locks = GetComponent<PlayerControlLocks>(); }

        private bool Build()
        {
            if (document != null) return true;
            if (layout == null || panelSettings == null) { Debug.LogError("Dialogue box needs its layout and panel settings.", this); return false; }
            var child = new GameObject("Dialogue UI"); child.SetActive(false); child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
            child.SetActive(true);
            overlay = document.rootVisualElement.Q("dialogue-overlay");
            speaker = overlay.Q<Label>("speaker"); line = overlay.Q<Label>("line"); hint = overlay.Q<Label>("dialogue-hint");
            choicesRoot = overlay.Q("choices");
            list = new MenuList(choicesRoot, null, action => { if (IsOpen) action.Execute(); });
            // A click on the box turns the line like A/Enter; choices take their own clicks.
            overlay.Q("dialogue-panel").RegisterCallback<PointerDownEvent>(e => { if (!AtChoices) Submit(); });
            return true;
        }

        // Starts a conversation; false when another menu, a lease or loading is in the way.
        public bool Open(string speakerName, IReadOnlyList<string> say, IReadOnlyList<PauseMenuAction> then = null, Action onClosed = null)
        {
            if (!CanOpen || !Build()) return false;
            IsOpen = true; closed = onClosed;
            speaker.text = speakerName ?? "";
            SetVisible(speaker, !string.IsNullOrEmpty(speakerName));
            overlay.style.display = DisplayStyle.Flex;
            locks.Lock(this); MenuStack.Push(this);
            var zoom = Camera.main != null ? Camera.main.GetComponent<CameraZoom2D>() : null;
            if (zoom != null) zoom.SetOverride(this, closeUpSize);
            Say(say, then);
            return true;
        }

        // Continues the open conversation with new lines and choices (e.g. after a question).
        public void Say(IReadOnlyList<string> say, IReadOnlyList<PauseMenuAction> then = null)
        {
            if (!IsOpen) return;
            lines.Clear();
            if (say != null) foreach (var text in say) if (!string.IsNullOrEmpty(text)) lines.Add(text);
            if (lines.Count == 0) lines.Add("");
            choices = then ?? Array.Empty<PauseMenuAction>();
            lineIndex = 0;
            BeginLine();
        }

        // handOff: another menu opens in the same moment (the shop), so the lease is released now, not next frame.
        public void Close(bool handOff = false)
        {
            if (!IsOpen) return;
            Hide();
            if (handOff) locks?.Unlock(this);
            else releasing = StartCoroutine(UnlockNextFrame());
            var done = closed; closed = null;
            done?.Invoke();
        }

        private void Hide()
        {
            IsOpen = false;
            MenuStack.Remove(this);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
            var zoom = Camera.main != null ? Camera.main.GetComponent<CameraZoom2D>() : null;
            if (zoom != null) zoom.ClearOverride(this);
        }

        private IEnumerator UnlockNextFrame() { yield return null; locks?.Unlock(this); releasing = null; }

        private void OnDisable()
        {
            if (releasing != null) { StopCoroutine(releasing); releasing = null; }
            if (IsOpen) { Hide(); var done = closed; closed = null; done?.Invoke(); }
            locks?.Unlock(this);
        }

        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }

        private void BeginLine()
        {
            lineStartedAt = Time.unscaledTime;
            hintState = -1;
            shownCount = -1;
            SetVisible(choicesRoot, false);
            list.Show(Array.Empty<PauseMenuAction>());
            Refresh();
        }

        // Clamped as a float first: a finished line starts at negative infinity, which must not overflow to a negative count.
        private int Visible(string text)
        {
            float typed = (Time.unscaledTime - lineStartedAt) * charactersPerSecond;
            return typed >= text.Length ? text.Length : Mathf.Max(0, Mathf.FloorToInt(typed));
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (player == null || !player.IsAlive || GameSession.Instance == null || GameSession.Instance.IsLoading) { Close(); return; }
            Refresh();
        }

        // The hidden rest of the line is laid out but transparent, so words never jump to the next row as they type.
        private void Refresh()
        {
            string text = lineIndex < lines.Count ? lines[lineIndex] : "";
            int shown = Visible(text);
            if (shown != shownCount || !ReferenceEquals(text, shownLine))
            {
                line.text = shown >= text.Length ? text : text.Substring(0, shown) + "<alpha=#00>" + text.Substring(shown);
                shownLine = text; shownCount = shown;
            }
            bool showChoices = AtChoices;
            if (showChoices && choicesRoot.resolvedStyle.display == DisplayStyle.None)
            {
                list.Show(choices);
                SetVisible(choicesRoot, true);
            }
            int state = showChoices ? 2 : shown >= text.Length ? 1 : 0;
            if (state == hintState) return;
            hintState = state;
            if (state == 2) ControlLabels.Bind(hint, "↑↓ Choose · Enter Select", "D-pad Choose · A Select");
            else if (state == 1) ControlLabels.Bind(hint, "Enter Next", "A Next");
            else ControlLabels.Bind(hint, "", "");
        }

        private void FinishLine() => lineStartedAt = float.NegativeInfinity;

        public void Submit()
        {
            if (!IsOpen) return;
            if (Typing) { FinishLine(); Refresh(); return; }
            if (lineIndex < lines.Count - 1) { lineIndex++; BeginLine(); return; }
            if (choices.Count > 0) { list.Submit(); return; }
            Close();
        }

        public void Back()
        {
            if (!IsOpen) return;
            if (Typing) { FinishLine(); Refresh(); return; }
            if (lineIndex < lines.Count - 1) { lineIndex = lines.Count - 1; BeginLine(); FinishLine(); Refresh(); return; }
            Close();
        }

        public void Navigate(int direction) { if (IsOpen && AtChoices) list.Navigate(direction); }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
