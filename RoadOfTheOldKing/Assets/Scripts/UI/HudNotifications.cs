using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.UI
{
    // Event-driven text channels for the HUD. Gameplay posts; the HUD presents. Nothing here
    // reads or writes gameplay state.
    //  Receipts: short, queued confirmations (rewards, unlocks, lessons); bounded, oldest dropped.
    //  Hint: the single current teaching hint owned by the Tutorial guide.
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

        public static string Hint { get; private set; }
        public static bool HintCompleted { get; private set; }
        public static int HintVersion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { receipts.Clear(); Hint = null; HintCompleted = false; Version++; HintVersion++; }

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

        public static void SetHint(string text, bool completed = false)
        {
            if (Hint == text && HintCompleted == completed) return;
            Hint = text; HintCompleted = completed; HintVersion++;
        }

        public static void ClearHint() => SetHint(null);
    }
}
