using System;
using TheLostShrine.Combat;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Locomotion owns body writes; this supplies the action cel and its matching grip.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerCombatController))]
    public sealed class PlayerChopAnimation : MonoBehaviour
    {
        [Serializable]
        private struct Pose
        {
            public Sprite sprite;
            public Vector2 grip;
            public float angle;
            public float endAngle;
            public bool flipX;
            public Sprite handOverlay;
            [Tooltip("Optional perspective cel. Its pivot must stay at the weapon grip.")]
            public Sprite weaponSprite;
        }

        [Serializable]
        private sealed class DirectionPoses
        {
            public string name;
            public Pose[] opening = new Pose[4];
            [Tooltip("Optional two-cel preparation for the returning cut.")]
            public Pose[] backhandPreparation = Array.Empty<Pose>();
            public Pose backhand;
            public Pose backhandFollowThrough;
            public Pose finisher;
            public Pose recovery;
        }

        [SerializeField] private SpriteRenderer body;
        [Tooltip("South, East, West, North, matching the locomotion directions.")]
        [SerializeField] private DirectionPoses[] directions = Array.Empty<DirectionPoses>();
        [SerializeField, Range(0f, 0.1f)] private float hitPause = 0.045f;
        private PlayerCombatController combat;
        private HatchetWeapon observedWeapon;
        private SpriteRenderer hand;
        private RegisteredPlayerAnimation registered;

        public int DirectionIndex
        {
            get
            {
                Vector2 aim = combat.Weapon.AttackDirection;
                return Mathf.Abs(aim.x) >= Mathf.Abs(aim.y)
                    ? (aim.x > 0f ? 1 : 2) : (aim.y > 0f ? 3 : 0);
            }
        }

        public bool IsPlaying => isActiveAndEnabled && body != null && directions.Length == 4 &&
            (registered == null || !registered.IsPresentingBody) &&
            combat != null && combat.Weapon != null && combat.Weapon.State == HatchetState.LightChop &&
            directions[DirectionIndex] != null && directions[DirectionIndex].opening.Length == 4;

        public float MovementScale => IsPlaying ? combat.Weapon.ActionMovementScale : 1f;

        private void Awake()
        {
            combat = GetComponent<PlayerCombatController>();
            registered = GetComponent<RegisteredPlayerAnimation>();
            if (body == null) return;
            hand = new GameObject("Combat Hand").AddComponent<SpriteRenderer>();
            hand.transform.SetParent(body.transform, false);
            hand.sharedMaterial = body.sharedMaterial;
            hand.enabled = false;
        }
        private void LateUpdate()
        {
            if (!IsPlaying && hand != null) hand.enabled = false;
        }
        public void HideHandLayer() { if (hand != null) hand.enabled = false; }
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
            if (hand != null) hand.enabled = false;
            combat.WeaponEquipped -= ObserveWeapon;
            if (observedWeapon != null) observedWeapon.HitConfirmed -= OnHit;
        }
        private void OnHit(CombatHit hit)
        {
            if (isActiveAndEnabled && combat.Weapon != null && combat.Weapon.State == HatchetState.LightChop &&
                hit.Kind == AttackKind.LightChop)
                combat.Weapon.PauseOnImpact(hitPause * (combat.Weapon.ComboIndex == 2 ? 1.4f : 1f));
        }

        private Pose ContactPose()
        {
            var set = directions[DirectionIndex];
            return combat.Weapon.ComboIndex == 1 ? set.backhand
                : combat.Weapon.ComboIndex == 2 ? set.finisher : set.opening[2];
        }

        private Pose CurrentPose(out float angleProgress)
        {
            var weapon = combat.Weapon;
            var set = directions[DirectionIndex];
            float t = weapon.AttackProgress;
            float contact = weapon.Settings.lightWindupFraction;
            float end = weapon.Settings.lightSwingEndFraction;
            angleProgress = Mathf.InverseLerp(contact, end, t);
            if (t >= 0.9f) return set.recovery;

            if (weapon.ComboIndex == 1)
            {
                if (t < contact)
                {
                    // Roll the wrist before contact, while the blade is edge-on.
                    if (set.backhandPreparation != null && set.backhandPreparation.Length == 2 &&
                        set.backhandPreparation[0].sprite != null && set.backhandPreparation[1].sprite != null)
                    {
                        int frame = t < contact * 0.5f ? 0 : 1;
                        angleProgress = Mathf.InverseLerp(frame * contact * 0.5f,
                            (frame + 1) * contact * 0.5f, t);
                        return set.backhandPreparation[frame];
                    }
                    return set.opening[3];
                }
                if (t < end + 0.10f) return set.backhand;
                if (set.backhandFollowThrough.sprite != null)
                {
                    angleProgress = Mathf.InverseLerp(end + 0.10f, 0.9f, t);
                    return set.backhandFollowThrough;
                }
                return set.opening[0];
            }

            if (t < contact * 0.5f)
            {
                angleProgress = Mathf.InverseLerp(0f, contact * 0.5f, t);
                return set.opening[0];
            }
            if (t < contact)
            {
                angleProgress = Mathf.InverseLerp(contact * 0.5f, contact, t);
                return set.opening[1];
            }
            if (weapon.ComboIndex == 2 && t < 0.76f) return set.finisher;
            if (t < end) return set.opening[2];
            angleProgress = Mathf.InverseLerp(end, 0.9f, t);
            return set.opening[3];
        }

        public bool TryApplyBody()
        {
            if (!IsPlaying) return false;
            var pose = CurrentPose(out _);
            body.sprite = pose.sprite;
            // An atlas slice of the same fist covers the grip when a cut crosses the chest.
            hand.sprite = pose.handOverlay;
            hand.enabled = pose.handOverlay != null;
            hand.transform.localPosition = pose.grip;
            hand.sortingLayerID = body.sortingLayerID;
            hand.sortingOrder = body.sortingOrder + 2;
            return true;
        }

        public bool TryApplyWeapon(Transform model, SpriteRenderer blade)
        {
            if (!IsPlaying || model == null || blade == null) return false;
            var pose = CurrentPose(out float angleProgress);
            // The blade turns through the cut; the authored fist stays seated on the grip.
            float angle = Mathf.Lerp(pose.angle, pose.endAngle, angleProgress);
            model.SetPositionAndRotation(body.transform.TransformPoint(pose.grip),
                body.transform.rotation * Quaternion.Euler(0f, 0f, angle));
            if (pose.weaponSprite != null) blade.sprite = pose.weaponSprite;
            blade.flipX = pose.flipX;
            blade.sortingLayerID = body.sortingLayerID;
            blade.sortingOrder = body.sortingOrder + (pose.handOverlay != null ? 1 : -1);
            return true;
        }

        public bool TryGetSlash(out Vector3 origin, out float startAngle, out float sweep, out float fade)
        {
            origin = Vector3.zero;
            startAngle = sweep = fade = 0f;
            if (!IsPlaying) return false;
            var weapon = combat.Weapon;
            float t = weapon.AttackProgress;
            float start = weapon.Settings.lightWindupFraction;
            float end = weapon.Settings.lightSwingEndFraction;
            if (t < start || t > end + 0.12f) return false;
            var pose = ContactPose();
            float cut = Mathf.InverseLerp(start, end, t);
            float sign = Mathf.Sign(pose.endAngle - pose.angle);
            sweep = sign * 50f;
            startAngle = Mathf.Lerp(pose.angle, pose.endAngle, cut) + 90f - sweep;
            origin = body.transform.TransformPoint(pose.grip);
            fade = 1f - Mathf.InverseLerp(end, end + 0.12f, t);
            return true;
        }
    }
}
