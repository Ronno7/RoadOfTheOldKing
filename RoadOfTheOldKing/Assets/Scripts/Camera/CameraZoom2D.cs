using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraZoom2D : MonoBehaviour
    {
        [Tooltip("Closest view, measured as half the visible world height.")]
        [SerializeField, Min(0.1f)] private float minSize = 3f;
        [Tooltip("Widest view, measured as half the visible world height.")]
        [SerializeField, Min(0.1f)] private float maxSize = 8f;
        [Tooltip("Zoom amount per scroll step. Uses the Input System's uniform scroll range.")]
        [SerializeField, Min(0.01f)] private float scrollSensitivity = 0.5f;
        [Tooltip("Seconds of zoom smoothing. Set to zero for instant zoom.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.08f;
        [Tooltip("Seconds of smoothing into and out of an override (e.g. the bonfire close-up).")]
        [SerializeField, Min(0f)] private float overrideSmoothTime = 0.3f;

        private Camera viewCamera;
        private float targetSize;
        private float zoomVelocity;
        // One size override at a time (e.g. the bonfire menu); the player's own zoom returns afterwards.
        private object overrideOwner;
        private float overrideSize;
        private float easeUntil;

        private void Awake() => viewCamera = GetComponent<Camera>();

        public void SetOverride(object owner, float size)
        {
            overrideOwner = owner;
            overrideSize = size;
            easeUntil = Time.unscaledTime + overrideSmoothTime * 3f;
        }

        public void ClearOverride(object owner)
        {
            if (overrideOwner != owner) return;
            overrideOwner = null;
            easeUntil = Time.unscaledTime + overrideSmoothTime * 3f;
        }

        private void OnEnable()
        {
            targetSize = Mathf.Clamp(viewCamera.orthographicSize, minSize, maxSize);
            zoomVelocity = 0f;
            if (viewCamera.orthographic)
                viewCamera.orthographicSize = targetSize;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (TheLostShrine.UI.DevToolsPanel.CapturesInput) return;
#endif
            if (!viewCamera.orthographic || !Application.isFocused || Time.deltaTime <= 0f)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // Scroll is already a per-frame delta; do not multiply it by deltaTime.
            if (overrideOwner != null) return;
            float scroll = mouse.scroll.ReadValue().y;
            targetSize = Mathf.Clamp(targetSize - scroll * scrollSensitivity, minSize, maxSize);
        }

        private void LateUpdate()
        {
            if (!viewCamera.orthographic || Time.deltaTime <= 0f)
                return;

            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
            float goal = overrideOwner != null ? overrideSize : targetSize;
            float smoothing = Time.unscaledTime < easeUntil ? overrideSmoothTime : smoothTime;
            float size = smoothing <= 0f
                ? goal
                : Mathf.SmoothDamp(viewCamera.orthographicSize, goal, ref zoomVelocity,
                    smoothing, Mathf.Infinity, Time.deltaTime);
            viewCamera.orthographicSize = Mathf.Clamp(size, minSize, maxSize);
        }

        private void OnValidate()
        {
            minSize = Mathf.Max(0.1f, minSize);
            maxSize = Mathf.Max(minSize, maxSize);
            scrollSensitivity = Mathf.Max(0.01f, scrollSensitivity);
            smoothTime = Mathf.Max(0f, smoothTime);
        }
    }
}
