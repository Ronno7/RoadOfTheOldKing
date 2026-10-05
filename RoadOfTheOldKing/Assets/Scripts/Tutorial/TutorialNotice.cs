using RoadOfTheOldKing.UI;

namespace RoadOfTheOldKing.Tutorial
{
    // Lesson/unlock messages. Kept as the Tutorial-facing entry point; presentation is the HUD's
    // receipt channel (HudNotifications), so callers never depend on a particular UI.
    public static class TutorialNotice
    {
        public static void Show(string text, float seconds = 5f) => HudNotifications.Post(ControlLabels.Format(text), seconds);
    }
}
