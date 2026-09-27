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

        private Camera viewCamera;
        private float targetSize;
        private float zoomVelocity;

        private void Awake() => viewCamera = GetComponent<Camera>();

        private void OnEnable()
        {
            targetSize = Mathf.Clamp(viewCamera.orthographicSize, minSize, maxSize);
            zoomVelocity = 0f;
            if (viewCamera.orthographic)
                viewCamera.orthographicSize = targetSize;
        }

        private void Update()
        {
            if (!viewCamera.orthographic || !Application.isFocused || Time.deltaTime <= 0f)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            // Scroll is already a per-frame delta; do not multiply it by deltaTime.
            float scroll = mouse.scroll.ReadValue().y;
            targetSize = Mathf.Clamp(targetSize - scroll * scrollSensitivity, minSize, maxSize);
        }

        private void LateUpdate()
        {
            if (!viewCamera.orthographic || Time.deltaTime <= 0f)
                return;

            targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
            float size = smoothTime <= 0f
                ? targetSize
                : Mathf.SmoothDamp(viewCamera.orthographicSize, targetSize, ref zoomVelocity,
                    smoothTime, Mathf.Infinity, Time.deltaTime);
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
