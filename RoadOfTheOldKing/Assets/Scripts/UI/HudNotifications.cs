using System.Collections.Generic;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Event-driven text channels for the HUD. Gameplay posts; the HUD presents. Nothing here
    // reads or writes gameplay state.
    //  Receipts: short, queued confirmations (rewards, unlocks, lessons); bounded, oldest dropped.
    //  Hint: the single current teaching hint owned by the Tutorial guide.
    //  Banner: one large, brief milestone line across the screen (for example "BONFIRE LIT").
    //  Speech: one short line spoken by someone in the world, shown over their head (the trader's barks).
    public static class HudNotifications
    {
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
        public static int HintVersion { get; private set; }

        public static string Banner { get; private set; }
        public static float BannerStartedAt { get; private set; } = float.NegativeInfinity;
        public static int BannerVersion { get; private set; }

        public static Transform Speaker { get; private set; }
        public static string Speech { get; private set; }
        public static float SpeechHeight { get; private set; }
        public static float SpeechUntil { get; private set; } = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            receipts.Clear(); HintWorldPosition = null; Hint = null; HintCompleted = false; Version++; HintVersion++;
            Banner = null; BannerStartedAt = float.NegativeInfinity; BannerVersion++;
            Speaker = null; Speech = null; SpeechUntil = float.NegativeInfinity;
        }

        // A line over a speaker's head for a few seconds (real time); a newer line replaces it.
        public static void Say(Transform speaker, string text, float height = 2.35f, float seconds = 2.75f)
        {
            if (speaker == null || string.IsNullOrEmpty(text)) return;
            Speaker = speaker; Speech = text; SpeechHeight = height; SpeechUntil = Time.unscaledTime + seconds;
        }

        public static bool SpeechShowing => Speaker != null && !string.IsNullOrEmpty(Speech) && Time.unscaledTime < SpeechUntil;

        public static void ClearSpeech(Transform speaker) { if (Speaker == speaker) SpeechUntil = float.NegativeInfinity; }

        // Replaces any banner already showing; the HUD fades it in, holds and fades it out on unscaled time.
        public static void ShowBanner(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Banner = text; BannerStartedAt = Time.unscaledTime; BannerVersion++;
        }

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

        public static void SetHint(string text, bool completed = false, Vector3? worldPosition = null)
        {
            HintWorldPosition = worldPosition;
            if (Hint == text && HintCompleted == completed) return;
            Hint = text; HintCompleted = completed; HintVersion++;
        }

        public static void ClearHint() => SetHint(null);
    }
}
