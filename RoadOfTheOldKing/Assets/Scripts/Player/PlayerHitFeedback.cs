using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Presentation only: makes taking a hit unmistakable. A red flash on the sprite, a tiny global
    // hit-stop, a capped camera kick along the blow, then the classic 16-bit blink for the rest of the
    // post-hit invulnerability. The HUD adds the screen-edge vignette; the animator plays the flinch.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    [DefaultExecutionOrder(150)] // after PlayerSpriteAnimator, before PlayerWeaponCarry copies the body colour
    public sealed class PlayerHitFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Color flashColor = new Color(1f, .32f, .26f, 1f);
        [Tooltip("Flash when a healing flask's heal lands.")]
        [SerializeField] private Color healFlashColor = new Color(1f, .84f, .45f, 1f);
        [SerializeField, Min(0f)] private float healFlashDuration = .18f;
        [SerializeField, Min(0f)] private float flashDuration = .1f;
        [Tooltip("Global freeze on taking damage (only from normal speed).")]
        [SerializeField, Min(0f)] private float hitStop = .06f;
        [Tooltip("Camera kick distance along the blow, in world units (capped by the camera).")]
        [SerializeField, Min(0f)] private float cameraKick = .22f;
        [SerializeField, Min(.02f)] private float blinkInterval = .07f;
        [SerializeField, Range(0f, 1f)] private float blinkAlpha = .3f;

        private PlayerHealth player;
        private Damageable health;
        private MaterialPropertyBlock properties;
        private float flashUntil;
        private Color currentFlash;
        private PlayerFlask flask;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            health = GetComponent<Damageable>();
            flask = GetComponent<PlayerFlask>();
            properties = new MaterialPropertyBlock();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (health != null) health.HitReceived += OnHit;
            if (flask != null) flask.Healed += OnHealed;
        }

        private void OnHealed() { currentFlash = healFlashColor; flashUntil = Time.unscaledTime + healFlashDuration; }

        private void OnDisable()
        {
            if (health != null) health.HitReceived -= OnHit;
            if (flask != null) flask.Healed -= OnHealed;
            flashUntil = 0f;
            Apply(0f, 1f);
        }

        private void OnHit(CombatHit hit)
        {
            if (hit.Damage <= 0) return;
            currentFlash = flashColor;
            flashUntil = Time.unscaledTime + flashDuration;
            HitStop.Freeze(hitStop);
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow2D>() : null;
            if (cam != null) cam.Kick(hit.Direction, cameraKick);
        }

        private void LateUpdate()
        {
            float flash = Time.unscaledTime < flashUntil ? Mathf.Clamp01(FeedbackSettings.FlashScale) : 0f;
            bool blink = player.IsAlive && player.IsRecoveringFromHit && Mathf.Repeat(Time.time / blinkInterval, 2f) >= 1f;
            Apply(flash, blink ? blinkAlpha : 1f);
        }

        private void Apply(float flash, float alpha)
        {
            if (body == null) return;
            var color = body.color;
            color.a = alpha;
            body.color = color;
            body.GetPropertyBlock(properties);
            properties.SetFloat("_FlashAmount", flash);
            properties.SetColor("_FlashColor", currentFlash);
            body.SetPropertyBlock(properties);
        }
    }
}
