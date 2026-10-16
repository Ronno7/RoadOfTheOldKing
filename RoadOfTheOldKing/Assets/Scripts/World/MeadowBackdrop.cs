using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // Render-only overscan: serialize one brush and rectangle instead of thousands of identical cells.
    // The temporary native Tilemap preserves RuleTile variation, material, batching and grid alignment.
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MeadowBackdrop : MonoBehaviour
    {
        [SerializeField] private RectInt area;
        [SerializeField] private TileBase grass;
        [SerializeField] private Material material;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private string sortingLayer = "Ground";
        [SerializeField] private int sortingOrder = -1;

        private GameObject generated;

        private void OnEnable() => Rebuild();

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RebuildIfActive;
#endif
            Release();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Inspector validation can run during deserialization; create scene objects on the next editor tick.
            UnityEditor.EditorApplication.delayCall -= RebuildIfActive;
            UnityEditor.EditorApplication.delayCall += RebuildIfActive;
        }

        private void RebuildIfActive()
        {
            if (this != null && isActiveAndEnabled) Rebuild();
        }
#endif

        [ContextMenu("Rebuild Backdrop")]
        public void Rebuild()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RebuildIfActive;
#endif
            Release();
            if (!isActiveAndEnabled || grass == null || material == null || area.width <= 0 || area.height <= 0) return;

            generated = new GameObject("Meadow Backdrop") { hideFlags = HideFlags.HideAndDontSave };
            generated.transform.SetParent(transform, false);
            var map = generated.AddComponent<Tilemap>();
            map.color = color;
            var renderer = generated.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.mode = TilemapRenderer.Mode.Chunk;
            renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var tiles = new TileBase[area.width * area.height];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = grass;
            map.SetTilesBlock(new BoundsInt(area.x, area.y, 0, area.width, area.height, 1), tiles);
            map.CompressBounds();
        }

        private void Release()
        {
            if (generated == null) return;
            generated.SetActive(false);
            if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
            generated = null;
        }
    }
}
