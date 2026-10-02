using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "Road of the Old King/Axe Settings")]
    public sealed class AxeSettings : ScriptableObject
    {
        [Header("Light combo")]
        [Min(0.1f)] public float lightDuration = 0.2f;
        [Min(1f)] public float finisherDurationMultiplier = 1.5f;
        [Tooltip("Fraction of the swing spent drawing back before the slash can hit.")]
        [Range(0f, 0.4f)] public float lightWindupFraction = 0.12f;
        [Tooltip("The slash and damage window end here; the remaining time is recovery.")]
        [Range(0.2f, 0.85f)] public float lightSwingEndFraction = 0.5f;
        [Min(0.1f)] public float lightRadius = 1.35f;
        [Range(20f, 180f)] public float lightArc = 160f;
        [Range(20f, 180f)] public float secondLightArc = 120f;
        [Header("Finisher thrust")]
        [Tooltip("Lane width is derived from this angle at the tip; the lane has parallel sides.")]
        [Range(5f, 45f)] public float finisherArc = 28f;
        [Min(1f)] public float finisherReachMultiplier = 1.3f;
        [Range(0f, 0.6f)] public float finisherWindupFraction = 0.4f;
        [Range(0.2f, 0.85f)] public float finisherSwingEndFraction = 0.6f;
        [Min(0f)] public float finisherLungeDistance = 0.4f;
        [Min(0f)] public float comboWindow = 0.55f;
        [Tooltip("Share of walking speed kept while a light attack plays, so attacking never roots the player.")]
        [Range(0f, 1f)] public float lightMovementScale = 0.55f;
        [Tooltip("Damage multiplier for an attack whose stamina cost pushed stamina below zero.")]
        [Range(0f, 1f)] public float exhaustedDamageMultiplier = 0.6f;
        [Tooltip("Speed multiplier for a light attack or cleave made into a stamina deficit (0.75 = takes a third longer).")]
        [Range(0.1f, 1f)] public float exhaustedSpeedMultiplier = 0.75f;
        [Min(1)] public int lightDamage = 10;
        [Min(1)] public int finisherDamage = 25;
        [Min(0f)] public float lightStaminaCost = 18f;
        [Min(0f)] public float finisherStaminaCost = 24f;
        [Tooltip("Confirmed light hits pause only this swing's clock; enemies and world time keep running.")]
        [Range(0f, 0.1f)] public float lightHitPause = 0.045f;
        [Min(1f)] public float finisherHitPauseMultiplier = 1.8f;
        [Tooltip("Impulse on a mass-1 target; with damping 8 the slide is about impulse / 8 units.")]
        [Min(0f)] public float lightKnockback = 5.5f;
        [Min(0f)] public float finisherKnockback = 10f;
        [Tooltip("Stun while the knockback slides; enemies add their own short hit recovery after it.")]
        [Min(0f)] public float lightStagger = 0.2f;
        [Min(0f)] public float finisherStagger = 0.3f;
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
        [Header("Throw action (seconds per drawn cel)")]
        public ThrowActionTiming throwAction = new ThrowActionTiming();
        [Header("Flight")]
        [Min(0.1f)] public float throwSpeed = 10f;
        [Min(0.1f)] public float recallSpeed = 16f;
        [Min(0.5f)] public float throwRange = 6f;
        [Tooltip("Recall automatically beyond this distance from the player, once unlocked. Kept beyond throw range.")]
        [Min(1f)] public float autoRecallDistance = 10f;
        [Min(0.01f)] public float flightRadius = 0.18f;
        [Min(1)] public int throwDamage = 15;
        [Min(1)] public int recallDamage = 10;
        [Min(0f)] public float throwKnockback = 4f;
        [Min(0f)] public float recallKnockback = 4f;
        [Min(0f)] public float throwStaminaCost = 20f;
        [Tooltip("Stamina for a Recall the player triggers. Automatic Recall (axe too far away) stays free.")]
        [Min(0f)] public float recallStaminaCost = 10f;
        [Tooltip("After catching a recalled axe: no attack or dodge for this long. Every throw cycle ends with an opening.")]
        [Min(0f)] public float catchRecovery = 0.25f;
        [Min(0.1f)] public float retrieveDistance = 1.4f;

        private void OnValidate()
        {
            fullCharge = Mathf.Max(minimumCharge, fullCharge);
            autoRecallDistance = Mathf.Max(throwRange + 1f, autoRecallDistance);
            lightSwingEndFraction = Mathf.Max(lightWindupFraction + 0.05f, lightSwingEndFraction);
            finisherSwingEndFraction = Mathf.Max(finisherWindupFraction + 0.05f, finisherSwingEndFraction);
            secondLightArc = Mathf.Min(secondLightArc, lightArc);
        }
    }
}
