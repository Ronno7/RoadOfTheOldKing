namespace RoadOfTheOldKing.Combat
{
    // What shared systems (encounter signal, awareness cue, impact feedback, dev panel) need from any
    // enemy AI. Enemies register themselves with EnemyRegistry while enabled.
    public interface IEnemy
    {
        bool IsAware { get; }
        bool IsDefeated { get; }
        // Increments on rest/reset so presenters can clear transient effects.
        int ResetVersion { get; }
        // Raised on real detection or reacquisition of the player.
        event System.Action PlayerDetected;
    }
}
