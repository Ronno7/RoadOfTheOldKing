using RoadOfTheOldKing.Input;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Cameras
{
    // Supplies a bounded offset; CameraFollow2D remains the sole position writer. As in Dark Souls 3 the view stays where
    // it was put when the look ends, and the next step the player takes eases it back.
    [DisallowMultipleComponent, RequireComponent(typeof(Camera), typeof(CameraLookInput))]
    public sealed class CameraFreelook2D : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputSource;
        [Tooltip("Maximum camera displacement from its normal player-follow position, in world units (tiles).")]
        [SerializeField, Min(0f)] private float maxDistance = 3f;
        [Tooltip("Seconds of full stick tilt to go from centre to the limit.")]
        [SerializeField, Min(.05f)] private float stickSeconds = .5f;
        [Tooltip("Smoothing as the view eases back to centre once the player moves; larger is slower. It settles in about three times this.")]
        [SerializeField, Min(.05f)] private float returnSmoothing = .8f;
        private Camera view;
        private ICameraLookInput input;
        private Transform subject;
        private PlayerHealth health;
        private PlayerMovementInput movementInput;
        private bool requireRelease;
        private bool returning;
        private Vector2 returnVelocity;

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
            if (!isActiveAndEnabled || target == null || !view.orthographic || (health != null && !health.IsAlive)) ResetLook();
            else if (deltaTime <= 0f || (movementInput != null && !movementInput.IsActive))
            {
                // A menu or a pause holds the view; it can't start a look until the key has been let go.
                IsLooking = false;
                requireRelease = true;
                return LookOffset;
            }
            if (!frame.Held) requireRelease = false;
            IsLooking = frame.Held && !requireRelease && isActiveAndEnabled && target != null;
            if (IsLooking)
            {
                returning = false;
                returnVelocity = Vector2.zero;
                // The stick moves the view rather than holding it, so letting the stick spring back leaves it there.
                LookOffset = frame.UseStick
                    ? Vector2.ClampMagnitude(LookOffset + Vector2.ClampMagnitude(frame.Stick, 1f) * (MaxDistance / stickSeconds * deltaTime), MaxDistance)
                    : PointerOffset(frame.PointerPosition, view.pixelRect, view.orthographicSize, view.aspect, MaxDistance);
            }
            else
            {
                // One step starts the return and it finishes on its own, so the view never rests halfway.
                if (movementInput != null && movementInput.MoveDirection.sqrMagnitude > 0f) returning = true;
                if (returning) EaseBack(deltaTime);
            }
            return LookOffset;
        }

        private void EaseBack(float deltaTime)
        {
            LookOffset = Vector2.SmoothDamp(LookOffset, Vector2.zero, ref returnVelocity, returnSmoothing, Mathf.Infinity, deltaTime);
            if (LookOffset.sqrMagnitude > .0001f) return;
            LookOffset = Vector2.zero;
            returnVelocity = Vector2.zero;
            returning = false;
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

        public void ResetLook() { LookOffset = Vector2.zero; returnVelocity = Vector2.zero; returning = false; IsLooking = false; requireRelease = true; }
        private void OnDisable() => ResetLook();
    }
}
