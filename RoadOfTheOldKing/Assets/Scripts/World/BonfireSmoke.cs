using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Presentation owned by BonfireSpriteView. One bounded emitter per fire, no particle objects
    // or per-frame allocations. The existing flame/material remain unchanged.
    internal sealed class BonfireSmoke
    {
        private readonly GameObject root;
        private readonly ParticleSystem particles;
        private readonly ParticleSystemRenderer renderer;
        private bool emitting;

        public BonfireSmoke(Transform parent, Material material)
        {
            root = new GameObject("Bonfire smoke") { hideFlags = HideFlags.DontSave };
            root.layer = parent.gameObject.layer;
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 12;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.4f);
            // Fixed six-pixel quad at 16 PPU; the shader shrinks its silhouette in whole pixels.
            main.startSize = 0.375f;
            main.startSpeed = 0f;
            main.startRotation = 0f;
            main.startColor = Color.white;
            var emission = particles.emission;
            emission.rateOverTime = 1.5f;
            var shape = particles.shape;
            shape.enabled = false;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.14f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 0.65f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                // Alpha carries silhouette size only; rendered pixels remain opaque.
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public void Refresh(bool lit, SpriteRenderer flame)
        {
            renderer.sortingLayerID = flame.sortingLayerID;
            renderer.sortingOrder = flame.sortingOrder;
            if (emitting == lit) return;
            emitting = lit;
            if (lit) particles.Play();
            else particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
