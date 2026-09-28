using UnityEngine;

namespace TheLostShrine.Combat
{
    // Damageable publishes accepted hits only: misses, guards and dead targets stay silent.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class EnemyImpactFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private AudioClip impactSound;
        [SerializeField, Range(0f, 1f)] private float volume = .48f;
        [SerializeField, Min(1f)] private float audibleDistance = 18f;
        [SerializeField, Range(1, 16)] private int particleCount = 7;
        [SerializeField, Min(0f)] private float visualHeight = .45f;
        private Damageable health;
        private ParticleSystem flecks;
        private ParticleSystemRenderer particleRenderer;
        private AudioSource speaker;
        private int resetVersion;
        private SimpleMeleeEnemy enemy;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            enemy = GetComponent<SimpleMeleeEnemy>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            var child = new GameObject("Confirmed hit feedback");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            speaker = child.AddComponent<AudioSource>();
            speaker.playOnAwake = false;
            speaker.spatialBlend = 0f;
            speaker.Stop();
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
            particleRenderer = child.GetComponent<ParticleSystemRenderer>();
            if (body != null) particleRenderer.sharedMaterial = body.sharedMaterial;
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            particleRenderer.SetPropertyBlock(properties);
        }

        private void OnEnable()
        {
            health.HitReceived += OnHit;
            if (enemy != null) resetVersion = enemy.ResetVersion;
        }

        private void LateUpdate()
        {
            if (body != null)
            {
                particleRenderer.sortingLayerID = body.sortingLayerID;
                particleRenderer.sortingOrder = body.sortingOrder + 5;
            }
            if (enemy != null && resetVersion != enemy.ResetVersion)
            {
                resetVersion = enemy.ResetVersion;
                Clear();
            }
        }

        private void OnHit(CombatHit hit)
        {
            if (body == null || !body.enabled || !body.gameObject.activeInHierarchy) return;
            Vector2 direction = hit.Direction.sqrMagnitude > .001f ? hit.Direction : Vector2.up;
            Vector3 position = hit.ImpactPoint.HasValue ? (Vector3)hit.ImpactPoint.Value : transform.position;
            position += Vector3.up * visualHeight;
            position.z = body.transform.position.z;
            float strength = Mathf.Clamp(Mathf.Sqrt(hit.Damage / 10f), .8f, 1.6f);
            float angle = Mathf.Atan2(direction.y, direction.x);
            particleRenderer.sortingLayerID = body.sortingLayerID;
            particleRenderer.sortingOrder = body.sortingOrder + 5;
            flecks.Play();
            for (int i = 0; i < Mathf.RoundToInt(particleCount * strength); i++)
            {
                float a = angle + Random.Range(-.55f, .55f);
                var particle = new ParticleSystem.EmitParams {
                    position = position,
                    velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(1.3f, 3.1f) * strength,
                    startLifetime = Random.Range(.12f, .23f),
                    startSize = Random.Range(.035f, .075f),
                    startColor = i % 3 == 0 ? new Color(.64f, .45f, .31f) : new Color(.86f, .78f, .62f),
                    rotation = 0f
                };
                flecks.Emit(particle, 1);
            }
            if (impactSound == null) return;
            var camera = Camera.main;
            float attenuation = camera == null ? 1f : Mathf.Clamp01(1f - Vector2.Distance(camera.transform.position, transform.position) / audibleDistance);
            speaker.pitch = Random.Range(.94f, 1.06f);
            speaker.PlayOneShot(impactSound, volume * Mathf.Min(strength, 1.25f) * attenuation);
        }

        private void Clear()
        {
            if (flecks != null) flecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (speaker != null) speaker.Stop();
        }

        private void OnDisable() { health.HitReceived -= OnHit; Clear(); }
        private void OnDestroy() { if (flecks != null) Destroy(flecks.gameObject); }
    }
}
