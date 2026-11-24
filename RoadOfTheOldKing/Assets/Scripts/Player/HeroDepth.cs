using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // The hero's place in depth (Depth): his body on the Standing layer at his feet, in the middle slot of its row, so
    // he walks in front of everything standing farther north and behind everything farther south (user, 23 Nov: "go with
    // codex's cleaner long term approach"). His feet are where his boots are drawn: the body hangs a quarter unit under
    // the root (the collider's centre), and sorting from the root put him behind stones whose base was a few pixels north
    // of his boots (user, 23 Nov: "puts rocks in front of player when above"). The offset is the body's rest pose, read
    // once, so an animation that moves the body doesn't move his line. What follows the body (carried axe, grip fold,
    // inner blade, dust, the swing footprint, cut debris) reads its order in LateUpdate and sits a few slots either side,
    // inside its row; so this runs first.
    [DisallowMultipleComponent, DefaultExecutionOrder(-200)]
    public sealed class HeroDepth : MonoBehaviour
    {
        [Tooltip("The body art that sorts; found as the child named Body when empty.")]
        [SerializeField] private SpriteRenderer body;
        private float feet;

        public float Feet => transform.position.y + feet;

        private void Awake()
        {
            if (body == null)
            {
                var named = transform.Find("Body");
                body = named != null ? named.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>(true);
            }
            if (body != null) feet = body.transform.position.y - transform.position.y;
        }

        private void LateUpdate()
        {
            if (body != null) Depth.Apply(body, Feet, Depth.BodySlot);
        }
    }
}
