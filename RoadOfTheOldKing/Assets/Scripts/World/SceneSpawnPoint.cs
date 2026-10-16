using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Named arrival point for scene exits. Ids only need to be unique within their scene.
    [DisallowMultipleComponent]
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string id;
        [Tooltip("Optional world-space facing on arrival. Zero preserves the player's normal initial facing.")]
        [SerializeField] private Vector2 arrivalFacing = Vector2.zero;
        public string Id => id;
        public Vector2 Position => transform.position;
        public Vector2 ArrivalFacing => arrivalFacing;

        public static SceneSpawnPoint Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var point in FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None))
                if (point.id == id) return point;
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(.95f, .8f, .3f, .8f);
            Gizmos.DrawWireSphere(transform.position, .4f);
        }
    }
}
