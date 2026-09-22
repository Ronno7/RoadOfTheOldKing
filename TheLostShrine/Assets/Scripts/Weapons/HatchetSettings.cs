using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "The Lost Shrine/Hatchet Settings")]
    public sealed class HatchetSettings : ScriptableObject
    {
        [Header("Light combo")]
        [Min(0.1f)] public float lightDuration = 0.2f;
        [Min(1f)] public float finisherDurationMultiplier = 1.15f;
        [Tooltip("Fraction of the swing spent drawing back before the slash can hit.")]
        [Range(0f, 0.4f)] public float lightWindupFraction = 0.12f;
        [Tooltip("The slash and damage window end here; the remaining time is recovery.")]
        [Range(0.2f, 0.85f)] public float lightSwingEndFraction = 0.5f;
        [Min(0.1f)] public float lightRadius = 1.35f;
        [Range(20f, 180f)] public float lightArc = 110f;
        [Min(0f)] public float comboWindow = 0.55f;
        [Min(1)] public int lightDamage = 10;
        [Min(1)] public int finisherDamage = 15;
        [Min(0f)] public float lightStaminaCost = 18f;
        [Min(0f)] public float finisherStaminaCost = 24f;
        [Header("Charged cleave")]
        [Min(0.05f)] public float minimumCharge = 0.2f;
        [Min(0.1f)] public float fullCharge = 0.8f;
        [Min(0.1f)] public float cleaveDuration = 0.45f;
        [Min(0.1f)] public float cleaveRadius = 2.1f;
        [Min(1)] public int cleaveDamage = 30;
        [Tooltip("Paid when charging begins, including cancelled charges.")]
        [Min(0f)] public float cleaveStaminaCost = 35f;
        [Min(0f)] public float cleaveKnockback = 6f;
        [Min(0f)] public float cleaveStagger = 0.7f;
        [Header("Flight")]
        [Min(0.1f)] public float throwSpeed = 10f;
        [Min(0.1f)] public float recallSpeed = 16f;
        [Min(0.5f)] public float throwRange = 6f;
        [Tooltip("Recall automatically beyond this distance from the player, once unlocked. Kept beyond throw range.")]
        [Min(1f)] public float autoRecallDistance = 10f;
        [Min(0.01f)] public float flightRadius = 0.18f;
        [Min(1)] public int throwDamage = 15;
        [Min(1)] public int recallDamage = 10;
        [Tooltip("Throw pays for the whole round trip; Recall is always free.")]
        [Min(0f)] public float throwStaminaCost = 25f;
        [Min(0.1f)] public float retrieveDistance = 0.9f;

        private void OnValidate()
        {
            fullCharge = Mathf.Max(minimumCharge, fullCharge);
            autoRecallDistance = Mathf.Max(throwRange + 1f, autoRecallDistance);
            lightSwingEndFraction = Mathf.Max(lightWindupFraction + 0.05f, lightSwingEndFraction);
        }
    }
}
