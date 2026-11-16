using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    public enum AttackWarningStyle { GhostSteps, Chalk }

    // Accessibility scales for screen effects: 1 = full, 0 = off. Every flash, vignette and camera kick reads the
    // runtime values; the settings page saves the player's choice (outside the save slot) and loads it at start.
    public static class FeedbackSettings
    {
        private const string FlashKey = "RoadOfTheOldKing.Settings.FlashScale", ShakeKey = "RoadOfTheOldKing.Settings.ShakeScale";
        public static float FlashScale = 1f;
        public static float ShakeScale = 1f;
        // How enemy attacks are marked on the ground. Chalk for now; Ghost Steps still needs refining, and F4 flips to it.
        public static AttackWarningStyle AttackWarnings = AttackWarningStyle.Chalk;

        public static void SetFlash(float scale) => FlashScale = Save(FlashKey, scale);
        public static void SetShake(float scale) => ShakeScale = Save(ShakeKey, scale);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            FlashScale = Mathf.Clamp01(PlayerPrefs.GetFloat(FlashKey, 1f));
            ShakeScale = Mathf.Clamp01(PlayerPrefs.GetFloat(ShakeKey, 1f));
        }

        private static float Save(string key, float scale)
        {
            scale = Mathf.Clamp01(scale);
            PlayerPrefs.SetFloat(key, scale);
            PlayerPrefs.Save();
            return scale;
        }
    }

    // Master volume, outside the save slot like the other settings.
    public static class SoundSettings
    {
        private const string VolumeKey = "RoadOfTheOldKing.Settings.Volume";
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));

        public static void SetVolume(float volume)
        {
            PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(volume));
            PlayerPrefs.Save();
            AudioListener.volume = Volume;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load() => AudioListener.volume = Volume;
    }
}
