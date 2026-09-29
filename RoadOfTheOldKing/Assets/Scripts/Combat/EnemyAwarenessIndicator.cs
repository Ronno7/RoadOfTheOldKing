using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Combat
{
    // World-space pixel UI. Detection belongs to the enemy; this only presents its transition.
    [DisallowMultipleComponent, RequireComponent(typeof(SimpleMeleeEnemy))]
    [DefaultExecutionOrder(310)]
    public sealed class EnemyAwarenessIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField, Min(.1f)] private float duration = .75f;
        [Tooltip("Clearance above the body bounds, leaving room for the health bar.")]
        [SerializeField, Min(0f)] private float gap = .375f;
        private SimpleMeleeEnemy enemy;
        private MeshRenderer display;
        private Mesh mesh;
        private float remaining;
        private int resetVersion;
        private const float Pixel = 1f / 16f;

        private void Awake()
        {
            enemy = GetComponent<SimpleMeleeEnemy>();
            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body == null) { enabled = false; return; }
            var child = new GameObject("Enemy awareness !");
            child.layer = gameObject.layer;
            child.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Pixel awareness mark" };
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            display = child.AddComponent<MeshRenderer>();
            display.sharedMaterial = body.sharedMaterial;
            display.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            display.receiveShadows = false;
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_MainTex", Texture2D.whiteTexture);
            display.SetPropertyBlock(properties);
            display.sortingLayerName = "Player";
            display.sortingOrder = 30001;
            display.enabled = false;
            BuildMark();
        }

        private void OnEnable()
        {
            if (enemy == null) return;
            resetVersion = enemy.ResetVersion;
            enemy.PlayerDetected += OnDetected;
        }

        private void OnDetected()
        {
            resetVersion = enemy.ResetVersion;
            remaining = duration;
        }

        private void LateUpdate() => Present(Time.deltaTime);

        private void Present(float deltaTime)
        {
            if (display == null) return;
            if (!enemy.isActiveAndEnabled || !enemy.IsAware || resetVersion != enemy.ResetVersion)
            {
                remaining = 0f;
                resetVersion = enemy.ResetVersion;
            }
            display.enabled = remaining > 0f && body.enabled && body.gameObject.activeInHierarchy;
            if (!display.enabled) return;
            // One-pixel settle instead of smooth scaling or blur; keep the glyph upright.
            float pop = remaining > duration - .12f ? Pixel : 0f;
            display.transform.SetPositionAndRotation(new Vector3(body.bounds.center.x,
                body.bounds.max.y + gap + pop, transform.position.z), Quaternion.identity);
            remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));
        }

        private void BuildMark()
        {
            string[] rows = { ".###.", "#iii#", "#iii#", "#igi#", "#ggg#", ".#g#.", ".###.", ".....", ".###.", ".#i#.", ".###." };
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();
            for (int y = 0; y < rows.Length; y++) for (int x = 0; x < 5; x++)
            {
                char ink = rows[y][x];
                if (ink == '.') continue;
                Color color = ink == '#' ? new Color32(40, 35, 35, 255)
                    : ink == 'i' ? new Color32(246, 230, 181, 255) : new Color32(202, 158, 79, 255);
                float left = (x - 2.5f) * Pixel, bottom = (rows.Length - y - 1) * Pixel;
                int first = vertices.Count;
                vertices.Add(new Vector3(left, bottom)); vertices.Add(new Vector3(left, bottom + Pixel));
                vertices.Add(new Vector3(left + Pixel, bottom + Pixel)); vertices.Add(new Vector3(left + Pixel, bottom));
                for (int i = 0; i < 4; i++) colors.Add(color);
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
                indices.Add(first); indices.Add(first + 2); indices.Add(first + 3);
            }
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
        }

        private void OnDisable()
        {
            if (enemy != null) enemy.PlayerDetected -= OnDetected;
            remaining = 0f;
            if (display != null) display.enabled = false;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (display != null) Destroy(display.gameObject);
        }
    }
}
