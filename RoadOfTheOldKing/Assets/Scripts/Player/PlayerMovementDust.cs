using TheLostShrine.Input;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Presentation only; reads the body's real (post-physics) velocity every physics step.
    //  Trail: specks behind the feet that grow with speed (nothing at a stroll, thick at a sprint).
    //  Skid: when speed is shed hard (stop, reversal, sharp turn, wall bump, knockback) a cloud is
    //        thrown forward along the old heading, scaled by how fast we were going.
    // Whole-pixel specks with stepped sizes and hard alpha only (no fades).
    [DefaultExecutionOrder(-100)] // sample before PlayerMovement rewrites the velocity this step
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement), typeof(Rigidbody2D))]
    public sealed class PlayerMovementDust : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [Tooltip("Palette dry earth and cream plaster: lighter than both grass and courtyard paving.")]
        [SerializeField] private Color[] colors = { new Color32(0xDF, 0xC2, 0x91, 0xFF), new Color32(0xF1, 0xDE, 0xB0, 0xFF) };
        [Tooltip("Offset from the transform (ground pivot) to where the feet meet the ground.")]
        [SerializeField] private Vector2 footOffset = new Vector2(0f, 0.06f);

        [Header("Trail (grows with speed)")]
        [Tooltip("Speed, as a fraction of walk speed, where the trail begins.")]
        [SerializeField, Range(0f, 1.5f)] private float trailStart = 0.7f;
        [Tooltip("Units travelled between trail puffs: at the start speed, then at sprint speed.")]
        [SerializeField] private Vector2 trailStride = new Vector2(1.5f, 0.6f);
        [SerializeField] private Vector2 trailLifetime = new Vector2(0.28f, 0.42f);

        [Header("Skid clouds (inertia)")]
        [Tooltip("Rate of speed loss (units/s²) that counts as a skid.")]
        [SerializeField, Min(1f)] private float skidThreshold = 10f;
        [Tooltip("Minimum speed, as a fraction of walk speed, for a skid to throw dust.")]
        [SerializeField, Range(0f, 1.5f)] private float skidMinSpeed = 0.6f;
        [Tooltip("Puffs thrown when a skid starts: at walk speed, then at sprint speed (scaled by speed squared).")]
        [SerializeField] private Vector2Int skidBurst = new Vector2Int(2, 12);
        [Tooltip("Extra puffs per unit of speed shed while a skid continues, at sprint speed.")]
        [SerializeField, Min(0f)] private float skidSprayPerSpeed = 1.2f;
        [SerializeField] private Vector2 cloudLifetime = new Vector2(0.4f, 0.65f);

        private PlayerMovement movement;
        private PlayerDash dash;
        private PlayerHealth health;
        private PlayerMovementInput input;
        private Rigidbody2D rb;
        // Top-down depth: dust moving south (toward the camera) draws in front of the body, the rest behind.
        private ParticleSystem specksBack, specksFront, puffsBack, puffsFront;
        private ParticleSystem[] systems;
        private ParticleSystemRenderer[] backRenderers, frontRenderers;
        private Vector2 lastVelocity, lastPosition;
        private bool hasBaseline, wasSuppressed, skidding, trailing;
        private float trailTravelled, sprayCarry, trailOffFor = 1f;
        private int side = 1;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            dash = GetComponent<PlayerDash>();
            health = GetComponent<PlayerHealth>();
            input = GetComponent<PlayerMovementInput>();
            rb = GetComponent<Rigidbody2D>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            // 2px specks step to 1px halfway; 3px puffs step 3 -> 2 -> 1 in thirds.
            // Puffs drag less than specks so a cloud drifts apart instead of stacking on one spot.
            specksBack = CreateSystem("Movement dust (specks, back)", 48, 5f, 1f, 0.5f);
            specksFront = CreateSystem("Movement dust (specks, front)", 48, 5f, 1f, 0.5f);
            puffsBack = CreateSystem("Movement dust (puffs, back)", 48, 3f, 1f, 2f / 3f, 1f / 3f);
            puffsFront = CreateSystem("Movement dust (puffs, front)", 48, 3f, 1f, 2f / 3f, 1f / 3f);
            systems = new[] { specksBack, specksFront, puffsBack, puffsFront };
            backRenderers = new[] { specksBack.GetComponent<ParticleSystemRenderer>(), puffsBack.GetComponent<ParticleSystemRenderer>() };
            frontRenderers = new[] { specksFront.GetComponent<ParticleSystemRenderer>(), puffsFront.GetComponent<ParticleSystemRenderer>() };
        }

        private ParticleSystem CreateSystem(string name, int max, float dragAmount, params float[] steps)
        {
            var child = new GameObject(name);
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false; main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max; main.startSpeed = 0f; main.gravityModifier = 0f;
            var emission = system.emission; emission.enabled = false;
            var shape = system.shape; shape.enabled = false;
            var keys = new Keyframe[steps.Length + 1];
            for (int i = 0; i < steps.Length; i++)
                keys[i] = new Keyframe((float)i / steps.Length, steps[i], float.PositiveInfinity, float.PositiveInfinity);
            keys[steps.Length] = new Keyframe(1f, steps[steps.Length - 1], float.PositiveInfinity, float.PositiveInfinity);
            var size = system.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
            var drag = system.limitVelocityOverLifetime; drag.enabled = true; drag.drag = dragAmount;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            if (body != null) renderer.sharedMaterial = body.sharedMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            renderer.SetPropertyBlock(properties);
            return system;
        }

        private void OnEnable() { hasBaseline = false; skidding = trailing = false; }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = rb.linearVelocity, position = rb.position;
            bool suppressed = dash.IsDashing || (input != null && !input.IsActive) || (health != null && !health.IsAlive);
            // Respawn, travel and scripted moves: moved farther than any velocity could carry us.
            bool teleported = hasBaseline && (position - lastPosition).magnitude >
                Mathf.Max(lastVelocity.magnitude, velocity.magnitude) * dt * 3f + 0.1f;
            if (!hasBaseline || suppressed || wasSuppressed || teleported)
            {
                // Dashes, menus and teleports re-baseline instead of reading as a skid.
                hasBaseline = true;
                wasSuppressed = suppressed;
                lastVelocity = velocity; lastPosition = position;
                skidding = trailing = false; trailTravelled = sprayCarry = 0f;
                return;
            }
            float walk = movement.MoveSpeed, sprint = movement.SprintSpeed;
            float previousSpeed = lastVelocity.magnitude, speed = velocity.magnitude;
            SyncSorting();

            // Inertia: how much speed the last step shed along the old heading, plus sideways change.
            bool skidNow = false;
            if (previousSpeed >= walk * skidMinSpeed)
            {
                Vector2 heading = lastVelocity / previousSpeed;
                Vector2 change = velocity - lastVelocity;
                float along = Mathf.Max(0f, -Vector2.Dot(change, heading));
                float across = Mathf.Abs(change.x * heading.y - change.y * heading.x);
                float shed = along + across * 0.5f;
                if (shed / dt >= skidThreshold)
                {
                    skidNow = true;
                    float energy = Mathf.InverseLerp(walk * skidMinSpeed, sprint, previousSpeed);
                    float weight = energy * energy;
                    if (!skidding)
                        Cloud(position, heading, Mathf.RoundToInt(Mathf.Lerp(skidBurst.x, skidBurst.y, weight)), energy);
                    sprayCarry += shed * skidSprayPerSpeed * Mathf.Lerp(0.15f, 1f, weight);
                    for (int i = 0; sprayCarry >= 1f && i < 4; i++) { sprayCarry -= 1f; Cloud(position, heading, 1, energy); }
                }
            }
            skidding = skidNow;
            if (!skidNow) sprayCarry = 0f;

            // Trail: starts at a brisk walk and thickens toward sprint speed.
            if (!skidNow && speed >= walk * trailStart)
            {
                float intensity = Mathf.InverseLerp(walk * trailStart, sprint, speed);
                Vector2 direction = velocity / speed;
                if (!trailing && trailOffFor >= 0.2f) Trail(position, direction, intensity, 1 + Mathf.RoundToInt(intensity * 2f), 1.3f);
                trailing = true; trailOffFor = 0f;
                trailTravelled += speed * dt;
                float stride = Mathf.Lerp(trailStride.x, trailStride.y, intensity);
                if (trailTravelled >= stride)
                {
                    trailTravelled = Mathf.Min(trailTravelled - stride, stride);
                    Trail(position, direction, intensity, intensity > 0.5f ? 2 : 1, 1f);
                }
            }
            else
            {
                trailing = false; trailTravelled = 0f;
                trailOffFor += dt;
            }
            lastVelocity = velocity; lastPosition = position;
        }

        private void SyncSorting()
        {
            if (body == null) return;
            foreach (var renderer in backRenderers) { renderer.sortingLayerID = body.sortingLayerID; renderer.sortingOrder = body.sortingOrder - 1; }
            foreach (var renderer in frontRenderers) { renderer.sortingLayerID = body.sortingLayerID; renderer.sortingOrder = body.sortingOrder + 1; }
        }

        // Kicked back from alternating feet; faster running throws them farther.
        private void Trail(Vector2 position, Vector2 direction, float intensity, int count, float strength)
        {
            Vector2 across = new Vector2(-direction.y, direction.x);
            for (int i = 0; i < count; i++)
            {
                side = -side;
                Vector2 origin = position + footOffset + across * (side * 2f / 16f) + Random.insideUnitCircle * (1f / 16f);
                Vector2 kick = (-direction * Random.Range(0.5f, 1.1f) * Mathf.Lerp(0.8f, 1.3f, intensity)
                    + Vector2.up * Random.Range(0.25f, 0.6f) + across * (side * Random.Range(0f, 0.3f))) * strength;
                Emit(false, origin, kick, Random.Range(trailLifetime.x, trailLifetime.y), RandomColor());
            }
        }

        // Kicked out from the planted feet: fanned sideways so it spills out from under the body
        // (a sliding body outruns anything thrown straight ahead), with a gentle push along the old heading.
        private void Cloud(Vector2 position, Vector2 heading, int count, float energy)
        {
            Vector2 across = new Vector2(-heading.y, heading.x);
            for (int i = 0; i < count; i++)
            {
                side = -side;
                Vector2 origin = position + footOffset + heading * (Random.Range(-2f, 4f) / 16f) + across * (side * Random.Range(1f, 6f) / 16f);
                Vector2 throwVelocity = heading * Random.Range(0.5f, 1.4f) * Mathf.Lerp(0.8f, 1.4f, energy)
                    + across * (side * Random.Range(0.8f, 2.4f) * Mathf.Lerp(0.7f, 1.3f, energy))
                    + Vector2.up * Random.Range(0.15f, 0.5f);
                bool big = Random.value < Mathf.Lerp(0.25f, 0.8f, energy);
                // Big puffs favour the lightest colour so they read against dirt paths' own cream flecks.
                Color color = big && colors.Length > 0 && Random.value < 0.7f ? colors[colors.Length - 1] : RandomColor();
                Emit(big, origin, throwVelocity, Random.Range(cloudLifetime.x, cloudLifetime.y), color);
            }
        }

        private Color RandomColor() => colors.Length > 0 ? colors[Random.Range(0, colors.Length)] : Color.white;

        private void Emit(bool big, Vector2 origin, Vector2 velocity, float lifetime, Color color)
        {
            bool front = velocity.y < 0f;
            var system = big ? (front ? puffsFront : puffsBack) : (front ? specksFront : specksBack);
            int pixels = big ? 3 : 2;
            if (!system.isPlaying) system.Play();
            system.Emit(new ParticleSystem.EmitParams
            {
                position = new Vector3(Mathf.Round(origin.x * 16f) / 16f, Mathf.Round(origin.y * 16f) / 16f, transform.position.z),
                velocity = velocity,
                startLifetime = lifetime,
                startSize = pixels / 16f,
                startColor = color,
                rotation = 0f
            }, 1);
        }

        private void OnDisable()
        {
            if (systems == null) return;
            foreach (var system in systems) if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            if (systems == null) return;
            foreach (var system in systems) if (system != null) Destroy(system.gameObject);
        }
    }
}
