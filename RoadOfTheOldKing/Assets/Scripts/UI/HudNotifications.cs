using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Event-driven text channels for the HUD. Gameplay posts; the HUD presents. Nothing here
    // reads or writes gameplay state.
    //  Receipts: short, queued confirmations (rewards, unlocks, lessons); bounded, oldest dropped.
    //  Hint: the single current teaching hint owned by the Tutorial guide.
    //  Announcements: place names and milestone events in one queue, shown one at a time in one place and motion (user
    //  approved, 17 Nov): a region's name largest, a landmark's or an event's ("BONFIRE LIT") a size down.
    //  Speech: one short line spoken by someone in the world, shown over their head (the trader's barks).
    public enum AnnouncementTier { Region, Landmark, Event }

    public readonly struct Announcement
    {
        public readonly string Text; public readonly AnnouncementTier Tier;
        public Announcement(string text, AnnouncementTier tier) { Text = text; Tier = tier; }
    }

    public static class HudNotifications
    {
        public const int MaxAnnouncements = 3;
        private static readonly List<Announcement> announcements = new List<Announcement>();

        public const int MaxReceipts = 3;
        public readonly struct Receipt
        {
            public readonly string Text; public readonly float ExpiresAt;
            public Receipt(string text, float expiresAt) { Text = text; ExpiresAt = expiresAt; }
        }

        private static readonly List<Receipt> receipts = new List<Receipt>();
        public static IReadOnlyList<Receipt> Receipts => receipts;
        public static int Version { get; private set; }

        public static Vector3? HintWorldPosition { get; private set; }
        public static string Hint { get; private set; }
        public static bool HintCompleted { get; private set; }

        public static Transform Speaker { get; private set; }
        public static string Speech { get; private set; }
        public static float SpeechHeight { get; private set; }
        public static float SpeechUntil { get; private set; } = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            receipts.Clear(); HintWorldPosition = null; HintSpatialOnly = false; HintOnScreen = false; Hint = null; HintCompleted = false; Version++;
            announcements.Clear();
            Speaker = null; Speech = null; SpeechUntil = float.NegativeInfinity; Sign = null;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // A place name belongs to the scene that announced it: a new scene's HUD must not replay the last region
        // entered elsewhere (the storehouse showed "Green Lowlands"). Its own zones announce after this.
        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single) announcements.RemoveAll(a => a.Tier != AnnouncementTier.Event);
        }

        // A line over a speaker's head for a few seconds (real time); a newer line replaces it.
        public static void Say(Transform speaker, string text, float height = 2.35f, float seconds = 2.75f)
        {
            if (speaker == null || string.IsNullOrEmpty(text)) return;
            Speaker = speaker; Speech = text; SpeechHeight = height; SpeechUntil = Time.unscaledTime + seconds;
        }

        public static bool SpeechShowing => Speaker != null && !string.IsNullOrEmpty(Speech) && Time.unscaledTime < SpeechUntil;

        public static void ClearSpeech(Transform speaker) { if (Speaker == speaker) SpeechUntil = float.NegativeInfinity; }

        // A milestone event ("BONFIRE LIT"): queued behind anything already showing.
        public static void ShowBanner(string text) => Announce(text, AnnouncementTier.Event);

        // A place just entered. The HUD starts it once nothing covers it (a menu, a scripted moment, a scene fade).
        public static void ShowArea(string name, bool landmark = false) =>
            Announce(name, landmark ? AnnouncementTier.Landmark : AnnouncementTier.Region);

        // One queue, so two announcements never overlap; a repeat moves to the back, and the oldest drops past the cap.
        public static void Announce(string text, AnnouncementTier tier)
        {
            if (string.IsNullOrEmpty(text)) return;
            announcements.RemoveAll(a => a.Text == text);
            announcements.Add(new Announcement(text, tier));
            while (announcements.Count > MaxAnnouncements) announcements.RemoveAt(0);
        }

        public static bool TryPeekAnnouncement(out Announcement next)
        {
            next = announcements.Count > 0 ? announcements[0] : default;
            return announcements.Count > 0;
        }

        // Raised on the first frame an announcement is actually on screen (not merely taken from the queue, nor playing
        // behind a hidden HUD): a place is only remembered as seen once its name has been seen (18 Nov).
        public static event Action<Announcement> AnnouncementShown;

        public static void TakeAnnouncement() { if (announcements.Count > 0) announcements.RemoveAt(0); }

        public static void ReportShown(Announcement shown) => AnnouncementShown?.Invoke(shown);

        // Going to the title ends the run's queue: an event from the old run (BONFIRE LIT) must not greet a new one.
        public static void ClearAnnouncements() => announcements.Clear();

        public static void Post(string text, float seconds = 3.5f)
        {
            if (string.IsNullOrEmpty(text)) return;
            // Coalesce an identical message instead of stacking duplicates.
            receipts.RemoveAll(r => r.Text == text);
            receipts.Add(new Receipt(text, Time.unscaledTime + Mathf.Max(0.5f, seconds)));
            while (receipts.Count > MaxReceipts) receipts.RemoveAt(0);
            Version++;
        }

        // Called by the HUD each frame; returns true when the list changed.
        public static bool Expire()
        {
            int removed = receipts.RemoveAll(r => Time.unscaledTime >= r.ExpiresAt);
            if (removed > 0) Version++;
            return removed > 0;
        }

        // A spatial-only hint shows beside its world point or not at all; it never falls back to the bottom of the screen.
        public static bool HintSpatialOnly { get; private set; }
        // Set by the HUD each frame: the hint is actually showing (not hidden behind an interaction prompt or a menu).
        public static bool HintOnScreen { get; set; }

        // Text keeps its {tokens}: the HUD names the keys for the face each surface uses.
        public static void SetHint(string text, bool completed = false, Vector3? worldPosition = null, bool spatialOnly = false)
        {
            HintWorldPosition = worldPosition;
            HintSpatialOnly = spatialOnly && worldPosition.HasValue;
            if (Hint == text && HintCompleted == completed) return;
            Hint = text; HintCompleted = completed;
        }

        public static void ClearHint() => SetHint(null);

        // The sign being read (LessonSign, one at a time): its text with {tokens}, or null. The HUD draws it in a small
        // box at the bottom while the world plays on (user, 23 Nov: the modal reader was "garbage").
        public static string Sign { get; private set; }
        public static void ShowSign(string text) => Sign = text;
        public static void ClearSign() => Sign = null;
    }
}
