using System;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    public enum ThrowPhase { Inactive, Preparation, Aim, Release }

    // Cel exposures and gameplay markers share this data. A renderer only samples CelIndex.
    [Serializable]
    public sealed class ThrowActionTiming
    {
        public float[] preparation = { 0.04f, 0.04f };
        public float[] aim = { 0.20f, 0.20f };
        public float[] release = { 0.04f, 0.04f, 0.07f, 0.08f };
        [Tooltip("Zero-based release cel at whose start the physical weapon leaves the hand.")]
        public int launchCel = 1;
        [Range(0f, 1f)] public float aimMovementScale = 0.55f;

        internal static double Duration(float[] exposures, int count = int.MaxValue)
        {
            double duration = 0;
            for (int i = 0; i < Math.Min(exposures.Length, count); i++) duration += exposures[i];
            return duration;
        }

        internal static int CelAt(float[] exposures, double elapsed)
        {
            for (int i = 0; i < exposures.Length - 1; i++)
            {
                if (elapsed + 0.000001 < exposures[i]) return i;
                elapsed -= exposures[i];
            }
            return exposures.Length - 1;
        }

        internal bool IsValid => Valid(preparation) && Valid(aim) && Valid(release) &&
            launchCel > 0 && launchCel < release.Length &&
            !float.IsNaN(aimMovementScale) && aimMovementScale >= 0f && aimMovementScale <= 1f;

        private static bool Valid(float[] exposures)
        {
            if (exposures == null || exposures.Length == 0) return false;
            foreach (float exposure in exposures)
                if (float.IsNaN(exposure) || float.IsInfinity(exposure) || exposure <= 0f) return false;
            return true;
        }
    }

    // No input, damage, transforms, or animation events. AxeWeapon advances this once per step.
    public sealed class ThrowActionClock
    {
        private ThrowActionTiming timing;
        private double elapsed;
        public ThrowPhase Phase { get; private set; }
        public bool IsActive => Phase != ThrowPhase.Inactive;
        public bool IsCommitted { get; private set; }
        public bool HasLaunched { get; private set; }
        public bool CanCancelAim => IsActive && !IsCommitted;
        public int CelIndex => Phase == ThrowPhase.Preparation ? ThrowActionTiming.CelAt(timing.preparation, elapsed)
            : Phase == ThrowPhase.Aim ? timing.preparation.Length + ThrowActionTiming.CelAt(timing.aim, elapsed)
            : Phase == ThrowPhase.Release ? timing.preparation.Length + timing.aim.Length +
                ThrowActionTiming.CelAt(timing.release, elapsed) : -1;
        // Presentation reads these to fit any frame count: progress through the current phase
        // (Aim loops) and where the physical launch falls within Release.
        public float PhaseProgress => !IsActive ? 0f : (float)(elapsed / ThrowActionTiming.Duration(
            Phase == ThrowPhase.Preparation ? timing.preparation : Phase == ThrowPhase.Aim ? timing.aim : timing.release));
        public float LaunchFraction => timing == null ? 0f : (float)(ThrowActionTiming.Duration(timing.release, timing.launchCel) /
            ThrowActionTiming.Duration(timing.release));
        public float MovementScale => !IsActive ? 1f : CanCancelAim ? timing.aimMovementScale
            : Phase != ThrowPhase.Release ? 0f
            : Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(
                (float)ThrowActionTiming.Duration(timing.release, timing.launchCel + 1),
                (float)ThrowActionTiming.Duration(timing.release), (float)elapsed));

        public bool Begin(ThrowActionTiming definition)
        {
            if (IsActive || definition == null || !definition.IsValid) return false;
            timing = definition;
            IsCommitted = HasLaunched = false;
            SetPhase(ThrowPhase.Preparation);
            return true;
        }

        public bool Commit()
        {
            if (!CanCancelAim) return false;
            IsCommitted = true;
            if (Phase == ThrowPhase.Aim) SetPhase(ThrowPhase.Release);
            return true;
        }

        public void Cancel()
        {
            SetPhase(ThrowPhase.Inactive);
            IsCommitted = HasLaunched = false;
        }

        // Returns the launch edge once, plus only the part of this step AFTER that edge.
        // A slow frame therefore cannot launch twice or simulate flight during anticipation.
        public bool Advance(float deltaTime, out float flightSeconds)
        {
            flightSeconds = 0f;
            if (!IsActive || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return false;
            double remaining = deltaTime;
            if (Phase == ThrowPhase.Preparation)
            {
                double untilEnd = ThrowActionTiming.Duration(timing.preparation) - elapsed;
                if (remaining + 0.000001 < untilEnd) { elapsed += remaining; return false; }
                remaining = Math.Max(0, remaining - untilEnd);
                SetPhase(IsCommitted ? ThrowPhase.Release : ThrowPhase.Aim);
            }
            if (Phase == ThrowPhase.Aim)
            {
                elapsed = (elapsed + remaining) % ThrowActionTiming.Duration(timing.aim);
                return false;
            }
            double marker = ThrowActionTiming.Duration(timing.release, timing.launchCel);
            bool launch = !HasLaunched && elapsed + remaining + 0.000001 >= marker;
            if (HasLaunched) flightSeconds = deltaTime;
            else if (launch) flightSeconds = (float)Math.Max(0, remaining - Math.Max(0, marker - elapsed));
            HasLaunched |= launch;
            elapsed += remaining;
            if (elapsed + 0.000001 >= ThrowActionTiming.Duration(timing.release)) Phase = ThrowPhase.Inactive;
            return launch;
        }

        private void SetPhase(ThrowPhase phase) { Phase = phase; elapsed = 0; }
    }
}
