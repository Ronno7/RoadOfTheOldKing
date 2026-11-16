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
        public static PauseMenuAction[] CreateDefault(Action openSettings = null, Action openControls = null) => new[]
        {
            new PauseMenuAction("Resume", "Back to the road.", null),
            new PauseMenuAction("Controls", "Keyboard and controller.", openControls, openControls != null, closeOnExecute: false),
            new PauseMenuAction("Settings", "Sound, display and HUD options.", openSettings, openSettings != null, closeOnExecute: false),
            new PauseMenuAction("Main Menu", "Save and return to the title.\nContinue resumes here.",
                () => GameSession.Instance?.ReturnToTitle(), GameSession.Instance != null && GameSession.Instance.CanReturnToTitle),
            new PauseMenuAction("Quit", "Leave the game.", Quit)
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

    // The settings page, shared by the pause menu and the title: grouped rows with sliders and choices (user, 15 Nov).
    // Settings live outside the save slot (PlayerPrefs), so they apply to every run.
    public static class SettingsOptions
    {
        public static OptionList.Row[] Create(Action back)
        {
            // Screen.fullScreen only reports a switch on the next frame, so the row keeps its own copy.
            bool full = Screen.fullScreen;
            return new OptionList.Row[]
            {
                new OptionList.Header("SOUND"),
                new OptionList.Slider("Volume", "All game sound.", () => SoundSettings.Volume, SoundSettings.SetVolume, 10, Percent),
                new OptionList.Header("DISPLAY"),
                new OptionList.Choice("Window", "Fill the screen or play in a window.", new[] { "Fullscreen", "Windowed" },
                    () => full ? 0 : 1, i => { full = i == 0; Screen.fullScreenMode = full ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed; }),
                new OptionList.Choice("Atmosphere", "Light, haze and weather: quiet and crisp, or stronger.", new[] { "Restrained", "Cinematic" },
                    () => AtmosphereSettings.Cinematic ? 1 : 0, i => AtmosphereSettings.Cinematic = i == 1),
                new OptionList.Header("COMFORT"),
                new OptionList.Slider("Screen flashes", "Hit flashes and the low health pulse.", () => FeedbackSettings.FlashScale, FeedbackSettings.SetFlash, 10, Percent),
                new OptionList.Slider("Screen shake", "Camera kicks and shakes.", () => FeedbackSettings.ShakeScale, FeedbackSettings.SetShake, 10, Percent),
                new OptionList.Choice("Vitals", "Health, stamina and flasks in the corner.", new[] { "When needed", "Always" },
                    () => HudSettings.AlwaysShowVitals ? 1 : 0, i => HudSettings.AlwaysShowVitals = i == 1),
                new OptionList.Command("Back", "", back)
            };
        }

        private static string Percent(float value) => value <= .001f ? "Off" : Mathf.RoundToInt(value * 100f) + "%";
    }
}
