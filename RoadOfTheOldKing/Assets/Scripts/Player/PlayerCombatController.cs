using System;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Input;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerStamina))]
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private bool recallUnlocked;
        [Tooltip("The axe this player carries once owned. Taking the axe is a one-time pickup; after that each scene's player spawns its own copy on load.")]
        [SerializeField] private AxeWeapon ownedAxePrefab;
        [SerializeField, Min(0f)] private float lightInputBuffer = 0.25f;
        [Tooltip("Charge share after which the charged cleave has hyper-armor (hits still hurt but cannot stagger), through the spin.")]
        [SerializeField, Range(0f, 1f)] private float heavyArmorCharge = 0.5f;
        private ICombatInput input;
        private float queuedLightUntil = -1f;
        private PlayerDash dash;
        private PlayerFlask flask;
        private PlayerHealth health;
        private HitReaction reaction;
        private enum ThrowIntent { None, Throw, Recall, Consumed }
        private ThrowIntent throwIntent;
        private bool suppressCharge;
        private bool controlsActive = true;

        public AxeWeapon Weapon { get; private set; }
        public Vector2 AimDirection { get; private set; } = Vector2.down;
        public bool CanRecall => recallUnlocked;
        public IStamina Stamina { get; private set; }
        public bool IsAttacking => Weapon != null && Weapon.IsAttacking;
        public bool CanContinueAction => isActiveAndEnabled && controlsActive &&
            inputSource != null && inputSource.isActiveAndEnabled && Time.timeScale > 0f &&
            (health == null || health.IsAlive) && (reaction == null || !reaction.IsStaggered);
        public bool CanStartAttack => CanContinueAction && (dash == null || !dash.IsDashing) && (flask == null || !flask.IsDrinking);
        public bool CanCancelThrowAim => Weapon != null && Weapon.CanCancelThrowAim;
        public bool ControlsMovement => Weapon != null && Weapon.ControlsMovement;
        public float ActionMovementScale => Weapon != null ? Weapon.ActionMovementScale : 1f;
        public Vector2 ActionFacing => Weapon != null ? Weapon.ActionFacing : AimDirection;
        public event Action WeaponEquipped;
        public event Action RecallUnlocked;

        private void Awake()
        {
            Stamina = GetComponent<IStamina>();
            dash = GetComponent<PlayerDash>();
            flask = GetComponent<PlayerFlask>();
            health = GetComponent<PlayerHealth>();
            reaction = GetComponent<HitReaction>();
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
            ProcessInput(frame);
            // The heavy attack trades: once committed it cannot be interrupted.
            if (reaction != null) reaction.Armored = Weapon != null && (Weapon.State == AxeState.Cleaving ||
                (Weapon.State == AxeState.Charging && Weapon.Charge01 >= heavyArmorCharge));
        }

        private void ProcessInput(CombatInputFrame frame)
        {
            controlsActive = frame.Active;
            if (!CanContinueAction)
            {
                ClearActionInput();
                return;
            }

            UpdateAim(frame.PointerPosition);
            if (Weapon == null)
                return;
            Weapon.SetAim(AimDirection);

            // Bind the whole E press/release cycle to the intent at key-down. Catching an
            // away weapon while E is held must never reinterpret key-up as a new throw.
            bool throwInput = frame.ThrowPressed || frame.ThrowReleased || throwIntent != ThrowIntent.None;
            if (frame.ThrowPressed && throwIntent == ThrowIntent.None)
            {
                queuedLightUntil = -1f;
                throwIntent = ThrowIntent.Consumed;
                if (Weapon.IsAway)
                {
                    throwIntent = ThrowIntent.Recall;
                    if (CanRecall) Weapon.TryPlayerRecall();
                }
                else if (CanStartAttack && Weapon.TryBeginThrow(AimDirection))
                    throwIntent = ThrowIntent.Throw;
            }

            // RMB is cancellation while aiming; consume its release as well as its press.
            if (CanCancelThrowAim && frame.ChargePressed)
            {
                CancelThrowAim();
                suppressCharge = true;
            }
            if (frame.ThrowReleased)
            {
                if (throwIntent == ThrowIntent.Throw && CanStartAttack)
                    Weapon.TryReleaseThrow(AimDirection);
                throwIntent = ThrowIntent.None;
            }
            else if (!frame.ThrowHeld && !frame.ThrowPressed)
            {
                // Lost release edges (focus, disabled maps) cancel, they never launch.
                if (throwIntent == ThrowIntent.Throw) Weapon.CancelThrow();
                throwIntent = ThrowIntent.None;
            }

            bool chargeBlocked = suppressCharge;
            if (!frame.ChargeHeld && !frame.ChargePressed) suppressCharge = false;
            bool actionInputBlocked = !CanStartAttack || Weapon.IsThrowing || throwInput;
            if (!actionInputBlocked && frame.ChargePressed && !chargeBlocked)
            {
                queuedLightUntil = -1f;
                Weapon.TryBeginCharge();
            }
            // A rejected E press must not swallow the release of an existing cleave.
            if (Weapon.State == AxeState.Charging)
            {
                if (frame.ChargeReleased && !chargeBlocked) Weapon.TryReleaseCharge(AimDirection);
                else if (!frame.ChargeHeld) Weapon.CancelCharge();
            }
            if (actionInputBlocked)
            {
                queuedLightUntil = -1f;
                return;
            }
            if (frame.LightPressed && (Weapon.State == AxeState.Held || Weapon.State == AxeState.LightChop))
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

        public bool TryEquip(AxeWeapon weapon)
        {
            if (Weapon != null || weapon == null || !weapon.TryEquip(this))
                return false;
            Weapon = weapon;
            Weapon.SetAim(AimDirection);
            WeaponEquipped?.Invoke();
            return true;
        }

        // Spawns and equips the player's own axe (owned saves, scene arrivals). No-op when already armed.
        public bool EquipOwnedAxe()
        {
            if (Weapon != null) return true;
            if (ownedAxePrefab == null) return false;
            var axe = Instantiate(ownedAxePrefab, transform.position, Quaternion.identity);
            axe.name = ownedAxePrefab.name;
            if (TryEquip(axe)) return true;
            Destroy(axe.gameObject);
            return false;
        }

        public void UnlockRecall()
        {
            if (recallUnlocked)
                return;
            recallUnlocked = true;
            RecallUnlocked?.Invoke();
        }

        public void CancelThrowAim()
        {
            if (!CanCancelThrowAim) return;
            Weapon.CancelThrow();
            throwIntent = ThrowIntent.Consumed;
            queuedLightUntil = -1f;
        }

        private void ClearActionInput()
        {
            queuedLightUntil = -1f;
            throwIntent = ThrowIntent.Consumed;
            suppressCharge = true;
            if (Weapon != null)
            {
                Weapon.CancelLightCombo();
                Weapon.CancelCharge();
                Weapon.CancelThrow();
            }
        }

        private void OnDisable() { controlsActive = false; ClearActionInput(); if (reaction != null) reaction.Armored = false; }
        private void OnApplicationFocus(bool focused) { if (!focused) { controlsActive = false; ClearActionInput(); } }
        private void OnApplicationPause(bool paused) { if (paused) { controlsActive = false; ClearActionInput(); } }
    }
}
