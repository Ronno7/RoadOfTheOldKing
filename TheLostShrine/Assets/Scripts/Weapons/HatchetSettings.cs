using UnityEngine;

namespace TheLostShrine.Weapons
{
    [CreateAssetMenu(menuName = "The Lost Shrine/Hatchet Settings")]
    public sealed class HatchetSettings : ScriptableObject
    {
        [Header("Light combo")]
        [Min(0.1f)] public float lightDuration = 0.25f;
        [Min(0.1f)] public float lightRadius = 1.35f;
        [Range(20f, 180f)] public float lightArc = 110f;
        [Min(0f)] public float comboWindow = 0.55f;
        [Min(1)] public int lightDamage = 1;
        [Min(1)] public int finisherDamage = 2;
        [Header("Charged cleave")]
        [Min(0.05f)] public float minimumCharge = 0.2f;
        [Min(0.1f)] public float fullCharge = 0.8f;
        [Min(0.1f)] public float cleaveDuration = 0.45f;
        [Min(0.1f)] public float cleaveRadius = 2.1f;
        [Min(1)] public int cleaveDamage = 4;
        [Min(0f)] public float cleaveKnockback = 6f;
        [Min(0f)] public float cleaveStagger = 0.7f;
        [Header("Flight")]
        [Min(0.1f)] public float throwSpeed = 10f;
        [Min(0.1f)] public float recallSpeed = 16f;
        [Min(0.5f)] public float throwRange = 6f;
        [Min(0.01f)] public float flightRadius = 0.18f;
        [Min(1)] public int throwDamage = 2;
        [Min(1)] public int recallDamage = 2;
        [Min(0.1f)] public float retrieveDistance = 0.9f;

        private void OnValidate() => fullCharge = Mathf.Max(minimumCharge, fullCharge);
    }
}
