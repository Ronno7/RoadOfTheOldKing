using System;

namespace RoadOfTheOldKing.Progression
{
    // Which save a build reads and writes. Every build profile shares one PlayerPrefs store on a Windows account (same
    // company and product), so public demos (Steam, itch.io) keep their own progress and resume keys and never migrate
    // older keys: a demo New Game can't erase a playtest run, and a demo can't Continue one (18 Nov). Settings stay shared.
    // Only in a real build: the editor compiles with whichever build profile was last active, and its working save
    // must not switch because of that.
    public static class SaveProfile
    {
        public const string DemoSuffix = ".Demo";

#if (ROTOK_DEMO || ROTOK_STEAM_DEMO) && !UNITY_EDITOR
        public const bool IsDemo = true;
#else
        public const bool IsDemo = false;
#endif

        public static string Key(string authored, bool demo) => demo ? authored + DemoSuffix : authored;
        public static string[] LegacyKeys(string[] authored, bool demo) => demo ? Array.Empty<string>() : authored ?? Array.Empty<string>();
    }
}
