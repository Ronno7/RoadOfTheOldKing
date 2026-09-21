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
                    float direction = weapon.ComboIndex == 1 ? -1f : 1f;
                    float aimAngle = Mathf.Atan2(weapon.AttackDirection.y, weapon.AttackDirection.x) * Mathf.Rad2Deg;
                    angle = aimAngle + Mathf.Lerp(-weapon.Settings.lightArc * 0.5f, weapon.Settings.lightArc * 0.5f, weapon.AttackProgress) * direction;
                    model.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 0.65f;
                    DrawArc(transform.position, weapon.Settings.lightRadius, aimAngle - weapon.Settings.lightArc * 0.5f,
                        weapon.Settings.lightArc * weapon.AttackProgress, new Color(1f, 0.9f, 0.55f, 0.8f));
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

        private void DrawArc(Vector3 center, float radius, float startAngle, float sweep, Color color)
        {
            if (arc == null)
                return;
            arc.enabled = true;
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
