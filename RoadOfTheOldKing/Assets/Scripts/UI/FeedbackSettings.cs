namespace TheLostShrine.UI
{
    // Accessibility scales for screen effects: 1 = full, 0 = off. A future settings menu drives these
    // (the UI proposal's reduced flash/shake option); every flash, vignette and camera kick reads them.
    public static class FeedbackSettings
    {
        public static float FlashScale = 1f;
        public static float ShakeScale = 1f;
    }
}
