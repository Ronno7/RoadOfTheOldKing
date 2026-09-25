using System;
using TheLostShrine.Combat;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Player
{
    // First action-art sample. Locomotion remains the sole writer of the body pose.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerCombatController))]
    public sealed class PlayerChopAnimation : MonoBehaviour
    {
        [Serializable]
        private struct Pose
        {
            public Sprite sprite;
            public Vector2 grip;
            public float angle;
        }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Pose[] poses = Array.Empty<Pose>();
        [SerializeField] private Sprite recoverySprite;
        [SerializeField] private Vector2 recoveryGrip = new Vector2(0.246f, 0.5f);
        [SerializeField, Range(0f, 0.1f)] private float hitPause = 0.045f;
        private PlayerCombatController combat;
        private HatchetWeapon observedWeapon;

        public bool IsPlaying => isActiveAndEnabled && body != null && poses.Length == 4 &&
            combat != null && combat.Weapon != null && combat.Weapon.State == HatchetState.LightChop &&
            combat.Weapon.AttackDirection.x > 0f &&
            Mathf.Abs(combat.Weapon.AttackDirection.x) >= Mathf.Abs(combat.Weapon.AttackDirection.y);

        public float MovementScale => IsPlaying
            ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1f, combat.Weapon.AttackProgress)) : 1f;

        private void Awake() => combat = GetComponent<PlayerCombatController>();
        private void OnEnable()
        {
            combat.WeaponEquipped += ObserveWeapon;
            ObserveWeapon();
        }
        private void ObserveWeapon()
        {
            if (observedWeapon != null) observedWeapon.HitConfirmed -= OnHit;
            observedWeapon = combat.Weapon;
            if (observedWeapon != null) observedWeapon.HitConfirmed += OnHit;
        }
        private void OnDisable()
        {
            combat.WeaponEquipped -= ObserveWeapon;
            if (observedWeapon != null) observedWeapon.HitConfirmed -= OnHit;
        }
        private void OnHit(CombatHit hit)
        {
            if (IsPlaying && hit.Kind == AttackKind.LightChop)
                combat.Weapon.PauseOnImpact(hitPause);
        }

        private Pose CurrentPose()
        {
            float t = combat.Weapon.AttackProgress;
            float contact = combat.Weapon.Settings.lightWindupFraction;
            float end = combat.Weapon.Settings.lightSwingEndFraction;
            if (t >= 0.88f)
                return new Pose { sprite = recoverySprite, grip = recoveryGrip, angle = -28f };
            int index = t < contact * 0.5f ? 0 : t < contact ? 1 : t < end ? 2 : 3;
            return poses[index];
        }

        public bool TryApplyBody()
        {
            if (!IsPlaying) return false;
            body.sprite = CurrentPose().sprite;
            return true;
        }

        public bool TryApplyWeapon(Transform model, SpriteRenderer blade)
        {
            if (!IsPlaying || model == null || blade == null) return false;
            var pose = CurrentPose();
            // The hand is baked into each cel: never interpolate its anchor independently.
            float t = combat.Weapon.AttackProgress;
            float contact = combat.Weapon.Settings.lightWindupFraction;
            float end = combat.Weapon.Settings.lightSwingEndFraction;
            float angle = pose.angle;
            if (t >= contact && t < end)
                angle = Mathf.Lerp(pose.angle, -112f, Mathf.InverseLerp(contact, end, t));
            model.SetPositionAndRotation(body.transform.TransformPoint(pose.grip),
                body.transform.rotation * Quaternion.Euler(0f, 0f, angle));
            blade.flipX = true;
            blade.sortingLayerID = body.sortingLayerID;
            blade.sortingOrder = body.sortingOrder - 1;
            return true;
        }
    }
}
