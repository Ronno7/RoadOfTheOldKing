using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The spark spray when a fire is lit (user, 18 Nov: "the bonfire just kinda spawns in, not very eventful"). One
    // emitter that only fires on demand: pixel sparks leap up and out of the flame's base, arc and fall back, stepping
    // down the flame's own colours (BonfireEmber shader; orange for ordinary fires, gold for upgrade fires).
    internal sealed class BonfireBurst
    {
        private readonly GameObject root;
        private readonly ParticleSystem particles;
        private readonly ParticleSystemRenderer renderer;
        private static readonly int RampId = Shader.PropertyToID("_Ramp");

        public BonfireBurst(Transform parent, Material material, Vector3 localPosition, bool gold)
        {
            root = new GameObject("Bonfire burst") { hideFlags = HideFlags.DontSave };
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true; // always simulating; nothing is emitted except by Fire
            main.maxParticles = 40;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1f;
            main.startSpeed = 0f;
            main.startSize = 1f / 16f;
            main.startColor = Color.white;
            // Sparks arc and fall back: about half of world gravity.
            main.gravityModifier = .5f;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.enabled = false;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var block = new MaterialPropertyBlock();
            block.SetFloat(RampId, gold ? 0f : 1f);
            renderer.SetPropertyBlock(block);
            particles.Play();
        }

        public int Count => particles.particleCount;

        public void SetBase(float localY)
        {
            var position = root.transform.localPosition;
            position.y = localY;
            root.transform.localPosition = position;
        }

        // count sparks, speed scaled by power; a quarter are two pixels wide. Drawn over the flame and its embers.
        public void Fire(int count, float power, SpriteRenderer flame)
        {
            renderer.sortingLayerID = flame.sortingLayerID;
            renderer.sortingOrder = flame.sortingOrder + 2;
            if (!particles.isPlaying) particles.Play();
            Vector3 origin = root.transform.position;
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(-60f, 60f) * Mathf.Deg2Rad;
                float speed = Random.Range(2.2f, 3.6f) * power;
                var spark = new ParticleSystem.EmitParams
                {
                    position = origin + new Vector3(Random.Range(-.25f, .25f), Random.Range(0f, .3f), 0f),
                    velocity = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f) * speed,
                    startLifetime = Random.Range(.45f, .8f) * Mathf.Lerp(1f, 1.25f, power - 1f),
                    startSize = Random.value < .25f ? 2f / 16f : 1f / 16f,
                    applyShapeToPosition = false
                };
                particles.Emit(spark, 1);
            }
        }

        public void Stop()
        {
            if (particles != null) particles.Clear();
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root);
        }
    }
}
