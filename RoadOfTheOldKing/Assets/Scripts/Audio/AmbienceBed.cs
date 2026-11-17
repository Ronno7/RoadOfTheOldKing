using UnityEngine;

namespace RoadOfTheOldKing.Audio
{
    // A scene's looping background (wind, room tone), heard at one level everywhere in the scene. Fades in on arrival.
    [DisallowMultipleComponent]
    public sealed class AmbienceBed : MonoBehaviour
    {
        [SerializeField] private AudioClip loop;
        [SerializeField, Range(0f, 1f)] private float volume = .35f;
        [SerializeField, Min(0f)] private float fadeInSeconds = 3f;
        private AudioSource source;
        private float level;

        private void OnEnable()
        {
            if (loop == null) return;
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = true;
                source.spatialBlend = 0f;
                source.clip = loop;
                AudioChannels.Route(source, AudioChannel.Ambience);
            }
            level = 0f;
            source.volume = 0f;
            source.timeSamples = Random.Range(0, Mathf.Max(1, loop.samples));
            source.Play();
        }

        private void Update()
        {
            if (source == null) return;
            level = fadeInSeconds > 0f ? Mathf.MoveTowards(level, 1f, Time.unscaledDeltaTime / fadeInSeconds) : 1f;
            source.volume = volume * level;
        }

        private void OnDisable()
        {
            if (source != null) source.Stop();
        }
    }
}
