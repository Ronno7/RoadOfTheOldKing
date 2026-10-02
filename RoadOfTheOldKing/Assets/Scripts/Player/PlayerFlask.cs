using System;
using TheLostShrine.Combat;
using TheLostShrine.Input;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Estus-style healing flask. A drink is a commitment: the charge is spent at once, the player walks
    // slowly and cannot attack or dodge, and the heal lands late in the drink. A stagger before then
    // wastes the charge. Charges refill at a bonfire rest and on respawn (they are not saved).
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerFlask : MonoBehaviour
    {
        [SerializeField, Min(0)] private int maxCharges = 3;
        [SerializeField, Min(1)] private int healAmount = 40;
        [SerializeField, Min(.1f)] private float drinkDuration = .9f;
        [Tooltip("Share of the drink after which the heal lands.")]
        [SerializeField, Range(0f, 1f)] private float healAt = .65f;
        [Tooltip("Share of walking speed while drinking.")]
        [SerializeField, Range(0f, 1f)] private float moveScale = .35f;
        [Tooltip("A stagger before the heal lands wastes the charge (Dark Souls rule).")]
        [SerializeField] private bool interruptWastesCharge = true;

        private PlayerHealth player;
        private Damageable health;
        private HitReaction reaction;
        private PlayerCombatController combat;
        private PlayerDash dash;
        private ICombatInput input;
        private float elapsed;
        private bool healed;

        public int Charges { get; private set; }
        public int MaxCharges => maxCharges;
        public bool IsDrinking { get; private set; }
        public float MoveScale => moveScale;
        public float Progress => IsDrinking ? Mathf.Clamp01(elapsed / drinkDuration) : 0f;
        public event Action Healed;
        public event Action DrinkRefused;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            health = GetComponent<Damageable>();
            reaction = GetComponent<HitReaction>();
            combat = GetComponent<PlayerCombatController>();
            dash = GetComponent<PlayerDash>();
            input = GetComponent<ICombatInput>();
            Charges = maxCharges;
        }

        private void Update()
        {
            if (input != null && input.Read().HealPressed) TryDrink();
        }

        public bool TryDrink()
        {
            var weapon = combat != null ? combat.Weapon : null;
            bool busy = IsDrinking || !player.IsAlive || Time.timeScale <= 0f ||
                (combat != null && !combat.CanStartAttack) || (weapon != null && weapon.IsAttacking) ||
                (dash != null && dash.IsDashing) || (reaction != null && reaction.IsStaggered);
            if (busy) return false;
            if (Charges <= 0) { DrinkRefused?.Invoke(); return false; }
            Charges--;
            IsDrinking = true;
            healed = false;
            elapsed = 0f;
            return true;
        }

        private void FixedUpdate()
        {
            if (!IsDrinking) return;
            if (!player.IsAlive) { Stop(); return; }
            // Hit out of the drink before the heal: the flask is wasted.
            if (!healed && reaction != null && reaction.IsStaggered)
            {
                if (!interruptWastesCharge) Charges = Mathf.Min(maxCharges, Charges + 1);
                Stop();
                return;
            }
            elapsed += Time.fixedDeltaTime;
            if (!healed && elapsed >= drinkDuration * healAt)
            {
                healed = true;
                health.Heal(healAmount);
                Healed?.Invoke();
            }
            if (elapsed >= drinkDuration) Stop();
        }

        private void Stop()
        {
            IsDrinking = false;
            elapsed = 0f;
        }

        public void Refill()
        {
            Stop();
            Charges = maxCharges;
        }

        private void OnDisable() => Stop();
    }
}
