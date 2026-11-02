using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The dry masonry is permanent. Only the wet-pixel overlay follows the channel reveal.
    [DisallowMultipleComponent, DefaultExecutionOrder(45)]
    public sealed class SluiceDrainView : MonoBehaviour
    {
        [SerializeField] private SluiceWaterReveal channel;
        [SerializeField] private Renderer channelSurface;
        [SerializeField] private SpriteRenderer wetOverlay;

        private Material originalMaterial;
        private bool originalEnabled, cached;
        private MaterialPropertyBlock originalProperties, channelProperties;

        private void Cache()
        {
            if (cached || wetOverlay == null) return;
            originalMaterial = wetOverlay.sharedMaterial;
            originalEnabled = wetOverlay.enabled;
            originalProperties = new MaterialPropertyBlock();
            channelProperties = new MaterialPropertyBlock();
            wetOverlay.GetPropertyBlock(originalProperties);
            cached = true;
        }

        private void OnEnable() => RefreshNow();
        private void LateUpdate() => RefreshNow();

        // Explicit refresh also supports deterministic presentation checks without a save session.
        public void RefreshNow()
        {
            Cache();
            if (!cached) return;
            if (channel == null || channelSurface == null)
            {
                RestoreAuthoredVisuals();
                return;
            }
            wetOverlay.enabled = originalEnabled && channel.gameObject.activeInHierarchy && channelSurface.enabled;
            if (channel.IsDraining)
            {
                wetOverlay.sharedMaterial = channelSurface.sharedMaterial;
                channelSurface.GetPropertyBlock(channelProperties);
                wetOverlay.SetPropertyBlock(channelProperties);
            }
            else
            {
                wetOverlay.sharedMaterial = originalMaterial;
                wetOverlay.SetPropertyBlock(originalProperties);
            }
        }

        public void RestoreAuthoredVisuals()
        {
            if (!cached || wetOverlay == null) return;
            wetOverlay.enabled = originalEnabled;
            wetOverlay.sharedMaterial = originalMaterial;
            wetOverlay.SetPropertyBlock(originalProperties);
        }

        private void OnDisable() => RestoreAuthoredVisuals();
    }
}
