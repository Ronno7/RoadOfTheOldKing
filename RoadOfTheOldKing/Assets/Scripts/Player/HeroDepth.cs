using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // The hero's place in depth (Depth): his body on the Standing layer at his feet, in the middle slot of its row, so
    // he walks in front of everything standing farther north and behind everything farther south (user, 23 Nov: "go with
    // codex's cleaner long term approach"). What follows the body (carried axe, grip fold, inner blade, dust, the swing
    // footprint, cut debris) reads its order in LateUpdate and sits a few slots either side, inside its row; so this runs
    // first.
    [DisallowMultipleComponent, DefaultExecutionOrder(-200)]
    public sealed class HeroDepth : MonoBehaviour
    {
        [Tooltip("The body art that sorts; found as the child named Body when empty.")]
        [SerializeField] private SpriteRenderer body;

        private void Awake()
        {
            if (body != null) return;
            var named = transform.Find("Body");
            body = named != null ? named.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>(true);
        }

        private void LateUpdate()
        {
            if (body != null) Depth.Apply(body, transform.position.y, Depth.BodySlot);
        }
    }
}
