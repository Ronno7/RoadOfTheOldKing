using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraZoom2D : MonoBehaviour
    {
        [Tooltip("Closest view, measured as half the visible world height.")]
        [SerializeField, Min(0.1f)] private float minSize = 3f;
        [Tooltip("Widest view, measured as half the visible world height.")]
        [SerializeField, Min(0.1f)] private float maxSize = 8f;
        [Tooltip("Starting view of a session (half the visible world height). After that the player's zoom carries " +
            "across scenes; the camera's authored size only frames the Editor.")]
        [SerializeField, Min(0.1f)] private float defaultSize = 6.5f;
        [Tooltip("Zoom amount per scroll step. Uses the Input System's uniform scroll range.")]
        [SerializeField, Min(0.01f)] private float scrollSensitivity = 0.5f;
        [Tooltip("Controller zoom while the D-pad is held, in size units per second.")]
        [SerializeField, Min(0.01f)] private float controllerZoomSpeed = 4f;
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
        private InputAction zoomIn;
        private InputAction zoomOut;
        // The player's zoom for this session, shared by every scene's camera so a scene change keeps it (user, 17 Oct).
        private static float? sessionSize;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() => sessionSize = null;

        private void Awake()
        {
            viewCamera = GetComponent<Camera>();
            zoomIn = new InputAction("Zoom In", InputActionType.Button, RoadOfTheOldKing.Input.GamepadLayout.ZoomIn);
            zoomOut = new InputAction("Zoom Out", InputActionType.Button, RoadOfTheOldKing.Input.GamepadLayout.ZoomOut);
        }

        private void OnDisable() { zoomIn?.Disable(); zoomOut?.Disable(); }
        private void OnDestroy() { zoomIn?.Dispose(); zoomOut?.Dispose(); }

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
            zoomIn.Enable(); zoomOut.Enable();
            targetSize = Mathf.Clamp(sessionSize ?? defaultSize, minSize, maxSize);
            zoomVelocity = 0f;
            if (viewCamera.orthographic)
                viewCamera.orthographicSize = targetSize;
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (RoadOfTheOldKing.UI.DevToolsPanel.CapturesInput) return;
#endif
            if (!viewCamera.orthographic || !Application.isFocused || Time.deltaTime <= 0f)
                return;
            if (overrideOwner != null) return;

            // Scroll is already a per-frame delta; do not multiply it by deltaTime.
            var mouse = Mouse.current;
            if (mouse != null)
                targetSize = Mathf.Clamp(targetSize - mouse.scroll.ReadValue().y * scrollSensitivity, minSize, maxSize);
            sessionSize = targetSize;

            // The D-pad navigates menus; it zooms only during play.
            if (RoadOfTheOldKing.UI.MenuStack.IsAnyOpen) return;
            float held = (zoomOut.IsPressed() ? 1f : 0f) - (zoomIn.IsPressed() ? 1f : 0f);
            if (held != 0f)
                targetSize = Mathf.Clamp(targetSize + held * controllerZoomSpeed * Time.unscaledDeltaTime, minSize, maxSize);
            sessionSize = targetSize;
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
            defaultSize = Mathf.Clamp(defaultSize, minSize, maxSize);
            scrollSensitivity = Mathf.Max(0.01f, scrollSensitivity);
            controllerZoomSpeed = Mathf.Max(0.01f, controllerZoomSpeed);
            smoothTime = Mathf.Max(0f, smoothTime);
        }
    }
}
