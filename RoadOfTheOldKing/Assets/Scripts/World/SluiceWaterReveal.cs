using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Presentation only. The sluice saves its final outcome before starting this reveal.
    // Keep the authored collision active until the water has visibly cleared.
    [DisallowMultipleComponent]
    public sealed class SluiceWaterReveal : MonoBehaviour
    {
        [SerializeField] private Material drainingMaterial;
        [SerializeField, Min(.1f)] private float duration = 2.6f;
        [SerializeField] private Texture2D basinDepth;
        [SerializeField] private Vector4 basinBounds;
        private Renderer[] surfaces;
        private Material[] originals;
        private MaterialPropertyBlock properties;
        private float elapsed, bottom, top;
        public bool IsDraining { get; private set; }
        public float Progress => IsDraining ? Mathf.Clamp01(elapsed / duration) : 1f;
        private static readonly int Waterline = Shader.PropertyToID("_Waterline");
        private static readonly int BasinDepth = Shader.PropertyToID("_BasinDepth");
        private static readonly int BasinBounds = Shader.PropertyToID("_BasinBounds");
        private static readonly int UseDepth = Shader.PropertyToID("_UseBasinDepth");
        private static readonly int WaterLevel = Shader.PropertyToID("_WaterLevel");

        private void Cache()
        {
            // MaterialPropertyBlock is not retained by an editor script reload.
            if(properties==null)properties=new MaterialPropertyBlock();
            // A restore may run while renderers are temporarily disabled by an
            // editor geometry audit. Do not retain that empty discovery forever.
            if (surfaces != null && surfaces.Length > 0) return;
            var all = GetComponentsInChildren<Renderer>(true);
            var visible = new System.Collections.Generic.List<Renderer>();
            foreach (var surface in all)
                if (surface.enabled) visible.Add(surface);
            surfaces = visible.ToArray();
            originals = new Material[surfaces.Length];
            bottom = float.PositiveInfinity;
            top = float.NegativeInfinity;
            for (int i = 0; i < surfaces.Length; i++)
            {
                originals[i] = surfaces[i].sharedMaterial;
                bottom = Mathf.Min(bottom, surfaces[i].bounds.min.y);
                top = Mathf.Max(top, surfaces[i].bounds.max.y);
            }
        }

        public void SetVisible(bool visible, bool animate)
        {
            Cache();
            if (visible || !animate || drainingMaterial == null || surfaces.Length == 0)
            {
                RestoreMaterials();
                gameObject.SetActive(visible);
                return;
            }
            if (IsDraining) return;
            elapsed = 0f;
            IsDraining = true;
            gameObject.SetActive(true);
            foreach (var surface in surfaces) surface.sharedMaterial = drainingMaterial;
            DrawWaterline();
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float seconds)
        {
            if (!IsDraining || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            elapsed += seconds;
            if (elapsed >= duration)
            {
                RestoreMaterials();
                gameObject.SetActive(false);
            }
            else DrawWaterline();
        }

        private void DrawWaterline()
        {
            // Small native-pixel undulations keep the receding front from becoming a ruler line.
            float line = Mathf.Lerp(top + .3f, bottom - .3f, Mathf.SmoothStep(0f, 1f, Progress));
            foreach (var surface in surfaces)
            {
                surface.GetPropertyBlock(properties);
                properties.SetFloat(Waterline, line);
                bool depth=basinDepth!=null&&basinBounds.z>0&&basinBounds.w>0;
                properties.SetFloat(UseDepth,depth?1f:0f);
                properties.SetFloat(WaterLevel,1f-Mathf.SmoothStep(0f,1f,Progress));
                if(depth){properties.SetTexture(BasinDepth,basinDepth);properties.SetVector(BasinBounds,basinBounds);}
                surface.SetPropertyBlock(properties);
            }
        }

        private void RestoreMaterials()
        {
            IsDraining = false;
            if (surfaces == null) return;
            for (int i = 0; i < surfaces.Length; i++)
                if (surfaces[i] != null) surfaces[i].sharedMaterial = originals[i];
        }

        private void OnDisable()
        {
            bool finish = IsDraining;
            RestoreMaterials();
            if (finish) gameObject.SetActive(false);
        }
    }
}
