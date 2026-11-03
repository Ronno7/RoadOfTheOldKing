using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // The drawn ground under a heavy attack (see EnemySlam.shader): a ring for a full circle, a wedge for an arc, which
    // follows his facing until it locks. Replaces the attack outline on an enemy that has one; charges keep their lanes.
    // It reads the brain's clock and reach and drives nothing.
    [DisallowMultipleComponent, RequireComponent(typeof(PursuitBrain))]
    public sealed class EnemySlamView : MonoBehaviour
    {
        // On the ground, over every painted detail, under props and bodies.
        private const string GroundLayer = "Ground";
        private const int GroundOrder = 90;
        // How far out the streaks of a draw run; the pull itself reaches anything it can see.
        private const float DrawStreakReach = 18f;
        private static readonly int CenterId = Shader.PropertyToID("_Center"), RadiusId = Shader.PropertyToID("_Radius"),
            WindupId = Shader.PropertyToID("_Windup"), ImpactId = Shader.PropertyToID("_Impact"), DrawId = Shader.PropertyToID("_Draw"),
            ArcId = Shader.PropertyToID("_Arc"), FacingId = Shader.PropertyToID("_Facing"), LockedId = Shader.PropertyToID("_Locked");

        [SerializeField] private Shader shader;
        [Tooltip("How long the split ground glows and fades after the blow lands.")]
        [SerializeField, Min(.1f)] private float afterglow = 1.1f;

        private PursuitBrain enemy;
        private Transform quad;
        private MeshRenderer view;
        private Material material;
        private float sinceImpact = -1f;

        private void Awake()
        {
            enemy = GetComponent<PursuitBrain>();
            if (shader == null) { enabled = false; return; }
            material = new Material(shader);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Attack ground";
            Destroy(go.GetComponent<Collider>());
            quad = go.transform;
            // Not parented: the ground stays where it cracked while the body moves on.
            view = go.GetComponent<MeshRenderer>();
            view.sharedMaterial = material;
            view.sortingLayerName = GroundLayer;
            view.sortingOrder = GroundOrder;
            view.enabled = false;
        }

        private void OnDisable()
        {
            sinceImpact = -1f;
            if (view != null) view.enabled = false;
        }

        private void OnDestroy()
        {
            if (quad != null) Destroy(quad.gameObject);
            if (material != null) Destroy(material);
        }

        private void LateUpdate()
        {
            bool drawn = !enemy.IsChargeAttack;
            bool windup = drawn && enemy.State == PursuitState.Windup;
            bool strike = drawn && enemy.State == PursuitState.Striking;
            if (windup)
            {
                sinceImpact = -1f;
                Place(enemy.AttackOrigin);
            }
            else if (strike && sinceImpact < 0f)
            {
                sinceImpact = 0f;
                Place(enemy.AttackOrigin);
            }
            else if (sinceImpact >= 0f)
            {
                sinceImpact += Time.deltaTime;
                if (sinceImpact >= afterglow) sinceImpact = -1f;
            }
            bool showing = windup || sinceImpact >= 0f;
            view.enabled = showing;
            if (!showing) return;
            material.SetFloat(DrawId, enemy.IsDrawing ? DrawStreakReach : 0f);
            material.SetFloat(LockedId, enemy.IsLaneLocked ? 1f : 0f);
            material.SetFloat(WindupId, windup ? enemy.StateProgress : 1f);
            // Slightly past zero the moment it lands, since zero means not yet.
            material.SetFloat(ImpactId, sinceImpact >= 0f ? Mathf.Max(.001f, sinceImpact / afterglow) : 0f);
        }

        private void Place(Vector2 center)
        {
            float size = (enemy.IsDrawing ? DrawStreakReach : enemy.AttackReach) * 2f + .25f;
            quad.position = center;
            quad.localScale = new Vector3(size, size, 1f);
            material.SetVector(CenterId, center);
            material.SetFloat(RadiusId, enemy.AttackReach);
            // In turns of a circle: half the arc either side of the facing, and where the facing points.
            material.SetFloat(ArcId, Mathf.Min(enemy.AttackArc, 360f) / 720f);
            Vector2 facing = enemy.FacingDirection;
            material.SetFloat(FacingId, Mathf.Atan2(facing.y, facing.x) / (Mathf.PI * 2f) + .5f);
        }
    }
}
