namespace TheLostShrine.Combat
{
    // Actions ask for a budget; the resource owns depletion and recovery rules.
    public interface IStamina
    {
        bool TrySpend(float amount);
        void DelayRecovery();
    }
}
