using System;
using UnityEngine;

namespace TheLostShrine.UI
{
    // Leases compose transient freezes and menus without restoring time over another owner.
    public static class SimulationPause
    {
        private static int leases, audioLeases;
        private static float resumeSpeed;
        private static bool resumeAudio;
        public static bool IsPaused => leases > 0;
        public static float UnpausedTimeScale => IsPaused ? resumeSpeed : Time.timeScale;
        public static bool UnpausedAudio => audioLeases > 0 ? resumeAudio : AudioListener.pause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { leases = audioLeases = 0; }

        public static IDisposable Acquire(bool pauseAudio = false)
        {
            if (leases++ == 0) resumeSpeed = Time.timeScale;
            Time.timeScale = 0f;
            if (pauseAudio)
            {
                if (audioLeases++ == 0) resumeAudio = AudioListener.pause;
                AudioListener.pause = true;
            }
            return new Lease(pauseAudio);
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
            private readonly bool audio;
            public Lease(bool audio) => this.audio = audio;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                if (audio && --audioLeases == 0) AudioListener.pause = resumeAudio;
                if (--leases == 0) Time.timeScale = resumeSpeed;
            }
        }
    }
}
