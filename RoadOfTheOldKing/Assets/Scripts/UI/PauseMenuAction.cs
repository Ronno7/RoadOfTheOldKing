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
        // Shown dimmed but still selectable, to browse something not yet within reach (e.g. upgrades without shards).
        public bool Locked { get; }
        private readonly Action execute;
        public PauseMenuAction(string label, string description, Action execute, bool enabled = true, bool closeOnExecute = true, bool locked = false)
        { Label = label; Description = description; this.execute = execute; Enabled = enabled; CloseOnExecute = closeOnExecute; Locked = locked; }
        public void Execute() { if (Enabled) execute?.Invoke(); }
    }

    public static class PauseMenuCommands
    {
        // New Game lives on the title screen; settings have their own page (user, 4 Oct). Resume replaced
        // Return to bonfire (user, 17 Oct): death is the way back to the last rest.
        public static PauseMenuAction[] CreateDefault(Action openSettings = null) => new[]
        {
            new PauseMenuAction("Resume", "Back to the road.", null),
            new PauseMenuAction("Settings", "Display and HUD options.", openSettings, openSettings != null, closeOnExecute: false),
            new PauseMenuAction("Main Menu", "Save and return to the title.\nContinue resumes here.",
                () => GameSession.Instance?.ReturnToTitle(), GameSession.Instance != null && GameSession.Instance.CanReturnToTitle),
            new PauseMenuAction("Quit", Application.platform == RuntimePlatform.WebGLPlayer
                ? "Close the browser tab to quit." : "Leave the game.", Quit,
                Application.platform != RuntimePlatform.WebGLPlayer)
        };

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
            new PauseMenuAction(AtmosphereSettings.Cinematic ? "Atmosphere: cinematic" : "Atmosphere: restrained",
                AtmosphereSettings.Cinematic ? "Return to the quieter, pixel-crisp atmosphere." : "Stronger light, haze and weather.",
                ToggleAtmosphere, closeOnExecute: false),
            new PauseMenuAction("Back", "", back, closeOnExecute: false)
        };

        private static void ToggleAtmosphere()
        {
            AtmosphereSettings.Cinematic = !AtmosphereSettings.Cinematic;
            HudNotifications.Post(AtmosphereSettings.Cinematic ? "Cinematic atmosphere" : "Restrained atmosphere");
        }

        private static void ToggleVitals()
        {
            HudSettings.AlwaysShowVitals = !HudSettings.AlwaysShowVitals;
            HudNotifications.Post(HudSettings.AlwaysShowVitals ? "Vitals always shown" : "Vitals shown when needed");
        }
    }
}
