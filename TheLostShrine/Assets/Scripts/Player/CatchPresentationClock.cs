using System;

namespace TheLostShrine.Player
{
    // Presentation only: a reach may wait indefinitely; time never grants possession.
    public sealed class CatchPresentationClock
    {
        private RegisteredActionSprites sprites;
        private float elapsed;
        public bool IsActive { get; private set; }
        public bool HasCaught { get; private set; }
        public int CelIndex => !IsActive ? -1 : HasCaught ? sprites.SampleAtSeconds(elapsed) : 0;

        public void BeginReach(RegisteredActionSprites sequence)
        {
            if (sequence == null || !sequence.IsRegistered(out _) || sequence.possessionCel < 1)
                throw new ArgumentException("Catch requires a registered sequence and possession marker.");
            sprites = sequence; elapsed = 0f; HasCaught = false; IsActive = true;
        }

        // Call on authoritative return arrival, never on a predicted timer.
        public bool ConfirmCatch()
        {
            if (!IsActive || HasCaught) return false;
            HasCaught = true; elapsed = sprites.CelStart(sprites.possessionCel);
            return true;
        }

        public void Advance(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!IsActive || !HasCaught) return;
            elapsed += seconds;
            if (elapsed + .000001f >= sprites.Duration) IsActive = false;
        }

        public void Cancel() { IsActive = HasCaught = false; elapsed = 0f; sprites = null; }
    }
}
