using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Tutorial
{
    // A sign read like a Souls message (user, 23 Nov): walk up, press interact, and its lesson shows in a small box at the
    // bottom of the screen, controls drawn as glyphs. The world keeps running; interact again or walk away to close it.
    // Rereadable in any order; it changes nothing.
    [DisallowMultipleComponent]
    public sealed class LessonSign : WorldPickup
    {
        [SerializeField, TextArea(2, 6), Tooltip("{tokens} become control glyphs, e.g. {throw}, {sprint}.")]
        private string text;
        [SerializeField, Min(.5f)] private float reach = 1.4f;
        [Tooltip("Units past the reach at which an open sign closes as the hero walks off.")]
        [SerializeField, Min(0f)] private float closeMargin = .75f;

        private static LessonSign open;
        private PlayerHealth reader;

        public string Text => text;
        public bool IsOpen => open == this;
        public static LessonSign Open => open;
        // Nothing to prompt while it is open: the same press closes it.
        public override string Prompt => IsOpen ? "" : "Read sign";
        protected override float PickupDistance => reach;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpen() => open = null;

        public override bool CanCollect(PlayerHealth player)
        {
            var session = GameSession.Instance;
            return base.CanCollect(player) && !string.IsNullOrWhiteSpace(text) && session != null && !session.IsLoading;
        }

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            if (IsOpen) { Close(); return true; }
            if (open != null) open.Close();
            open = this;
            reader = player;
            HudNotifications.ShowSign(text);
            return true;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (reader == null || !reader.IsAlive ||
                Vector2.Distance(reader.transform.position, transform.position) > reach + closeMargin)
                Close();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (IsOpen) Close();
        }

        private void Close()
        {
            if (open == this) { open = null; HudNotifications.ClearSign(); }
            reader = null;
        }
    }
}
