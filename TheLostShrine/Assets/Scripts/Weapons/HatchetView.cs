using UnityEngine;

namespace TheLostShrine.Weapons
{
    [DisallowMultipleComponent, RequireComponent(typeof(HatchetWeapon))]
    public sealed class HatchetView : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private SpriteRenderer blade;
        [SerializeField] private LineRenderer arc;
        [SerializeField] private TrailRenderer trail;
        private HatchetWeapon weapon;
        private Color originalBlade;
        private bool wasFlying;
        private readonly AnimationCurve ringWidth = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        private readonly AnimationCurve slashWidth = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.3f, 0.85f),
            new Keyframe(0.65f, 1f), new Keyframe(1f, 0f));

        private void Awake()
        {
            weapon = GetComponent<HatchetWeapon>();
            if (blade != null)
                originalBlade = blade.color;
        }

        private void LateUpdate()
        {
            if (model == null || weapon.Settings == null)
                return;
            bool flying = weapon.State == HatchetState.Flying || weapon.State == HatchetState.Returning;
            if (trail != null)
            {
                if (flying != wasFlying)
                    trail.Clear();
                trail.emitting = flying;
            }
            wasFlying = flying;
            if (arc != null)
                arc.enabled = false;
            if (blade != null)
                blade.color = Color.Lerp(originalBlade, new Color(1f, 0.85f, 0.25f), weapon.Charge01);

            float angle = Mathf.Atan2(weapon.AimDirection.y, weapon.AimDirection.x) * Mathf.Rad2Deg;
            model.localPosition = Vector3.zero;
            if (weapon.Owner != null && !weapon.IsAway)
            {
                transform.position = weapon.Owner.transform.position;
                model.localPosition = (Vector3)weapon.AimDirection * 0.5f;
            }

            switch (weapon.State)
            {
                case HatchetState.OnGround:
                    angle = 35f;
                    model.localPosition = Vector3.up * (0.06f + Mathf.Sin(Time.time * 3f) * 0.04f);
                    DrawArc(transform.position, 0.55f, 0f, 360f, new Color(1f, 0.8f, 0.3f, 0.55f));
                    break;
                case HatchetState.LightChop:
                    angle = DrawLightSlash();
                    break;
                case HatchetState.Charging:
                    angle += Mathf.Sin(Time.time * 35f) * weapon.Charge01 * 8f;
                    DrawArc(transform.position, 0.75f, -90f, 360f * weapon.Charge01,
                        weapon.Charge01 >= 1f ? new Color(1f, 0.8f, 0.1f) : new Color(0.7f, 0.85f, 1f));
                    break;
                case HatchetState.Cleaving:
                    angle += 360f * weapon.AttackProgress;
                    model.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.1f;
                    DrawArc(transform.position, weapon.Settings.cleaveRadius, angle - 300f, 300f,
                        new Color(1f, 0.75f, 0.2f, 0.85f));
                    break;
                case HatchetState.Flying:
                case HatchetState.Returning:
                    angle = Time.time * -1000f;
                    break;
                case HatchetState.Stuck:
                    angle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg - 25f;
                    break;
            }
            model.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private float DrawLightSlash()
        {
            var settings = weapon.Settings;
            float progress = weapon.AttackProgress;
            float direction = weapon.ComboIndex == 1 ? -1f : 1f;
            float aimAngle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
            float halfArc = settings.lightArc * 0.5f;
            float slashTime = Mathf.InverseLerp(settings.lightWindupFraction, settings.lightSwingEndFraction, progress);
            float sweepProgress = 1f - Mathf.Pow(1f - slashTime, 3f);
            float sweepAngle = aimAngle + Mathf.Lerp(-halfArc, halfArc, sweepProgress) * direction;
            float poseAngle = sweepAngle;
            float reach = Mathf.Max(0.25f, weapon.LightReach - 0.45f);
            float poseRadius;

            if (progress < settings.lightWindupFraction)
            {
                float windup = Mathf.InverseLerp(0f, settings.lightWindupFraction, progress);
                poseAngle = aimAngle - Mathf.Lerp(halfArc * 0.65f, halfArc, windup) * direction;
                poseRadius = Mathf.Lerp(0.5f, 0.6f, windup);
            }
            else if (progress <= settings.lightSwingEndFraction)
                poseRadius = Mathf.Lerp(0.6f, reach, sweepProgress);
            else
            {
                float recovery = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(settings.lightSwingEndFraction, 1f, progress));
                poseAngle = Mathf.Lerp(sweepAngle, aimAngle, recovery);
                poseRadius = Mathf.Lerp(reach, 0.5f, recovery);
            }
            model.localPosition = new Vector3(Mathf.Cos(poseAngle * Mathf.Deg2Rad), Mathf.Sin(poseAngle * Mathf.Deg2Rad)) * poseRadius;

            float fadeEnd = Mathf.Min(1f, settings.lightSwingEndFraction + 0.28f);
            float fade = 1f - Mathf.InverseLerp(settings.lightSwingEndFraction, fadeEnd, progress);
            if (sweepProgress > 0f && fade > 0f)
            {
                // Both the blade and crescent use the same signed sweep, including the backhand.
                float trailSweep = Mathf.Min(settings.lightArc * sweepProgress, 85f);
                Color color = weapon.ComboIndex == 2 ? new Color(1f, 0.8f, 0.3f, fade)
                    : new Color(1f, 0.96f, 0.78f, fade);
                DrawArc(transform.position, weapon.LightReach, sweepAngle - direction * trailSweep,
                    direction * trailSweep, color);
                if (arc != null)
                {
                    arc.widthCurve = slashWidth;
                    arc.widthMultiplier = weapon.ComboIndex == 2 ? 0.23f : 0.17f;
                    arc.startColor = new Color(color.r, color.g, color.b, fade * 0.3f);
                }
            }
            return poseAngle;
        }

        private void DrawArc(Vector3 center, float radius, float startAngle, float sweep, Color color)
        {
            if (arc == null)
                return;
            arc.enabled = true;
            arc.widthCurve = ringWidth;
            arc.widthMultiplier = 0.045f;
            arc.positionCount = 33;
            arc.startColor = color;
            arc.endColor = color;
            for (int i = 0; i < 33; i++)
            {
                float angle = (startAngle + sweep * i / 32f) * Mathf.Deg2Rad;
                arc.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }
        }
    }
}
