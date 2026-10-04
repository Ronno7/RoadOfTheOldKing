using TheLostShrine.UI;
using UnityEngine;

namespace TheLostShrine.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        [Tooltip("Seconds of follow smoothing. Set to zero for exact following.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.1f;
        [Header("Kick")]
        [Tooltip("How quickly an impact kick settles (per second, real time).")]
        [SerializeField, Min(1f)] private float kickRecovery = 16f;
        [Tooltip("Largest kick offset in world units.")]
        [SerializeField, Min(0f)] private float maxKick = .4f;
        [Header("Framing")]
        [Tooltip("Seconds of smoothing while a framing request moves the target off centre, and back.")]
        [SerializeField, Min(0f)] private float framingSmoothTime = .3f;
        private Vector3 velocity;
        // The follow position without the kick, so a kick never feeds back into the smoothing.
        private Vector3 basePosition;
        private Vector2 kick;
        private CameraFreelook2D freelook;
        private Camera view;
        // One framing request at a time (e.g. the bonfire menu): the target sits at this viewport point.
        private object framingOwner;
        private Vector2 framingPoint;
        private float framingBlend;

        private void Awake()
        {
            freelook = GetComponent<CameraFreelook2D>();
            view = GetComponent<Camera>();
            // Existing scenes may contain standalone cameras rather than prefab instances.
            // Compose the feature at runtime without rewriting their authored scene state.
            if (freelook == null) freelook = gameObject.AddComponent<CameraFreelook2D>();
            basePosition = transform.position;
        }

        private void OnEnable() => velocity = Vector3.zero;
        private void Start() => SnapToTarget();

        // A brief, capped offset along an impact; scaled by the shake accessibility setting.
        public void Kick(Vector2 direction, float distance)
        {
            if (direction.sqrMagnitude < .0001f || distance <= 0f) return;
            kick = Vector2.ClampMagnitude(kick + direction.normalized * distance * Mathf.Max(0f, FeedbackSettings.ShakeScale), maxKick);
        }

        // Puts the target at a viewport point (0..1, e.g. (1/3, 0.5) for the left third) until cleared by the
        // same owner. Freelook is ignored meanwhile.
        public void SetFraming(object owner, Vector2 viewportPoint)
        {
            framingOwner = owner;
            framingPoint = viewportPoint;
        }

        public void ClearFraming(object owner)
        {
            if (framingOwner == owner) framingOwner = null;
        }

        private Vector3 FramingOffset()
        {
            if (view == null || !view.orthographic) return Vector3.zero;
            float height = view.orthographicSize * 2f, width = height * view.aspect;
            return new Vector3((.5f - framingPoint.x) * width, (.5f - framingPoint.y) * height, 0f) * framingBlend;
        }

        private void LateUpdate()
        {
            // Kicks settle in real time so they read through hit-stop and pause leases alike.
            kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-kickRecovery * Time.unscaledDeltaTime));
            Vector2 lookOffset = freelook != null ? freelook.Evaluate(target, Time.deltaTime) : Vector2.zero;
            // Real time: the bonfire doesn't pause the world, but menus may.
            framingBlend = Mathf.MoveTowards(framingBlend, framingOwner != null ? 1f : 0f,
                Time.unscaledDeltaTime / Mathf.Max(.01f, framingSmoothTime));
            if (framingOwner != null) lookOffset = Vector2.zero;
            if (target == null || Time.deltaTime <= 0f)
            {
                transform.position = basePosition + (Vector3)kick;
                return;
            }

            Vector3 followCenter = target.position + offset;
            Vector3 destination = followCenter + (Vector3)lookOffset + FramingOffset();
            float smoothing = framingBlend > 0f && framingBlend < 1f ? Mathf.Max(smoothTime, framingSmoothTime * .5f) : smoothTime;
            basePosition = smoothing <= 0f
                ? destination
                : Vector3.SmoothDamp(basePosition, destination, ref velocity,
                    smoothing, Mathf.Infinity, Time.deltaTime);
            if (freelook != null && freelook.isActiveAndEnabled)
                basePosition = freelook.Constrain(basePosition, followCenter);
            transform.position = basePosition + (Vector3)kick;
        }

        // The camera needs only a Transform, so it can later follow another subject.
        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (freelook != null) freelook.ResetLook();
            velocity = Vector3.zero;
            if (snap)
                SnapToTarget();
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (freelook != null) freelook.ResetLook();
            velocity = Vector3.zero;
            kick = Vector2.zero;
            if (target != null)
                basePosition = transform.position = target.position + offset;
        }
    }
}
