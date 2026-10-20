using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Weapon-only body coverage. The root collider still owns movement at the feet.
    [DisallowMultipleComponent, RequireComponent(typeof(CapsuleCollider2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "EnemyHurtbox")]
    public sealed class EnemyHurtbox : MonoBehaviour
    {
        [Tooltip("Body coverage for E, NE, N, NW, W, SW, S, SE. Measured from the native standing art.")]
        [SerializeField] private Vector2[] sizes = new Vector2[0];
        [SerializeField] private Vector2[] offsets = new Vector2[0];
        private Damageable health;
        private IEnemyFacing enemy;
        private CapsuleCollider2D shape;
        private int octant = -1;
        public bool CanReceiveHits => isActiveAndEnabled && health != null && health.IsAlive;

        private void Awake()
        {
            health = GetComponentInParent<Damageable>();
            enemy = GetComponentInParent<IEnemyFacing>();
            shape = GetComponent<CapsuleCollider2D>();
            shape.isTrigger = true;
        }

        private void LateUpdate()
        {
            int facing = Player.DirectionalSpriteAnimation.Octant(
                enemy != null ? enemy.FacingDirection : Vector2.down);
            if (octant == facing || sizes.Length != 8 || offsets.Length != 8) return;
            octant = facing;
            shape.direction = sizes[facing].x > sizes[facing].y ? CapsuleDirection2D.Horizontal : CapsuleDirection2D.Vertical;
            shape.size = sizes[facing];
            shape.offset = offsets[facing];
        }
    }
}
