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
        private readonly Action execute;
        public PauseMenuAction(string label, string description, Action execute, bool enabled = true)
        { Label = label; Description = description; this.execute = execute; Enabled = enabled; }
        public void Execute() { if (Enabled) execute?.Invoke(); }
    }

    public static class PauseMenuCommands
    {
        public static PauseMenuAction[] CreateDefault() => new[]
        {
            new PauseMenuAction("Restart", "Return to your last bonfire or the start. Permanent progress is kept.", Restart),
            new PauseMenuAction("Quit", Application.platform == RuntimePlatform.WebGLPlayer
                ? "Close the browser tab to quit." : "Leave the game.", Quit,
                Application.platform != RuntimePlatform.WebGLPlayer)
        };

        private static void Restart()
        {
            if (CheckpointSession.Instance != null) CheckpointSession.Instance.RestartFromCheckpoint();
            else SceneReload.Active();
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
