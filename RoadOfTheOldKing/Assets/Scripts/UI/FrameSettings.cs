using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // V-Sync and a frame rate limit, as PC games lay them out (user, 22 Nov: "settings for unlocking or setting framerate
    // like other games"; 23 Nov: no preset list, "just follow other games standard settings layout"). Kept in
    // PlayerPrefs apart from the save slot. V-Sync on (the default) paces frames to the display and the limit waits;
    // off, the limit caps the frame rate, or nothing does when it's Unlimited.
    public static class FrameSettings
    {
        public const int MinLimit = 30, MaxLimit = 360;
        private const string VSyncKey = "RoadOfTheOldKing.Settings.VSync";
        private const string LimitKey = "RoadOfTheOldKing.Settings.FrameLimit";

        public static bool VSync
        {
            get => PlayerPrefs.GetInt(VSyncKey, 1) == 1;
            set { PlayerPrefs.SetInt(VSyncKey, value ? 1 : 0); PlayerPrefs.Save(); Apply(); }
        }

        // Frames a second, or 0 for Unlimited.
        public static int Limit
        {
            get { int limit = PlayerPrefs.GetInt(LimitKey, 0); return limit <= 0 ? 0 : Mathf.Clamp(limit, MinLimit, MaxLimit); }
            set { PlayerPrefs.SetInt(LimitKey, value <= 0 ? 0 : Mathf.Clamp(value, MinLimit, MaxLimit)); PlayerPrefs.Save(); Apply(); }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            // In the editor the Game view owns V-Sync, and a play-time QualitySettings change would be saved into the
            // project's quality levels; only builds set it.
#if !UNITY_EDITOR
            QualitySettings.vSyncCount = VSync ? 1 : 0;
#endif
            Application.targetFrameRate = VSync || Limit <= 0 ? -1 : Limit;
        }
    }
}
