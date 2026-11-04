using System;
using RoadOfTheOldKing.UI;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // Answers the axe striking the world: a short sound and a few whole-pixel flecks by material. It samples
    // the weapon's confirmed strikes and changes nothing about them. A fixed particle budget, no per-hit objects.
    [DisallowMultipleComponent, RequireComponent(typeof(AxeWeapon))]
    public sealed class AxeSurfaceFeedback : MonoBehaviour
    {
        [Serializable]
        private struct Voice
        {
            public SurfaceMaterial material;
            public AudioClip strike;
            [Tooltip("Plays instead of the strike when the blow breaks the object. Empty: the strike again.")]
            public AudioClip breakSound;
            public Color light, dark;
        }

        [SerializeField] private Voice[] voices;
        [SerializeField, Range(0f, 1f)] private float volume = .32f;
        [SerializeField, Range(0f, 1f)] private float breakVolume = .42f;
        [SerializeField, Range(1, 16)] private int strikeFlecks = 4;
        [SerializeField, Range(1, 24)] private int breakFlecks = 10;
        [Tooltip("Raises flecks off the ground contact to about the height of the object struck.")]
        [SerializeField, Min(0f)] private float visualHeight = .4f;
        private AxeWeapon weapon;
        private ParticleSystem flecks;
        private AudioSource speaker;

        private void Awake()
        {
            weapon = GetComponent<AxeWeapon>();
            var child = new GameObject("Surface feedback");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            speaker = child.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            flecks = child.AddComponent<ParticleSystem>();
            flecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = flecks.main;
            main.playOnAwake = false; main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48; main.startSpeed = 0f; main.gravityModifier = 0f;
            var emission = flecks.emission; emission.enabled = false;
            var shape = flecks.shape; shape.enabled = false;
            var size = flecks.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var view = child.GetComponent<ParticleSystemRenderer>();
            var sprite = GetComponentInChildren<SpriteRenderer>(true);
            if (sprite != null) view.sharedMaterial = sprite.sharedMaterial;
            view.renderMode = ParticleSystemRenderMode.Billboard;
            view.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            view.receiveShadows = false;
            view.sortingLayerName = "Player";
            view.sortingOrder = 60;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            view.SetPropertyBlock(properties);
        }

        private void OnEnable() => weapon.SurfaceStruck += OnStruck;
        private void OnDisable() => weapon.SurfaceStruck -= OnStruck;

        private void OnStruck(SurfaceStrike strike)
        {
            int index = Array.FindIndex(voices, v => v.material == strike.Material);
            if (index < 0) return;
            var voice = voices[index];
            var clip = strike.Broke && voice.breakSound != null ? voice.breakSound : voice.strike;
            if (clip != null)
            {
                speaker.pitch = UnityEngine.Random.Range(.94f, 1.06f);
                speaker.PlayOneShot(clip, strike.Broke ? breakVolume : volume);
            }
            if (FeedbackSettings.FlashScale <= 0f) return;
            // A blow that holds kicks back toward the swing; one that breaks through carries on.
            Vector2 direction = strike.Direction.sqrMagnitude > .001f ? strike.Direction.normalized : Vector2.up;
            if (!strike.Broke) direction = -direction;
            float angle = Mathf.Atan2(direction.y, direction.x);
            float spread = strike.Broke ? 1.3f : .8f;
            Vector3 position = (Vector3)strike.Point + Vector3.up * visualHeight;
            flecks.Play();
            int count = strike.Broke ? breakFlecks : strikeFlecks;
            for (int i = 0; i < count; i++)
            {
                float a = angle + UnityEngine.Random.Range(-spread, spread);
                var particle = new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * UnityEngine.Random.Range(1.6f, strike.Broke ? 4f : 3f),
                    startLifetime = UnityEngine.Random.Range(.16f, strike.Broke ? .36f : .26f),
                    startSize = UnityEngine.Random.Range(1, 3) / 16f,
                    startColor = i % 3 == 0 ? voice.light : voice.dark,
                    rotation = 0f
                };
                flecks.Emit(particle, 1);
            }
        }

        private void OnDestroy() { if (flecks != null) Destroy(flecks.gameObject); }
    }
}
