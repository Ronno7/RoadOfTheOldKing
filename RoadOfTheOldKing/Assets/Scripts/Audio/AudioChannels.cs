using UnityEngine;
using UnityEngine.Audio;

namespace RoadOfTheOldKing.Audio
{
    public enum AudioChannel { Music, Ambience, Effects }

    // One mixer (Resources/Audio/Mixer) with a group per channel. Every AudioSource is routed through Route,
    // so the settings sliders, and later filters, ducking and snapshots, act on whole channels.
    public static class AudioChannels
    {
        private static readonly string[] Names = { "Music", "Ambience", "Effects" };
        private static AudioMixer mixer;
        private static AudioMixerGroup[] groups;
        private static bool loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { mixer = null; groups = null; loaded = false; }

        // Exposed mixer parameters are dB; the sliders are linear 0..1.
        public static string VolumeParameter(AudioChannel channel) => Names[(int)channel] + "Volume";

        public static void Route(AudioSource source, AudioChannel channel)
        {
            if (source != null && Load()) source.outputAudioMixerGroup = groups[(int)channel];
        }

        public static void SetVolume(AudioChannel channel, float linear)
        {
            if (Load()) mixer.SetFloat(VolumeParameter(channel), linear > .0001f ? 20f * Mathf.Log10(linear) : -80f);
        }

        private static bool Load()
        {
            if (loaded) return mixer != null;
            loaded = true;
            mixer = Resources.Load<AudioMixer>("Audio/Mixer");
            if (mixer == null) { Debug.LogError("Audio mixer missing at Resources/Audio/Mixer."); return false; }
            groups = new AudioMixerGroup[Names.Length];
            for (int i = 0; i < Names.Length; i++)
            {
                var found = mixer.FindMatchingGroups("Master/" + Names[i]);
                groups[i] = found.Length > 0 ? found[0] : null;
                if (groups[i] == null) Debug.LogError("Audio mixer has no " + Names[i] + " group.");
            }
            return true;
        }
    }
}
