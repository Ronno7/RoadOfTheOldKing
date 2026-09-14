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

        private void OnEnable() => velocity = Vector3.zero;
        private void Start() => SnapToTarget();

        private void LateUpdate()
        {
            if (target == null || Time.deltaTime <= 0f)
                return;

            Vector3 destination = target.position + offset;
            transform.position = smoothTime <= 0f
                ? destination
                : Vector3.SmoothDamp(transform.position, destination, ref velocity,
                    smoothTime, Mathf.Infinity, Time.deltaTime);
        }

        // The camera needs only a Transform, so it can later follow another subject.
        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            velocity = Vector3.zero;
            if (snap)
                SnapToTarget();
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            velocity = Vector3.zero;
            if (target != null)
                transform.position = target.position + offset;
        }
    }
}
