using System;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    [Serializable]
    public struct EnemyAttackSpec
    {
        public float windup, active, recovery, reach, arc, knockback, stagger;
        public int damage;
        public float chargeSpeed, trackingTime;
        [Tooltip("Hyper-armour once the direction locks, through the active window: hits still damage but cannot interrupt the committed blow.")]
        public bool armoredWhenLocked;
    }

    // Immutable authoring data in play. The runner takes a value copy at commitment.
    [CreateAssetMenu(menuName = "Road of the Old King/Enemy Attack")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "EnemyAttackDefinition")]
    public sealed class EnemyAttackDefinition : ScriptableObject
    {
        public EnemyAttackSpec settings = new EnemyAttackSpec
        {
            windup = .7f, active = .12f, recovery = .95f,
            reach = 1.65f, arc = 85, damage = 18, knockback = 2.4f, stagger = .18f
        };
    }
}
