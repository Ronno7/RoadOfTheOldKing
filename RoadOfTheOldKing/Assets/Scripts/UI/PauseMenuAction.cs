using System;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.UI
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
        public static PauseMenuAction[] CreateDefault(Action newGame = null) => new[]
        {
            new PauseMenuAction("Restart", "Return to your last rest.\nProgress is kept.", Restart),
            new PauseMenuAction(HudSettings.AlwaysShowVitals ? "Vitals: always shown" : "Vitals: when needed",
                HudSettings.AlwaysShowVitals ? "Show vitals only when needed." : "Show vitals all the time.",
                ToggleVitals, closeOnExecute: false),
            new PauseMenuAction("New Game", "Start over with a new save.", newGame, newGame != null, closeOnExecute: false),
            new PauseMenuAction("Quit", Application.platform == RuntimePlatform.WebGLPlayer
                ? "Close the browser tab to quit." : "Leave the game.", Quit,
                Application.platform != RuntimePlatform.WebGLPlayer)
        };

        private static void Restart()
        {
            if (CheckpointSession.Instance != null) CheckpointSession.Instance.RestartFromCheckpoint();
            else SceneReload.Active();
        }

        private static void ToggleVitals()
        {
            HudSettings.AlwaysShowVitals = !HudSettings.AlwaysShowVitals;
            HudNotifications.Post(HudSettings.AlwaysShowVitals ? "Vitals always shown" : "Vitals shown when needed");
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
