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
        private Vector3 velocity;
        // The follow position without the kick, so a kick never feeds back into the smoothing.
        private Vector3 basePosition;
        private Vector2 kick;
        private CameraFreelook2D freelook;

        private void Awake()
        {
            freelook = GetComponent<CameraFreelook2D>();
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

        private void LateUpdate()
        {
            // Kicks settle in real time so they read through hit-stop and pause leases alike.
            kick = Vector2.Lerp(kick, Vector2.zero, 1f - Mathf.Exp(-kickRecovery * Time.unscaledDeltaTime));
            Vector2 lookOffset = freelook != null ? freelook.Evaluate(target, Time.deltaTime) : Vector2.zero;
            if (target == null || Time.deltaTime <= 0f)
            {
                transform.position = basePosition + (Vector3)kick;
                return;
            }

            Vector3 followCenter = target.position + offset;
            Vector3 destination = followCenter + (Vector3)lookOffset;
            basePosition = smoothTime <= 0f
                ? destination
                : Vector3.SmoothDamp(basePosition, destination, ref velocity,
                    smoothTime, Mathf.Infinity, Time.deltaTime);
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
