namespace RoadOfTheOldKing.Combat
{
    // Actions ask for a budget; the resource owns depletion and recovery rules.
    public interface IStamina
    {
        // Discrete actions (attacks, throws, dodges): allowed while stamina is above zero, and the
        // cost may drive it negative down to a floor (Souls-style). Returns false only at or below zero.
        bool TrySpend(float amount);
        // Continuous drains (sprint): need stamina above zero and stop at zero.
        bool TryDrain(float amount);
        // Below zero after an over-budget action: such attacks hit weaker.
        bool IsExhausted { get; }
        void DelayRecovery();
    }
}
