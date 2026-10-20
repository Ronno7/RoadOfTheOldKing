using System;
using System.Text.RegularExpressions;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Title screen (foundation): Continue resumes at the last rest, New Game starts over (confirmed when a
    // save exists), Settings opens the shared settings page, Quit leaves (disabled in the browser). Presentation only; GameSession loads the scenes.
    // Keyboard/controller input comes from the PauseMenuInput beside it; Escape or B steps back from the confirmation.
    // Opening (user, 17 Oct): when the game boots into this scene, the logo waits for any button before the menu
    // appears. New Game plays the text prologue (pages from a TextAsset) before the run starts; Escape/Start skips it.
    // The prologue touches no save: an existing one is only erased when the run actually starts.
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument), typeof(PauseMenuInput))]
    public sealed class TitleScreen : MonoBehaviour, IModalMenu
    {
        [Tooltip("Prologue pages: paragraphs separated by blank lines; lines starting with # are ignored.")]
        [SerializeField] private TextAsset prologue;
        [Tooltip("Seconds for a prologue page to fade in (and for the last one to fade out).")]
        [SerializeField, Min(.05f)] private float pageFade = .8f;

        private PauseMenuInput input;
        private MenuList list;
        private enum Page { Press, Main, Confirm, Settings, Prologue }
        private Page page;
        private PauseMenuAction[] current = new PauseMenuAction[0];
        private VisualElement panel, prologueRoot;
        private Label press, prologueText;
        private IDisposable anyButton;
        private int revealedFrame = -1;
        private string[] pages = new string[0];
        private int pageIndex;
        private float pageShownAt, leavingAt = -1f;
        private bool runStarted;

        // The "press any button" gate shows once, and only when the game started in this scene (not on a return
        // to the title, nor in a fixture that boots elsewhere).
        private static string bootScene;
        private static bool pressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { bootScene = null; pressed = false; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RecordBootScene() => bootScene = SceneManager.GetActiveScene().path;

        private void OnEnable()
        {
            input = GetComponent<PauseMenuInput>();
            var root = GetComponent<UIDocument>().rootVisualElement;
            list = new MenuList(root.Q("actions"), root.Q<Label>("description"), Run);
            panel = root.Q("title-panel");
            press = root.Q<Label>("press");
            prologueRoot = root.Q("prologue");
            prologueText = root.Q<Label>("prologue-text");
            // The prologue covers the menu and takes clicks too: a click turns the page like A/Enter.
            prologueRoot.RegisterCallback<PointerDownEvent>(_ => { if (page == Page.Prologue) AdvancePrologue(); });
            ControlLabels.Bind(root.Q<Label>("hint"), "↑↓ Choose · Enter Select", "D-pad Choose · A Select");
            ControlLabels.Bind(press, "Press Enter", "Press A");
            ControlLabels.Bind(root.Q<Label>("prologue-hint"), "Enter Next · Esc Skip", "A Next · Start Skip");
            var version = root.Q<Label>("version");
            if (version != null) version.text = "v" + Application.version;
            input.ToggleRequested += OnToggle;
            input.BackRequested += Back;
            input.NavigationRequested += Navigate;
            input.SubmitRequested += Submit;
            MenuStack.Push(this);
            if (!pressed && bootScene == gameObject.scene.path) ShowPress();
            else ShowMain();
        }

        private void OnDisable()
        {
            input.ToggleRequested -= OnToggle;
            input.BackRequested -= Back;
            input.NavigationRequested -= Navigate;
            input.SubmitRequested -= Submit;
            anyButton?.Dispose(); anyButton = null;
            MenuStack.Remove(this);
        }

        private static GameSession Session => GameSession.Instance;
        private static bool HasSave => Session != null && Session.HasSave;

        // Rows of the main page that other pages return to.
        private const int NewGameRow = 1, SettingsRow = 2;

        private void ShowPress()
        {
            ShowMain(); // build the rows now, so the hidden panel already has its final height
            page = Page.Press;
            // Hidden but still laid out, so the logo stays put when the menu appears.
            panel.style.visibility = Visibility.Hidden; SetVisible(press, true);
            anyButton = InputSystem.onAnyButtonPress.CallOnce(_ => Reveal());
        }

        // The button that reveals the menu must not also choose from it this frame (PauseMenuInput reads it too).
        private void Reveal()
        {
            if (page != Page.Press) return;
            pressed = true; revealedFrame = Time.frameCount;
            anyButton?.Dispose(); anyButton = null;
            SetVisible(press, false); panel.style.visibility = Visibility.Visible;
            ShowMain();
        }

        private void ShowMain(int select = -1)
        {
            Show(Page.Main, new[]
            {
                new PauseMenuAction("Continue", Session != null && Session.HasResumePoint ? "Resume where you left off." : "Resume at your last rest.",
                    () => Session.Continue(), HasSave),
                new PauseMenuAction("New Game", "Begin a new journey.", NewGame, Session != null),
                new PauseMenuAction("Settings", "Display and HUD options.", () => ShowSettings(0)),
                new PauseMenuAction("Quit", Application.platform == RuntimePlatform.WebGLPlayer
                    ? "Close the browser tab to quit." : "Leave the game.", Quit,
                    Application.platform != RuntimePlatform.WebGLPlayer)
            }, select >= 0 ? select : HasSave ? 0 : NewGameRow);
        }

        private void NewGame()
        {
            if (!HasSave) { BeginPrologue(); return; }
            Show(Page.Confirm, new[]
            {
                new PauseMenuAction("Keep my save", "Go back.", () => ShowMain(NewGameRow)),
                new PauseMenuAction("Erase and begin", "This erases your save.", BeginPrologue)
            }, 0);
        }

        // Rebuilt per show so toggles name their current state.
        private void ShowSettings(int select) =>
            Show(Page.Settings, SettingsCommands.Create(() => ShowMain(SettingsRow)), select);

        private void Show(Page next, PauseMenuAction[] actions, int select)
        {
            page = next;
            current = actions;
            list.Show(actions, select);
        }

        // A settings toggle stays on its row; anything that changed page already rebuilt the list.
        private void Run(PauseMenuAction action)
        {
            var shown = current;
            action.Execute();
            if (page == Page.Settings && ReferenceEquals(shown, current))
                ShowSettings(Array.IndexOf(current, action));
        }

        private void BeginPrologue()
        {
            pages = Pages(prologue);
            if (pages.Length == 0) { StartRun(); return; }
            page = Page.Prologue;
            pageIndex = 0; leavingAt = -1f;
            SetVisible(prologueRoot, true);
            ShowPage();
        }

        private void ShowPage()
        {
            prologueText.text = pages[pageIndex];
            prologueText.style.opacity = 0f;
            pageShownAt = Time.unscaledTime;
        }

        // A/Enter first completes a fading page, then turns it; after the last page the text fades and the run starts.
        private void AdvancePrologue()
        {
            if (leavingAt >= 0f) return;
            // Completing a fade marks the page as shown long ago, so a second press always turns it.
            if (Time.unscaledTime - pageShownAt < pageFade) { pageShownAt = float.NegativeInfinity; return; }
            if (pageIndex < pages.Length - 1) { pageIndex++; ShowPage(); }
            else leavingAt = Time.unscaledTime;
        }

        private void Update()
        {
            if (page == Page.Press)
            {
                // A slow blink, as on old title screens.
                press.style.opacity = Mathf.Repeat(Time.unscaledTime, 1.2f) < .8f ? 1f : 0f;
                return;
            }
            if (page != Page.Prologue) return;
            if (leavingAt >= 0f)
            {
                float t = (Time.unscaledTime - leavingAt) / pageFade;
                prologueText.style.opacity = Mathf.Clamp01(1f - t);
                if (t >= 1f) StartRun();
                return;
            }
            prologueText.style.opacity = Mathf.Clamp01((Time.unscaledTime - pageShownAt) / pageFade);
        }

        private void StartRun()
        {
            if (runStarted || Session == null) return;
            runStarted = true;
            Session.StartNewRun();
        }

        // Pages are paragraphs; '#' lines are notes for writers, and line breaks inside a page are only wrapping.
        private static string[] Pages(TextAsset source)
        {
            if (source == null) return new string[0];
            var kept = Regex.Replace(source.text.Replace("\r\n", "\n"), @"^#.*\n?", "", RegexOptions.Multiline);
            var blocks = Regex.Split(kept.Trim(), @"\n\s*\n");
            var result = new System.Collections.Generic.List<string>();
            foreach (var block in blocks)
            {
                string text = Regex.Replace(block.Trim(), @"\s*\n\s*", " ");
                if (text.Length > 0) result.Add(text);
            }
            return result.ToArray();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Escape/Start skips the prologue; elsewhere it steps back like B.
        private void OnToggle()
        {
            if (page == Page.Prologue) { if (leavingAt < 0f) leavingAt = Time.unscaledTime; }
            else Back();
        }

        public void Back()
        {
            if (page == Page.Confirm) ShowMain(NewGameRow);
            else if (page == Page.Settings) ShowMain(SettingsRow);
        }
        public void Navigate(int direction)
        {
            if (MenuStack.Top != (IModalMenu)this || Time.frameCount == revealedFrame) return;
            if (page != Page.Press && page != Page.Prologue) list.Navigate(direction);
        }
        public void Submit()
        {
            if (MenuStack.Top != (IModalMenu)this || Session == null || Session.IsLoading || Time.frameCount == revealedFrame) return;
            if (page == Page.Prologue) AdvancePrologue();
            else if (page != Page.Press) list.Submit();
        }
    }
}
