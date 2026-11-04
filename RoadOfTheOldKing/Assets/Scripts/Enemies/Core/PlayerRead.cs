using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Weapons;

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
        public bool StaminaBelow(float limit) => stamina != null && stamina.Current < limit;
    }
}
