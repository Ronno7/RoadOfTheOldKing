using System;
using System.Linq;
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
        // An extra USS class for this row only (the upgrade fire's glowing Upgrades row).
        public string StyleClass { get; set; }
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
            new PauseMenuAction("Resume", "", null),
            new PauseMenuAction("Controls", "", openControls, openControls != null, closeOnExecute: false),
            new PauseMenuAction("Settings", "", openSettings, openSettings != null, closeOnExecute: false),
            new PauseMenuAction("Main Menu", "Save and return to the title.\nContinue resumes here.",
                () => GameSession.Instance?.ReturnToTitle(), GameSession.Instance != null && GameSession.Instance.CanReturnToTitle),
            new PauseMenuAction("Quit", "", Quit)
        };

        private static void Quit()
        {
            GameSession.Instance?.SaveOnExit();
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
        // The game has no music or ambience yet (user, 22 Nov: hide them until it does). The channels
        // and their saved levels stay; turn this on with the sound pass.
        private static readonly bool ShowMusicAndAmbience = false;

        public static OptionList.Row[] Create(Action back)
        {
            // Screen.fullScreen only reports a switch on the next frame, so the row keeps its own copy.
            bool full = Screen.fullScreen;
            var sound = new System.Collections.Generic.List<OptionList.Row>
            {
                new OptionList.Header("SOUND"),
                new OptionList.Slider("Master", "", () => SoundSettings.Volume, SoundSettings.SetVolume, 10, Percent)
            };
            if (ShowMusicAndAmbience)
            {
                sound.Add(Channel("Music", Audio.AudioChannel.Music));
                sound.Add(Channel("Ambience", Audio.AudioChannel.Ambience));
            }
            sound.Add(Channel("Effects", Audio.AudioChannel.Effects));
            return sound.Concat(new OptionList.Row[]
            {
                new OptionList.Header("DISPLAY"),
                new OptionList.Choice("Window", "", new[] { "Fullscreen", "Windowed" },
                    () => full ? 0 : 1, i => { full = i == 0; SetWindow(full); }),
                new OptionList.Choice("Atmosphere", "Light, haze and weather.", new[] { "Restrained", "Cinematic" },
                    () => AtmosphereSettings.Cinematic ? 1 : 0, i => AtmosphereSettings.Cinematic = i == 1),
                new OptionList.Header("COMFORT"),
                new OptionList.Slider("Screen flashes", "Hit flashes and the low health pulse.", () => FeedbackSettings.FlashScale, FeedbackSettings.SetFlash, 10, Percent),
                new OptionList.Slider("Screen shake", "", () => FeedbackSettings.ShakeScale, FeedbackSettings.SetShake, 10, Percent),
                new OptionList.Slider("Vibration", "Controller rumble.", () => RoadOfTheOldKing.Input.Rumble.Scale, RoadOfTheOldKing.Input.Rumble.SetScale, 10, Percent),
                new OptionList.Choice("Vitals", "Health, stamina and flasks in the corner.", new[] { "When needed", "Always" },
                    () => HudSettings.AlwaysShowVitals ? 1 : 0, i => HudSettings.AlwaysShowVitals = i == 1),
                new OptionList.Command("Back", "", back)
            }).ToArray();
        }

        private static string Percent(float value) => value <= .001f ? "Off" : Mathf.RoundToInt(value * 100f) + "%";

        private static OptionList.Row Channel(string label, Audio.AudioChannel channel) =>
            new OptionList.Slider(label, "", () => SoundSettings.ChannelVolume(channel), v => SoundSettings.SetChannelVolume(channel, v), 10, Percent);

        // Fullscreen fills the display at its own resolution (the first-launch default);
        // a window takes three quarters of it, so it never opens as large as the screen.
        private static void SetWindow(bool fullscreen)
        {
            var display = Screen.mainWindowDisplayInfo;
            int width = display.width > 0 ? display.width : Screen.currentResolution.width;
            int height = display.height > 0 ? display.height : Screen.currentResolution.height;
            if (fullscreen) Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
            else Screen.SetResolution(Mathf.RoundToInt(width * .75f), Mathf.RoundToInt(height * .75f), FullScreenMode.Windowed);
        }
    }
}
