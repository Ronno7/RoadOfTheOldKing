using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Player HUD preferences, kept in PlayerPrefs apart from the save slot (a new run keeps them).
    // AlwaysShowVitals: health, stamina and flask meters stay in the corner instead of appearing
    // only when relevant (stamina otherwise shows as the arc over the hero during combat).
    public static class HudSettings
    {
        private const string AlwaysShowVitalsKey = "RoadOfTheOldKing.Settings.AlwaysShowVitals";

        public static bool AlwaysShowVitals
        {
            get => PlayerPrefs.GetInt(AlwaysShowVitalsKey, 0) == 1;
            set { PlayerPrefs.SetInt(AlwaysShowVitalsKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
