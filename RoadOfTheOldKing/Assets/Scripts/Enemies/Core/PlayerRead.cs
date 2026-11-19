using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Weapons;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // What an enemy can observe the player doing, never what was pressed. Both brains read it, so
    // "committed" means the same thing to all of them.
    public sealed class PlayerRead
    {
        private PlayerHealth bound;
        private PlayerCombatController combat;
        private PlayerStamina stamina;
        private PlayerFlask flask;
        private uint lastFlight;

        public void Bind(PlayerHealth target)
        {
            if (target == bound) return;
            bound = target;
            combat = target != null ? target.GetComponent<PlayerCombatController>() : null;
            stamina = target != null ? target.GetComponent<PlayerStamina>() : null;
            flask = target != null ? target.GetComponent<PlayerFlask>() : null;
        }

        public AxeWeapon Weapon => combat != null ? combat.Weapon : null;

        // A swing that can still hit: light windup/active, charging or cleaving.
        public bool Swinging
        {
            get
            {
                var w = Weapon;
                return w != null && ((w.State == AxeState.LightChop && w.LightPhase != MeleePhase.Recovery) ||
                    w.State == AxeState.Charging || w.State == AxeState.Cleaving);
            }
        }

        // A blow actually coming: a light chop or a released cleave. Holding a charge is not one, so
        // evasion keys on the release rather than the charge start (playtest, 22 Oct).
        public bool Striking
        {
            get
            {
                var w = Weapon;
                return w != null && ((w.State == AxeState.LightChop && w.LightPhase != MeleePhase.Recovery) ||
                    w.State == AxeState.Cleaving);
            }
        }

        // Committed and unable to swing back: drinking, attack recovery, a throw being released, or the axe away.
        public bool Exposed
        {
            get
            {
                var w = Weapon;
                if (combat == null) return false;
                return w == null || w.IsAway || w.IsCatching || (flask != null && flask.IsDrinking) ||
                    (w.State == AxeState.LightChop && w.LightPhase == MeleePhase.Recovery) ||
                    (w.IsThrowing && !w.CanCancelThrowAim);
            }
        }

        public bool AxeAway => Weapon != null && Weapon.IsAway;

        // True once per throw: on the first read after the axe leaves the hand. Read it once a tick.
        public bool ThrowLaunched()
        {
            var w = Weapon;
            bool launched = w != null && w.FlightSequence != lastFlight && w.State == AxeState.Flying;
            if (w != null) lastFlight = w.FlightSequence;
            return launched;
        }

        // The axe's flight line points at this enemy: within a throw's reach (plus a step), and no more than
        // maxAngle off the line to it. stepAside leaves the line to the side the enemy already stands on.
        public bool ThrowAimedAt(float distance, Vector2 toPlayer, float maxAngle, out Vector2 stepAside)
        {
            stepAside = Vector2.zero;
            var w = Weapon;
            if (w == null || distance > w.Settings.throwRange + 1f) return false;
            Vector2 line = w.AttackDirection, fromPlayer = -toPlayer.normalized;
            if (Vector2.Angle(line, fromPlayer) > maxAngle) return false;
            float side = line.x * fromPlayer.y - line.y * fromPlayer.x;
            if (Mathf.Abs(side) < .001f) side = Random.value < .5f ? -1f : 1f;
            stepAside = new Vector2(-line.y, line.x) * Mathf.Sign(side);
            return true;
        }
        public bool StaminaBelow(float limit) => stamina != null && stamina.Current < limit;
    }
}
