using System;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Small command model: new menu actions do not require changing the view or input code.
    public sealed class PauseMenuAction
    {
        public string Label { get; }
        public string Description { get; }
        public bool Enabled { get; }
        public bool CloseOnExecute { get; }
        private readonly Action execute;
        public PauseMenuAction(string label, string description, Action execute, bool enabled = true, bool closeOnExecute = true)
        { Label = label; Description = description; this.execute = execute; Enabled = enabled; CloseOnExecute = closeOnExecute; }
        public void Execute() { if (Enabled) execute?.Invoke(); }
    }

    public static class PauseMenuCommands
    {
        // New Game lives on the title screen; settings have their own page (user, 4 Oct).
        public static PauseMenuAction[] CreateDefault(Action openSettings = null) => new[]
        {
            ReturnToRest(),
            new PauseMenuAction("Settings", "Display and HUD options.", openSettings, openSettings != null, closeOnExecute: false),
            new PauseMenuAction("Main Menu", "Save and return to the title.\nContinue resumes at your last rest.",
                () => GameSession.Instance?.ReturnToTitle(), GameSession.Instance != null && GameSession.Instance.CanReturnToTitle),
            new PauseMenuAction("Quit", Application.platform == RuntimePlatform.WebGLPlayer
                ? "Close the browser tab to quit." : "Leave the game.", Quit,
                Application.platform != RuntimePlatform.WebGLPlayer)
        };

        private static PauseMenuAction ReturnToRest()
        {
            bool checkpoint = GameSession.Instance != null && GameSession.Instance.Checkpoints.HasCheckpoint;
            return checkpoint
                ? new PauseMenuAction("Return to bonfire", "Return to your last rest.\nProgress is kept.", Restart)
                : new PauseMenuAction("Return to start", "Return to where you began.\nProgress is kept.", Restart);
        }

        private static void Restart()
        {
            if (GameSession.Instance != null) GameSession.Instance.Checkpoints.RestartFromCheckpoint();
            else SceneLoader.ReloadActive();
        }

        private static void Quit()
        {
            GameSession.Instance?.RecordResume();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // The settings page, shared by the pause menu and the title. Settings live outside the save slot
    // (PlayerPrefs), so they apply to every run. Each entry stays on the page; Back returns to the caller.
    public static class SettingsCommands
    {
        public static PauseMenuAction[] Create(Action back) => new[]
        {
            new PauseMenuAction(HudSettings.AlwaysShowVitals ? "Vitals: always shown" : "Vitals: when needed",
                HudSettings.AlwaysShowVitals ? "Show vitals only when needed." : "Show vitals all the time.",
                ToggleVitals, closeOnExecute: false),
            new PauseMenuAction("Back", "", back, closeOnExecute: false)
        };

        private static void ToggleVitals()
        {
            HudSettings.AlwaysShowVitals = !HudSettings.AlwaysShowVitals;
            HudNotifications.Post(HudSettings.AlwaysShowVitals ? "Vitals always shown" : "Vitals shown when needed");
        }
    }
}
