using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Draws the trader from his walker: the walk while moving, the breathing idle while standing, in the
    // facing octant. Falls back to the standing views when a clip is missing. Presentation only.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class TraderSpriteView : MonoBehaviour
    {
        [SerializeField] private NpcAnimationSet animations;
        [SerializeField] private TraderWalker walker;

        private SpriteRenderer body;
        private bool wasWalking;
        private float clipTime;

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            if (walker == null) walker = GetComponentInParent<TraderWalker>();
        }

        private void LateUpdate()
        {
            if (animations == null || walker == null) return;
            bool walking = walker.Velocity.sqrMagnitude > .0001f;
            if (walking != wasWalking) { clipTime = 0f; wasWalking = walking; }
            clipTime += Time.deltaTime;
            int octant = DirectionalSpriteAnimation.Octant(walker.Facing);
            var clip = walking ? animations.walk : animations.idle;
            Sprite sprite = null;
            if (clip != null && !clip.IsEmpty)
                sprite = clip.Frame(octant, Mathf.FloorToInt(clipTime * clip.framesPerSecond));
            if (sprite == null && animations.rotations != null) sprite = animations.rotations.Frame(octant, 0);
            if (sprite != null) body.sprite = sprite;
        }
    }
}
