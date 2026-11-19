using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Gold embers over a lit upgrade fire (#90: "so that I can spot where to upgrade from a distance"). One bounded
    // emitter, like BonfireSmoke: single-pixel sparks born in the upper flame drift up and waver, stepping down the gold
    // flame's ramp as they age (the BonfireEmber shader reads the age from the alpha), then wink out.
    internal sealed class BonfireEmbers
    {
        private readonly GameObject root;
        private readonly ParticleSystem particles;
        private readonly ParticleSystemRenderer renderer;
        private bool emitting;

        public BonfireEmbers(Transform parent, Material material, Vector3 localPosition)
        {
            root = new GameObject("Bonfire embers") { hideFlags = HideFlags.DontSave };
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.8f, 1.4f);
            main.startSize = 1f / 16f; // one native pixel
            main.startSpeed = 0f;
            main.startRotation = 0f;
            main.startColor = Color.white;
            var emission = particles.emission;
            emission.rateOverTime = 3f;
            // Born across the upper flame, not at one point.
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(.45f, .3f, 0f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-.12f, .12f);
            velocity.y = new ParticleSystem.MinMaxCurve(.7f, 1.1f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            // A gentle waver; the shader's pixel snapping turns it into one-pixel hops.
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = .2f;
            noise.frequency = 2f;
            noise.scrollSpeed = .5f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            // Alpha is the age the shader steps through: bright gold, gold, amber, deep amber, gone.
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
        }

        public bool IsEmitting => emitting;

        // Above the flame it rises from.
        public void Refresh(bool lit, SpriteRenderer flame)
        {
            renderer.sortingLayerID = flame.sortingLayerID;
            renderer.sortingOrder = flame.sortingOrder + 1;
            if (emitting == lit) return;
            emitting = lit;
            if (lit) particles.Play();
            else particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void SetHeight(float localY)
        {
            var position = root.transform.localPosition;
            position.y = localY;
            root.transform.localPosition = position;
        }

        public void Stop()
        {
            emitting = false;
            if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Dispose()
        {
            if (root != null) Object.Destroy(root);
        }
    }
}
