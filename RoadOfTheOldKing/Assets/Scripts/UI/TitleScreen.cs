using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Title screen (foundation): Continue resumes at the last rest, New Game starts over (confirmed when a
    // save exists), Settings opens the shared settings page, Quit leaves (disabled in the browser). Presentation only; GameSession loads the scenes.
    // Keyboard input comes from the PauseMenuInput beside it; Escape steps back from the confirmation.
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument), typeof(PauseMenuInput))]
    public sealed class TitleScreen : MonoBehaviour, IModalMenu
    {
        private PauseMenuInput input;
        private MenuList list;
        private enum Page { Main, Confirm, Settings }
        private Page page;
        private PauseMenuAction[] current = new PauseMenuAction[0];

        private void OnEnable()
        {
            input = GetComponent<PauseMenuInput>();
            var root = GetComponent<UIDocument>().rootVisualElement;
            list = new MenuList(root.Q("actions"), root.Q<Label>("description"), Run);
            var version = root.Q<Label>("version");
            if (version != null) version.text = "v" + Application.version;
            input.ToggleRequested += Back;
            input.NavigationRequested += Navigate;
            input.SubmitRequested += Submit;
            MenuStack.Push(this);
            ShowMain();
        }

        private void OnDisable()
        {
            input.ToggleRequested -= Back;
            input.NavigationRequested -= Navigate;
            input.SubmitRequested -= Submit;
            MenuStack.Remove(this);
        }

        private static GameSession Session => GameSession.Instance;
        private static bool HasSave => Session != null && Session.HasSave;

        // Rows of the main page that other pages return to.
        private const int NewGameRow = 1, SettingsRow = 2;

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
            if (!HasSave) { Session.StartNewRun(); return; }
            Show(Page.Confirm, new[]
            {
                new PauseMenuAction("Keep my save", "Go back.", () => ShowMain(NewGameRow)),
                new PauseMenuAction("Erase and begin", "This erases your save.", () => Session.StartNewRun())
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
                ShowSettings(System.Array.IndexOf(current, action));
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Back()
        {
            if (page == Page.Confirm) ShowMain(NewGameRow);
            else if (page == Page.Settings) ShowMain(SettingsRow);
        }
        public void Navigate(int direction) { if (MenuStack.Top == (IModalMenu)this) list.Navigate(direction); }
        public void Submit() { if (MenuStack.Top == (IModalMenu)this && Session != null && !Session.IsLoading) list.Submit(); }
    }
}
