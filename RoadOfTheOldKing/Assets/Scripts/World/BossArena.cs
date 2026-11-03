using RoadOfTheOldKing.Enemies;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A boss's room. While the boss is awake a wall of fog keeps it inside and shades everything beyond. The player
    // can still walk in, but once inside can't leave until the boss falls or the player dies. Only a seated or
    // fallen boss drops the wall, so any reset (a rest, a death, the player leaving it to go home) opens the way.
    // Put this on an Environment layer object: the walls then stop bodies, throws and sight like any other wall.
    [DisallowMultipleComponent]
    public sealed class BossArena : MonoBehaviour
    {
        [SerializeField] private PursuitBrain boss;
        [SerializeField] private Shader fogShader;
        [Tooltip("The room, centred on this object. The fog stands on its edge.")]
        [SerializeField] private Vector2 size = new Vector2(16f, 16f);
        [SerializeField, Min(.1f)] private float riseSeconds = .8f;
        [SerializeField, Min(.1f)] private float clearSeconds = 2f;

        private const float WallThickness = 2f;
        // Past the fog's own reach in the shader, so the quad never crops it.
        private const float CoverMargin = 40f;
        private static readonly int AreaId = Shader.PropertyToID("_Area"), RiseId = Shader.PropertyToID("_Rise");

        private readonly BoxCollider2D[] walls = new BoxCollider2D[4];
        private Collider2D[] playerBodies = System.Array.Empty<Collider2D>();
        private Rect room;
        private MeshRenderer cover;
        private Material material;
        private float rise;

        public bool IsUp { get; private set; }
        public bool HoldsPlayer { get; private set; }

        private void Awake()
        {
            room = new Rect((Vector2)transform.position - size * .5f, size);
            Vector2 half = (size + Vector2.one * WallThickness) * .5f;
            Vector2 across = new Vector2(WallThickness, size.y + WallThickness * 2f);
            Vector2 along = new Vector2(size.x + WallThickness * 2f, WallThickness);
            AddWall(0, new Vector2(-half.x, 0f), across);
            AddWall(1, new Vector2(half.x, 0f), across);
            AddWall(2, new Vector2(0f, -half.y), along);
            AddWall(3, new Vector2(0f, half.y), along);
            if (fogShader != null) BuildCover();
        }

        private void AddWall(int index, Vector2 offset, Vector2 wallSize)
        {
            var wall = gameObject.AddComponent<BoxCollider2D>();
            wall.offset = offset;
            wall.size = wallSize;
            wall.enabled = false;
            walls[index] = wall;
        }

        private void BuildCover()
        {
            material = new Material(fogShader);
            material.SetVector(AreaId, new Vector4(room.xMin, room.yMin, room.xMax, room.yMax));
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Boss fog";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = new Vector3(size.x + CoverMargin * 2f, size.y + CoverMargin * 2f, 1f);
            cover = quad.GetComponent<MeshRenderer>();
            cover.sharedMaterial = material;
            cover.sortingLayerName = "Player";
            // As the dark interior: over enemy world UI (health 30000), under the player's charge bar and stamina arc.
            cover.sortingOrder = 30003;
            cover.enabled = false;
        }

        private void Update()
        {
            bool awake = boss != null && !boss.IsDormant && !boss.IsDefeated;
            if (awake != IsUp)
            {
                IsUp = awake;
                HoldsPlayer = false;
                if (awake) FindPlayer();
                ApplyWalls();
            }
            if (IsUp && !HoldsPlayer && PlayerInside())
            {
                HoldsPlayer = true;
                ApplyWalls();
            }
            UpdateFog();
        }

        private void FindPlayer()
        {
            var player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
            playerBodies = System.Array.FindAll(player.GetComponentsInChildren<Collider2D>(), body => !body.isTrigger);
        }

        private bool PlayerInside()
        {
            foreach (var body in playerBodies)
                if (body == null || !room.Contains(body.bounds.min) || !room.Contains(body.bounds.max)) return false;
            return playerBodies.Length > 0;
        }

        private void ApplyWalls()
        {
            foreach (var wall in walls)
            {
                wall.enabled = IsUp;
                if (!IsUp) continue;
                foreach (var body in playerBodies)
                    if (body != null) Physics2D.IgnoreCollision(wall, body, !HoldsPlayer);
            }
        }

        private void UpdateFog()
        {
            if (cover == null) return;
            rise = Mathf.MoveTowards(rise, IsUp ? 1f : 0f, Time.deltaTime / (IsUp ? riseSeconds : clearSeconds));
            cover.enabled = rise > 0f;
            material.SetFloat(RiseId, rise);
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.85f, .88f, .78f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
