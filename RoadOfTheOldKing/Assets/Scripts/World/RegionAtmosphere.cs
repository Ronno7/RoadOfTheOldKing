using System.Collections.Generic;
using RoadOfTheOldKing.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoadOfTheOldKing.World
{
    public enum AtmosphereTier { Baseline, Restrained, Cinematic }

    // A region's atmosphere (21 Oct). In play the player's setting picks the look: Restrained by default, Cinematic as
    // an option (AtmosphereSettings). Each look adds a grade (a higher-priority global Volume), a camera-fitted world
    // overlay and ambient motes over the scene's base grade. Everything built here is unsaved runtime content, so the
    // scene file only stores the looks. Baseline (base grade alone) remains for the Editor view and tooling.
    // Presentation only: nothing here touches gameplay or collision.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class RegionAtmosphere : MonoBehaviour
    {
        [SerializeField] private AtmosphereLook restrained, cinematic;
        [Tooltip("Editor view only (Baseline keeps painting views clear). Play follows the player's setting.")]
        [SerializeField] private AtmosphereTier preview = AtmosphereTier.Baseline;
        [SerializeField] private string sortingLayer = "Player";
        [Tooltip("Above every world sprite (overhead art reaches about 500), below world-space UI.")]
        [SerializeField] private int overlayOrder = 1000;
        [Tooltip("Share of the overlay removed while in combat, so cloud shade and mist never sit on a fight.")]
        [SerializeField, Range(0f, 1f)] private float combatEase = 0.5f;
        [Tooltip("Seconds to ease in or out of the combat strength.")]
        [SerializeField, Min(0.05f)] private float easeSeconds = 1f;

        private readonly List<GameObject> built = new List<GameObject>();
        private readonly List<ParticleSystem> motes = new List<ParticleSystem>();
        private Transform overlay;
        private MeshRenderer overlayRenderer;
        private MaterialPropertyBlock strength;
        private float intensity = 1f;
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private Mesh quad;

        // Current overlay strength (1 outside combat); tooling may set it to stage the combat state.
        public float Intensity { get => intensity; set { intensity = Mathf.Clamp01(value); PushIntensity(); } }
        private bool hasApplied;
        private AtmosphereTier applied;

        // Tooling (captures, checks) can force a tier without touching the saved preview or the player's setting.
        public AtmosphereTier? Override { get; set; }
        public AtmosphereTier Tier => Override ?? (!Application.isPlaying ? preview
            : AtmosphereSettings.Cinematic ? AtmosphereTier.Cinematic : AtmosphereTier.Restrained);

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += Fit;
#if UNITY_EDITOR
            AtmosphereLook.Edited += OnLookEdited;
#endif
            Apply(Tier);
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= Fit;
#if UNITY_EDITOR
            AtmosphereLook.Edited -= OnLookEdited;
#endif
            Clear();
        }

        private void OnDestroy()
        {
            if (quad != null) DestroyImmediate(quad);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            hasApplied = false; // Rebuilt on the next editor update; objects cannot be destroyed here.
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        }

        private void OnLookEdited(AtmosphereLook look)
        {
            if (look == restrained || look == cinematic) OnValidate();
        }
#endif

        private void Update()
        {
            if (!hasApplied || Tier != applied) Apply(Tier);
            var cam = Camera.main;
            if (cam != null) FollowView(cam);
            if (Application.isPlaying && overlayRenderer != null)
            {
                float target = RoadOfTheOldKing.Enemies.EncounterState.InCombat ? 1f - combatEase : 1f;
                if (!Mathf.Approximately(intensity, target))
                    Intensity = Mathf.MoveTowards(intensity, target, Time.unscaledDeltaTime / easeSeconds);
            }
        }

        private void PushIntensity()
        {
            if (overlayRenderer == null) return;
            strength ??= new MaterialPropertyBlock();
            overlayRenderer.GetPropertyBlock(strength);
            strength.SetFloat(IntensityId, intensity);
            overlayRenderer.SetPropertyBlock(strength);
        }

        // Keeps the motes' spawn box over the view; particles live in world space and stay where they drift.
        public void FollowView(Camera cam)
        {
            float height = cam.orthographic ? cam.orthographicSize * 2f : 10f;
            var size = new Vector3(height * cam.aspect + 4f, height + 4f, 0f);
            foreach (var system in motes)
            {
                if (system == null) continue;
                system.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
                var shape = system.shape;
                shape.scale = size;
            }
        }

        // Tooling: advance the motes so a still capture shows them settled.
        public void Simulate(float seconds)
        {
            foreach (var system in motes) if (system != null) system.Simulate(seconds, true, true);
        }

        private void Fit(ScriptableRenderContext context, Camera cam)
        {
            if (overlay == null || cam.cameraType == CameraType.Preview || cam.cameraType == CameraType.Reflection) return;
            float height = cam.orthographic ? cam.orthographicSize * 2f : 50f;
            overlay.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            overlay.localScale = new Vector3(height * cam.aspect + 4f, height + 4f, 1f);
        }

        private void Apply(AtmosphereTier tier)
        {
            Clear();
            hasApplied = true;
            applied = tier;
            var look = tier == AtmosphereTier.Restrained ? restrained : tier == AtmosphereTier.Cinematic ? cinematic : null;
            if (look == null) return;

            var root = Child("Atmosphere - " + tier, transform);
            built.Add(root);
            if (look.grade != null)
            {
                var volume = root.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10f;
                volume.sharedProfile = look.grade;
            }
            if (look.overlay != null)
            {
                var go = Child("Overlay", root.transform);
                go.AddComponent<MeshFilter>().sharedMesh = Quad();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = look.overlay;
                renderer.sortingLayerName = sortingLayer;
                renderer.sortingOrder = overlayOrder;
                overlay = go.transform;
                overlayRenderer = renderer;
                PushIntensity();
            }
            foreach (var layer in look.motes)
                if (layer != null && layer.material != null) motes.Add(Motes(layer, root.transform));
        }

        private ParticleSystem Motes(MoteLayer layer, Transform parent)
        {
            var go = Child(layer.name, parent);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(layer.lifetime.x, layer.lifetime.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(layer.size.x, layer.size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(layer.colorA, layer.colorB);
            main.maxParticles = layer.maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = system.emission;
            emission.rateOverTime = layer.perSecond;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 18f, 0f);
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(layer.drift.x);
            velocity.y = new ParticleSystem.MinMaxCurve(layer.drift.y);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            var noise = system.noise;
            noise.enabled = layer.wander > 0f;
            noise.strength = layer.wander;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.15f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = layer.material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = overlayOrder + 1;
            system.Play();
            return system;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(parent, false);
            return go;
        }

        private Mesh Quad()
        {
            if (quad != null) return quad;
            quad = new Mesh { name = "Atmosphere quad", hideFlags = HideFlags.DontSave };
            quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(-.5f, .5f), new Vector3(.5f, .5f) };
            quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quad.RecalculateBounds();
            return quad;
        }

        private void Clear()
        {
            foreach (var go in built)
            {
                if (go == null) continue;
                // Deactivate first so a switch takes effect this frame; Destroy itself waits for the frame's end.
                go.SetActive(false);
                if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            }
            built.Clear();
            motes.Clear();
            overlay = null;
            overlayRenderer = null;
        }
    }
}
