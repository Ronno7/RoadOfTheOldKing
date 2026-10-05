using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // On the ground (before the first pickup) the model keeps the pose authored in the scene: root and
    // model transforms, e.g. planted in the stump. Every other pose is set in world space, so a root
    // rotated to place the pickup never tilts swings, thrusts or the cleave.
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
        [Tooltip("Bob and ring the model while it lies on the ground (prototype pickups).")]
        [SerializeField] private bool bobOnGround = true;
        [Header("Detached presentation")]
        [SerializeField, Min(0.01f)] private float detachedScale = 1.15f;
        [SerializeField, Min(0.1f)] private float rotationsPerSecond = 7f;
        private Vector3 originalBladePosition;
        private Vector3 groundModelPosition;
        private Quaternion groundModelRotation;
        private AxeWeapon weapon;
        private Color originalBlade;
        private Sprite originalSprite;
        private Vector3 originalBladeScale;
        private float bladeForwardExtent = 0.45f;
        private bool wasFlying;
        private int originalSortingLayer;
        private int originalSortingOrder;
        private RoadOfTheOldKing.Player.PlayerCombatController carryOwner;
        private RoadOfTheOldKing.Player.PlayerWeaponCarry carry;
        private Vector3 offset; // world-space model offset from the root for the current pose
        private readonly AnimationCurve ringWidth = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        private void Awake()
        {
            weapon = GetComponent<AxeWeapon>();
            if (model != null)
            {
                groundModelPosition = model.localPosition;
                groundModelRotation = model.localRotation;
            }
            if (blade != null)
            {
                originalBlade = blade.color;
                originalSprite = blade.sprite;
                originalBladeScale = blade.transform.localScale;
                originalBladePosition = blade.transform.localPosition;
                originalSortingLayer = blade.sortingLayerID;
                originalSortingOrder = blade.sortingOrder;
                if (model != null && originalSprite != null)
                {
                    var bounds = originalSprite.bounds;
                    var rotation = Quaternion.Euler(0f, 0f, spriteAngleOffset);
                    bladeForwardExtent = 0f;
                    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
                    {
                        var corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                            y == 0 ? bounds.min.y : bounds.max.y, 0f);
                        var local = model.InverseTransformPoint(blade.transform.TransformPoint(corner));
                        bladeForwardExtent = Mathf.Max(bladeForwardExtent, (rotation * local).x);
                    }
                }
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
                    carry = carryOwner.GetComponent<RoadOfTheOldKing.Player.PlayerWeaponCarry>();
                }
                if (carry != null && carry.TryApply(model, blade))
                    return;
            }

            if (weapon.State == AxeState.OnGround)
            {
                // The authored pose, relative to the (possibly rotated) root.
                model.localPosition = groundModelPosition;
                model.localRotation = groundModelRotation;
                if (bobOnGround)
                {
                    model.position += Vector3.up * (0.06f + Mathf.Sin(Time.time * 3f) * 0.04f);
                    DrawArc(transform.position, 0.55f, 0f, 360f, new Color(1f, 0.8f, 0.3f, 0.55f));
                }
                return;
            }

            // Held and attack poses: offset and angle in world space around the root.
            float angle = Mathf.Atan2(weapon.AimDirection.y, weapon.AimDirection.x) * Mathf.Rad2Deg;
            offset = Vector3.zero;
            if (weapon.Owner != null && !weapon.IsAway)
            {
                transform.position = weapon.Owner.transform.position;
                offset = (Vector3)weapon.AimDirection * 0.5f;
            }

            switch (weapon.State)
            {
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
                    offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 1.1f;
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
            model.SetPositionAndRotation(transform.position + offset, Quaternion.Euler(0f, 0f, angle + spriteAngleOffset));
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
            float progress = weapon.AttackProgress;
            float direction = weapon.ComboIndex == 1 ? -1f : 1f;
            float aimAngle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
            float halfArc = weapon.LightArc * 0.5f;
            float slashTime = weapon.LightSwingProgress;
            if (weapon.IsLightThrust)
            {
                // Place the tip at the lane end, accounting for the long halberd sprite.
                float extension = Mathf.Max(0.15f, weapon.LightReach - bladeForwardExtent);
                float radius = weapon.LightPhase == MeleePhase.Windup
                    ? Mathf.Lerp(0.5f, 0.15f, progress / weapon.LightWindupFraction)
                    : weapon.LightPhase == MeleePhase.Active ? Mathf.Lerp(0.15f, extension, slashTime)
                    : Mathf.Lerp(extension, 0.5f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(weapon.LightSwingEndFraction, 1f, progress)));
                offset = (Vector3)weapon.AttackDirection * radius;
                return aimAngle;
            }
            float sweepProgress = 1f - Mathf.Pow(1f - slashTime, 3f);
            float sweepAngle = aimAngle + Mathf.Lerp(-halfArc, halfArc, sweepProgress) * direction;
            float poseAngle = sweepAngle;
            float reach = Mathf.Max(0.25f, weapon.LightReach - 0.45f);
            float poseRadius;

            if (progress < weapon.LightWindupFraction)
            {
                float windup = Mathf.InverseLerp(0f, weapon.LightWindupFraction, progress);
                poseAngle = aimAngle - Mathf.Lerp(halfArc * 0.65f, halfArc, windup) * direction;
                poseRadius = Mathf.Lerp(0.5f, 0.6f, windup);
            }
            else if (progress <= weapon.LightSwingEndFraction)
                poseRadius = Mathf.Lerp(0.6f, reach, sweepProgress);
            else
            {
                float recovery = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(weapon.LightSwingEndFraction, 1f, progress));
                poseAngle = Mathf.Lerp(sweepAngle, aimAngle, recovery);
                poseRadius = Mathf.Lerp(reach, 0.5f, recovery);
            }
            offset = new Vector3(Mathf.Cos(poseAngle * Mathf.Deg2Rad), Mathf.Sin(poseAngle * Mathf.Deg2Rad)) * poseRadius;

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
