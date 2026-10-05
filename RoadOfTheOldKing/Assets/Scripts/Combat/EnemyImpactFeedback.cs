using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Damageable publishes accepted hits only: misses, guards and dead targets stay silent.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class EnemyImpactFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private AudioClip impactSound;
        [SerializeField, Range(0f, 1f)] private float volume = .48f;
        [SerializeField, Min(1f)] private float audibleDistance = 18f;
        [SerializeField, Range(1, 24)] private int particleCount = 10;
        [Tooltip("Fleck size range in world pixels (16 per unit).")]
        [SerializeField] private Vector2Int fleckPixels = new Vector2Int(2, 3);
        [Header("Killing blow")]
        [SerializeField, Min(1f)] private float lethalBurst = 2.2f;
        [Tooltip("Global freeze-frame on the killing blow, in real seconds. 0 disables.")]
        [SerializeField, Range(0f, 0.2f)] private float killFreeze = 0.08f;
        [SerializeField, Min(0f)] private float visualHeight = .45f;
        private Damageable health;
        private ParticleSystem flecks;
        private ParticleSystemRenderer particleRenderer;
        private AudioSource speaker;
        private int resetVersion;
        private IEnemy enemy;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            enemy = GetComponent<IEnemy>();
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
            main.maxParticles = 96; main.startSpeed = 0f; main.gravityModifier = 0f;
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
            // Health is already reduced when HitReceived fires: a dead target took the killing blow.
            bool lethal = !health.IsAlive;
            float burst = lethal ? lethalBurst : 1f;
            float angle = Mathf.Atan2(direction.y, direction.x);
            particleRenderer.sortingLayerID = body.sortingLayerID;
            particleRenderer.sortingOrder = body.sortingOrder + 5;
            flecks.Play();
            // Whole-pixel flecks thrown along the hit direction; a killing blow throws a wider, bigger spray.
            float spread = lethal ? .85f : .5f;
            for (int i = 0; i < Mathf.RoundToInt(particleCount * strength * burst); i++)
            {
                float a = angle + Random.Range(-spread, spread);
                int pixels = Random.Range(fleckPixels.x, fleckPixels.y + 1) + (lethal && i % 2 == 0 ? 1 : 0);
                var particle = new ParticleSystem.EmitParams {
                    position = position,
                    velocity = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(3.5f, 6.5f) * strength * (lethal ? 1.25f : 1f),
                    startLifetime = Random.Range(.22f, .36f) * (lethal ? 1.3f : 1f),
                    startSize = pixels / 16f,
                    startColor = i % 4 == 0 ? new Color(1f, .97f, .88f) : i % 3 == 0 ? new Color(.64f, .45f, .31f) : new Color(.86f, .78f, .62f),
                    rotation = 0f
                };
                flecks.Emit(particle, 1);
            }
            if (lethal) HitStop.Freeze(killFreeze);
            if (impactSound == null) return;
            var camera = Camera.main;
            float attenuation = camera == null ? 1f : Mathf.Clamp01(1f - Vector2.Distance(camera.transform.position, transform.position) / audibleDistance);
            // The killing blow lands lower and louder.
            speaker.pitch = lethal ? Random.Range(.76f, .82f) : Random.Range(.94f, 1.06f);
            speaker.PlayOneShot(impactSound, volume * Mathf.Min(strength, 1.25f) * attenuation * (lethal ? 1.4f : 1f));
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
