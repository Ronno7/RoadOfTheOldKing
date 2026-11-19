using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoadOfTheOldKing.Input
{
    // Controller rumble (user, 18 Nov: the game should feel better on a controller). One pulse at a time: a stronger
    // pulse arriving mid-pulse raises both motors and runs to the later end. Only while the controller is the active
    // device; a pause silences it, and losing focus or quitting stops it. Scale is the Vibration setting (0 = off),
    // saved outside the save slot like the other settings.
    public static class Rumble
    {
        private const string Key = "RoadOfTheOldKing.Settings.Vibration";
        private static float low, high, until;
        private static Gamepad pad;
        private static bool running, silenced, subscribed, focused = true;

        public static float Scale { get; private set; } = 1f;

        public static void SetScale(float scale)
        {
            Scale = Mathf.Clamp01(scale);
            PlayerPrefs.SetFloat(Key, Scale);
            PlayerPrefs.Save();
            if (Scale <= 0f) Stop();
        }

        // Motor strengths 0..1: low is the heavy, slow motor, high the light, fast one.
        public static void Pulse(float lowMotor, float highMotor, float seconds)
        {
            if (Scale <= 0f || !ActiveInput.UsingGamepad || Gamepad.current == null || !focused) return;
            float now = Time.unscaledTime;
            if (running && now < until && Gamepad.current == pad)
            {
                low = Mathf.Max(low, lowMotor); high = Mathf.Max(high, highMotor); until = Mathf.Max(until, now + seconds);
            }
            else
            {
                if (running && pad != Gamepad.current) Stop();
                pad = Gamepad.current; low = lowMotor; high = highMotor; until = now + seconds;
            }
            running = true;
            silenced = false;
            pad.SetMotorSpeeds(Mathf.Clamp01(low * Scale), Mathf.Clamp01(high * Scale));
        }

        public static void Stop()
        {
            if (pad != null && pad.added) pad.SetMotorSpeeds(0f, 0f);
            running = silenced = false;
            pad = null;
        }

        private static void Tick()
        {
            if (!running || (InputState.currentUpdateType & InputUpdateType.Editor) != 0) return;
            if (pad == null || !pad.added || Time.unscaledTime >= until || !ActiveInput.UsingGamepad || !focused)
            {
                Stop();
                return;
            }
            bool hold = RoadOfTheOldKing.UI.SimulationPause.BlocksPlay;
            if (hold == silenced) return;
            silenced = hold;
            pad.SetMotorSpeeds(hold ? 0f : Mathf.Clamp01(low * Scale), hold ? 0f : Mathf.Clamp01(high * Scale));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (subscribed) { InputSystem.onAfterUpdate -= Tick; Application.quitting -= Stop; Application.focusChanged -= OnFocus; }
            subscribed = false;
            running = silenced = false;
            pad = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            Scale = Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 1f));
            focused = Application.isFocused;
            if (subscribed) return;
            InputSystem.onAfterUpdate += Tick;
            Application.quitting += Stop;
            Application.focusChanged += OnFocus;
            subscribed = true;
        }

        private static void OnFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) Stop();
        }
    }
}
