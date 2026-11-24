using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // An enemy's place in depth (Depth): its body on the Standing layer at its feet, in the middle slot of its row, like
    // the hero, so it draws in front of everything standing farther north and behind everything farther south (user,
    // 23 Nov: at a fixed order an enemy drew over props in front of it and under grass at its feet). Parts that follow
    // the body (EnemyWeaponView's spear, EnemyImpactFeedback) take its order plus or minus a few slots, inside its row.
    // Nothing is written to the transform, whose interpolated Rigidbody2D would snap back to its last physics pose. A
    // corpse stays with EnemyCorpseSorting.
    // Before EnemyWeaponView (210), which reads the body's order.
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public sealed class ActorDepth : MonoBehaviour
    {
        [Tooltip("The body art that sorts; found as the child named Body when empty.")]
        [SerializeField] private SpriteRenderer body;
        private Damageable health;

        public SpriteRenderer Body => body;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            if (body == null)
            {
                var named = transform.Find("Body");
                body = named != null ? named.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        private void LateUpdate()
        {
            if (body == null || (health != null && !health.IsAlive)) return;
            Depth.Apply(body, transform.position.y, Depth.BodySlot);
        }
    }
}
