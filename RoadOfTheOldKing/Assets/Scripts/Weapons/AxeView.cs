using UnityEngine;

namespace TheLostShrine.Weapons
{
    [DisallowMultipleComponent, RequireComponent(typeof(AxeWeapon))]
    [DefaultExecutionOrder(200)]
    public sealed class AxeView : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private SpriteRenderer blade;
        [SerializeField] private LineRenderer arc;
        [SerializeField] private TrailRenderer trail;
        [Tooltip("Production detached presentation: one scaled sprite centred on the flight position. " +
            "Held placement stays procedural until per-frame hand anchors exist.")]
        [SerializeField] private bool useAnimatedGrip;
        [Tooltip("While held out of combat, let the owner's PlayerWeaponCarry hold it at the hero's side.")]
        [SerializeField] private bool carryOnPlayer;
        [SerializeField] private float spriteAngleOffset;
        [SerializeField] private Vector3 groundVisualOffset;
        [SerializeField] private float groundAngle = 35f;
        [SerializeField] private bool bobOnGround = true;
        [Header("Detached presentation")]
        [SerializeField, Min(0.01f)] private float detachedScale = 1.15f;
        [SerializeField, Min(0.1f)] private float rotationsPerSecond = 7f;
        private Vector3 originalBladePosition;
        private AxeWeapon weapon;
        private Color originalBlade;
        private Sprite originalSprite;
        private Vector3 originalBladeScale;
        private bool wasFlying;
        private int originalSortingLayer;
        private int originalSortingOrder;
        private TheLostShrine.Player.PlayerCombatController carryOwner;
        private TheLostShrine.Player.PlayerWeaponCarry carry;
        private readonly AnimationCurve ringWidth = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        private void Awake()
        {
            weapon = GetComponent<AxeWeapon>();
            if (blade != null)
            {
                originalBlade = blade.color;
                originalSprite = blade.sprite;
                originalBladeScale = blade.transform.localScale;
                originalBladePosition = blade.transform.localPosition;
                originalSortingLayer = blade.sortingLayerID;
                originalSortingOrder = blade.sortingOrder;
            }
        }

        private void LateUpdate()
        {
            if (model == null || weapon.Settings == null)
                return;
            bool flying = weapon.State == AxeState.Flying || weapon.State == AxeState.Returning;
            if (trail != null)
            {
                if (flying != wasFlying)
                    trail.Clear();
                trail.emitting = flying;
            }
            wasFlying = flying;
            if (arc != null)
                arc.enabled = false;
            if (weapon.ThrowPhase == ThrowPhase.Aim && weapon.Owner != null)
                DrawThrowAim();
            if (blade != null)
            {
                blade.color = Color.Lerp(originalBlade, new Color(1f, 0.85f, 0.25f), weapon.Charge01);
                blade.sprite = originalSprite;
                blade.enabled = true;
                blade.transform.localScale = originalBladeScale;
                blade.transform.localPosition = originalBladePosition;
                blade.flipX = false;
                blade.sortingLayerID = originalSortingLayer;
                blade.sortingOrder = originalSortingOrder;
            }

            if (useAnimatedGrip && weapon.IsAway && blade != null && originalSprite != null)
            {
                DrawDetached();
                return;
            }

            if (carryOnPlayer && weapon.State == AxeState.Held && !weapon.IsThrowing && weapon.Owner != null)
            {
                transform.position = weapon.Owner.transform.position;
                if (carryOwner != weapon.Owner)
                {
                    carryOwner = weapon.Owner;
                    carry = carryOwner.GetComponent<TheLostShrine.Player.PlayerWeaponCarry>();
                }
                if (carry != null && carry.TryApply(model, blade))
                    return;
            }

            float angle = Mathf.Atan2(weapon.AimDirection.y, weapon.AimDirection.x) * Mathf.Rad2Deg;
            model.localPosition = Vector3.zero;
            if (weapon.Owner != null && !weapon.IsAway)
            {
                transform.position = weapon.Owner.transform.position;
                model.localPosition = (Vector3)weapon.AimDirection * 0.5f;
            }

            switch (weapon.State)
            {
                case AxeState.OnGround:
                    angle = groundAngle;
                    model.localPosition = groundVisualOffset;
                    if (bobOnGround)
                    {
                        model.localPosition += Vector3.up * (0.06f + Mathf.Sin(Time.time * 3f) * 0.04f);
                        DrawArc(transform.position, 0.55f, 0f, 360f, new Color(1f, 0.8f, 0.3f, 0.55f));
                    }
                    break;
                case AxeState.LightChop:
                    angle = DrawLightSlash();
                    break;
                case AxeState.Charging:
                    angle += Mathf.Sin(Time.time * 35f) * weapon.Charge01 * 8f;
                    DrawArc(transform.position, 0.75f, -90f, 360f * weapon.Charge01,
                        weapon.Charge01 >= 1f ? new Color(1f, 0.8f, 0.1f) : new Color(0.7f, 0.85f, 1f));
                    break;
                case AxeState.Cleaving:
                    angle += 360f * weapon.AttackProgress;
                    model.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.1f;
                    DrawArc(transform.position, weapon.CleaveRadius, angle - 300f, 300f,
                        new Color(1f, 0.75f, 0.2f, 0.85f));
                    break;
                case AxeState.Flying:
                case AxeState.Returning:
                    angle -= weapon.FlightSeconds * rotationsPerSecond * 360f;
                    break;
                case AxeState.Stuck:
                    angle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg - 25f;
                    break;
            }
            model.localRotation = Quaternion.Euler(0f, 0f, angle + spriteAngleOffset);
        }

        private void DrawDetached()
        {
            // The same top-down sprite, scale and center survive launch, impact and Recall.
            // Do not lift the prop above its collision point and snap it down when it stops.
            float angle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
            angle += spriteAngleOffset;
            angle += weapon.State == AxeState.Stuck
                ? -25f : -weapon.FlightSeconds * rotationsPerSecond * 360f;
            model.SetPositionAndRotation(weapon.transform.position, Quaternion.Euler(0f, 0f, angle));

            // The pickup's pivot is at its grip. Center detached rotation without changing
            // the imported pivot used by walking, authored hands or the stump pickup.
            blade.transform.localScale = originalBladeScale * detachedScale;
            blade.transform.localPosition = -Vector3.Scale(originalSprite.bounds.center, blade.transform.localScale);
        }

        private float DrawLightSlash()
        {
            var settings = weapon.Settings;
            float progress = weapon.AttackProgress;
            float direction = weapon.ComboIndex == 1 ? -1f : 1f;
            float aimAngle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
            float halfArc = weapon.LightArc * 0.5f;
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

            return poseAngle;
        }

        private void DrawThrowAim()
        {
            if (arc == null) return;
            Vector3 origin = weapon.Owner.transform.position;
            Vector3 direction = weapon.AimDirection;
            float distance = weapon.AimDistance;
            if (distance < 0.01f) return;
            arc.enabled = true;
            arc.widthCurve = ringWidth;
            arc.widthMultiplier = 0.025f;
            arc.startColor = new Color(1f, 0.94f, 0.74f, 0.1f);
            arc.endColor = new Color(1f, 0.94f, 0.74f, 0.5f);
            arc.positionCount = 2;
            arc.SetPosition(0, origin);
            arc.SetPosition(1, origin + direction * distance);
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
