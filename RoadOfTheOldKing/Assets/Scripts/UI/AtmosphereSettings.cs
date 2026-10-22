using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Player choice of region atmosphere (user, 21 Oct): Restrained by default, Cinematic as an option.
    // Stored with the other settings outside the save slot, so it applies to every run.
    public static class AtmosphereSettings
    {
        private const string CinematicKey = "RoadOfTheOldKing.Settings.CinematicAtmosphere";

        // RegionAtmosphere reads this each frame, so a change applies at once, including from the pause menu.
        public static bool Cinematic
        {
            get => PlayerPrefs.GetInt(CinematicKey, 0) == 1;
            set { PlayerPrefs.SetInt(CinematicKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }
}
