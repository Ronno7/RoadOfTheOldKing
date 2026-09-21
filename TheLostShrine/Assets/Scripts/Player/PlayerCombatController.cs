using System;
using TheLostShrine.Input;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private bool recallUnlocked;
        [SerializeField, Min(0f)] private float lightInputBuffer = 0.16f;
        private ICombatInput input;
        private float queuedLightUntil = -1f;

        public HatchetWeapon Weapon { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.down;
        public bool CanRecall => recallUnlocked;
        public event Action WeaponEquipped;
        public event Action RecallUnlocked;

        private void Awake()
        {
            if (inputSource == null)
                inputSource = GetComponent<ICombatInput>() as MonoBehaviour;
            input = inputSource as ICombatInput;
            if (input == null)
            {
                Debug.LogError("PlayerCombatController needs an ICombatInput component.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            CombatInputFrame frame = inputSource != null && inputSource.isActiveAndEnabled
                ? input.Read() : default;
            if (!frame.Active)
            {
                queuedLightUntil = -1f;
                if (Weapon != null)
                    Weapon.CancelCharge();
                return;
            }

            UpdateAim(frame.PointerPosition);
            if (Weapon == null)
                return;
            Weapon.SetAim(AimDirection);

            if (frame.ThrowPressed)
            {
                queuedLightUntil = -1f;
                if (Weapon.IsAway)
                {
                    if (CanRecall)
                        Weapon.TryRecall();
                }
                else
                    Weapon.TryThrow(AimDirection);
                return;
            }
            if (frame.ChargePressed)
            {
                queuedLightUntil = -1f;
                Weapon.TryBeginCharge();
            }
            if (frame.ChargeReleased)
                Weapon.TryReleaseCharge(AimDirection);
            else if (Weapon.State == HatchetState.Charging && !frame.ChargeHeld)
                Weapon.CancelCharge();

            if (frame.LightPressed && (Weapon.State == HatchetState.Held || Weapon.State == HatchetState.LightChop))
                queuedLightUntil = Time.time + lightInputBuffer;
            if (queuedLightUntil >= Time.time && Weapon.TryLightChop(AimDirection))
                queuedLightUntil = -1f;
        }

        private void UpdateAim(Vector2 pointer)
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
            if (aimCamera == null)
                return;
            Ray ray = aimCamera.ScreenPointToRay(pointer);
            var plane = new Plane(Vector3.forward, transform.position);
            if (!plane.Raycast(ray, out float distance))
                return;
            Vector2 direction = ray.GetPoint(distance) - transform.position;
            if (direction.sqrMagnitude > 0.01f)
                AimDirection = direction.normalized;
        }

        public bool TryEquip(HatchetWeapon weapon)
        {
            if (Weapon != null || weapon == null || !weapon.TryEquip(this))
                return false;
            Weapon = weapon;
            Weapon.SetAim(AimDirection);
            WeaponEquipped?.Invoke();
            return true;
        }

        public void UnlockRecall()
        {
            if (recallUnlocked)
                return;
            recallUnlocked = true;
            RecallUnlocked?.Invoke();
        }

        private void OnDisable()
        {
            queuedLightUntil = -1f;
            if (Weapon != null)
                Weapon.CancelCharge();
        }
    }
}
