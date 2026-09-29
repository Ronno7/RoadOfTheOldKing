using TheLostShrine.Input;
using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.Cameras
{
    // Supplies a bounded offset; CameraFollow2D remains the sole position writer.
    [DisallowMultipleComponent, RequireComponent(typeof(Camera), typeof(CameraLookInput))]
    public sealed class CameraFreelook2D : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [Tooltip("Maximum camera displacement from its normal player-follow position, in world units (tiles).")]
        [SerializeField, Min(0f)] private float maxDistance = 3f;
        private Camera view;
        private ICameraLookInput input;
        private Transform subject;
        private PlayerHealth health;
        private PlayerMovementInput movementInput;
        private bool requireRelease;

        public float MaxDistance => Mathf.Max(0f, maxDistance);
        public Vector2 LookOffset { get; private set; }
        public bool IsLooking { get; private set; }

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (inputSource == null) inputSource = GetComponent<CameraLookInput>();
            input = inputSource as ICameraLookInput;
            if (input == null) { Debug.LogError("CameraFreelook2D needs an ICameraLookInput source.", this); enabled = false; }
        }

        public Vector2 Evaluate(Transform target, float deltaTime)
        {
            if (subject != target)
            {
                subject = target;
                health = target != null ? target.GetComponent<PlayerHealth>() : null;
                movementInput = target != null ? target.GetComponent<PlayerMovementInput>() : null;
                ResetLook();
            }
            var frame = input != null && inputSource != null && inputSource.isActiveAndEnabled ? input.Read() : default;
            bool allowed = isActiveAndEnabled && target != null && view.orthographic && deltaTime > 0f &&
                (health == null || health.IsAlive) && (movementInput == null || movementInput.IsActive);
            if (!allowed) ResetLook();
            if (!frame.Held) requireRelease = false;
            IsLooking = allowed && frame.Held && !requireRelease;
            LookOffset = IsLooking ? PointerOffset(frame.PointerPosition, view.pixelRect, view.orthographicSize, view.aspect, MaxDistance) : Vector2.zero;
            return LookOffset;
        }

        // Use the viewport, not a cursor ray from the already displaced camera: no feedback drift.
        public static Vector2 PointerOffset(Vector2 pointer, Rect viewport, float size, float aspect, float limit)
        {
            if (viewport.width <= 0f || viewport.height <= 0f) return Vector2.zero;
            var normalized = new Vector2((pointer.x - viewport.center.x) / viewport.width,
                (pointer.y - viewport.center.y) / viewport.height);
            return Vector2.ClampMagnitude(new Vector2(normalized.x * 2f * size * aspect, normalized.y * 2f * size), Mathf.Max(0f, limit));
        }

        public Vector3 Constrain(Vector3 position, Vector3 followCenter)
        {
            Vector2 displacement = Vector2.ClampMagnitude((Vector2)(position - followCenter), MaxDistance);
            return new Vector3(followCenter.x + displacement.x, followCenter.y + displacement.y, position.z);
        }

        public void ResetLook() { LookOffset = Vector2.zero; IsLooking = false; requireRelease = true; }
        private void OnDisable() => ResetLook();
    }
}
