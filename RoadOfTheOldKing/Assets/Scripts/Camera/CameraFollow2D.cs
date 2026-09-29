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
        private Vector3 velocity;
        private CameraFreelook2D freelook;

        private void Awake()
        {
            freelook = GetComponent<CameraFreelook2D>();
            // Existing scenes may contain standalone cameras rather than prefab instances.
            // Compose the feature at runtime without rewriting their authored scene state.
            if (freelook == null) freelook = gameObject.AddComponent<CameraFreelook2D>();
        }

        private void OnEnable() => velocity = Vector3.zero;
        private void Start() => SnapToTarget();

        private void LateUpdate()
        {
            Vector2 lookOffset = freelook != null ? freelook.Evaluate(target, Time.deltaTime) : Vector2.zero;
            if (target == null || Time.deltaTime <= 0f)
                return;

            Vector3 followCenter = target.position + offset;
            Vector3 destination = followCenter + (Vector3)lookOffset;
            transform.position = smoothTime <= 0f
                ? destination
                : Vector3.SmoothDamp(transform.position, destination, ref velocity,
                    smoothTime, Mathf.Infinity, Time.deltaTime);
            if (freelook != null && freelook.isActiveAndEnabled)
                transform.position = freelook.Constrain(transform.position, followCenter);
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
            if (target != null)
                transform.position = target.position + offset;
        }
    }
}
