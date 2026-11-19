using System;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Leases compose transient freezes and menus without restoring time over another owner.
    public static class SimulationPause
    {
        private static int leases, audioLeases, freezeLeases;
        private static float resumeSpeed;
        private static bool resumeAudio;
        public static bool IsPaused => leases > 0;
        // Whether a menu, cutscene or stopped clock has the game. A hit-stop freeze holds time for a few frames but
        // isn't a pause: input, held buttons and committed actions carry through it. Gameplay gates ask this, never
        // Time.timeScale, which a freeze also sets to zero (that cancelled combos and charges until 18 Nov).
        public static bool BlocksPlay => Time.timeScale <= 0f && (leases == 0 || leases > freezeLeases);
        public static float UnpausedTimeScale => IsPaused ? resumeSpeed : Time.timeScale;
        public static bool UnpausedAudio => audioLeases > 0 ? resumeAudio : AudioListener.pause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { leases = audioLeases = freezeLeases = 0; }

        public static IDisposable Acquire(bool pauseAudio = false) => Acquire(pauseAudio, false);

        // A hit-stop: time stops, play doesn't (see BlocksPlay).
        public static IDisposable Freeze() => Acquire(false, true);

        private static IDisposable Acquire(bool pauseAudio, bool freeze)
        {
            if (leases++ == 0) resumeSpeed = Time.timeScale;
            if (freeze) freezeLeases++;
            Time.timeScale = 0f;
            if (pauseAudio)
            {
                if (audioLeases++ == 0) resumeAudio = AudioListener.pause;
                AudioListener.pause = true;
            }
            return new Lease(pauseAudio, freeze);
        }

        public static void SetTimeScale(float speed)
        {
            if (IsPaused) resumeSpeed = speed;
            else Time.timeScale = speed;
        }

        public static void SetAudioPaused(bool paused)
        {
            if (audioLeases > 0) resumeAudio = paused;
            else AudioListener.pause = paused;
        }

        private sealed class Lease : IDisposable
        {
            private bool disposed;
            private readonly bool audio, freeze;
            public Lease(bool audio, bool freeze) { this.audio = audio; this.freeze = freeze; }
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (audio && --audioLeases == 0) AudioListener.pause = resumeAudio;
                if (freeze) freezeLeases--;
                if (--leases == 0) Time.timeScale = resumeSpeed;
            }
        }
    }
}
