using System;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Out-of-combat carry: the weapon rests upright at the hero's right side, butt down, on the edge
    // of his silhouette, mirrored per facing and drawn in front of or behind the body. On the camera
    // side a small fold of his cloak wraps the grip, so a hand hidden inside it appears to hold the
    // haft. The hold rises and falls with the body's walk frames. Presentation only; the weapon keeps
    // ownership, state and collision.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerSpriteAnimator))]
    [DefaultExecutionOrder(210)]
    public sealed class PlayerWeaponCarry : MonoBehaviour
    {
        [Serializable]
        private struct Hold
        {
            [Tooltip("Weapon pivot (its grip) in whole pixels from the body's ground pivot.")]
            public Vector2Int grip;
            public bool flipX;
            [Tooltip("Draw over the body: his right side faces the camera.")]
            public bool inFront;
            [Tooltip("Wrap the grip in a fold of his cloak.")]
            public bool coverGrip;
        }

        private const float PixelsPerUnit = 16f;

        [SerializeField] private SpriteRenderer body;
        [Tooltip("Cloak fold drawn over the grip, authored for a weapon left of the body and mirrored " +
            "on the right. Its pivot sits on the haft's centre line at the grip.")]
        [SerializeField] private Sprite gripFold;
        [Tooltip("Octants counterclockwise from east: E, NE, N, NW, W, SW, S, SE.")]
        [SerializeField] private Hold[] holds = DefaultHolds();

        private PlayerSpriteAnimator animator;
        private SpriteRenderer fold;
        private int appliedFrame = -1;

        private void Awake()
        {
            animator = GetComponent<PlayerSpriteAnimator>();
            if (body == null || holds == null || holds.Length != 8)
            {
                Debug.LogError("Weapon carry needs the body renderer and eight holds.", this);
                enabled = false;
                return;
            }
            fold = new GameObject("Grip Fold").AddComponent<SpriteRenderer>();
            fold.transform.SetParent(transform, false);
            fold.sharedMaterial = body.sharedMaterial;
            fold.sprite = gripFold;
            fold.enabled = false;
        }

        // Called by the weapon's view while it is held out of combat.
        public bool TryApply(Transform model, SpriteRenderer weaponSprite)
        {
            if (!isActiveAndEnabled || model == null || weaponSprite == null || body.sprite == null)
                return false;
            var hold = holds[animator.Octant];
            // At a fire the axe stays upright beside the seated hero; no floating grip fold or bob.
            bool resting = animator.State == "Rest";
            Vector2 pixels = hold.grip + new Vector2Int(0, resting ? 0 : animator.Lift);
            Vector3 grip = body.transform.TransformPoint(pixels / PixelsPerUnit);
            model.SetPositionAndRotation(grip, body.transform.rotation);
            weaponSprite.flipX = hold.flipX;
            weaponSprite.sortingLayerID = body.sortingLayerID;
            weaponSprite.sortingOrder = body.sortingOrder + (hold.inFront ? 1 : -1);

            bool folded = !resting && hold.inFront && hold.coverGrip && gripFold != null;
            fold.enabled = folded;
            if (folded)
            {
                fold.transform.SetPositionAndRotation(grip, body.transform.rotation);
                fold.flipX = hold.grip.x > 0; // Outer side of the fold faces away from the body.
                fold.color = body.color;
                fold.sortingLayerID = body.sortingLayerID;
                fold.sortingOrder = body.sortingOrder + 2;
            }
            appliedFrame = Time.frameCount;
            return true;
        }

        // Script order puts this after the weapon's view: hide the fold on frames it was not carried.
        private void LateUpdate()
        {
            if (appliedFrame != Time.frameCount && fold != null) fold.enabled = false;
        }

        private void OnDisable()
        {
            if (fold != null) fold.enabled = false;
        }

        // Camera-side holds sit 3 px low (a depth cue) so the grip meets hand height (11 px): on the
        // silhouette edge, except east, which runs in line with his back half. Far-side holds stand
        // 1 px high behind him; west lines up with his body so it masks the haft, and north shows a
        // strip of handle along his right side with the head over his shoulder. East and west mirror each other. Blades point away from the
        // face (backward in side views).
        private static Hold[] DefaultHolds() => new[]
        {
            new Hold { grip = new Vector2Int(-4, 11), flipX = true, inFront = true, coverGrip = true },   // E
            new Hold { grip = new Vector2Int(7, 11), flipX = false, inFront = true, coverGrip = true },   // NE
            new Hold { grip = new Vector2Int(8, 15), flipX = false, inFront = false, coverGrip = false }, // N
            new Hold { grip = new Vector2Int(8, 15), flipX = false, inFront = false, coverGrip = false }, // NW
            new Hold { grip = new Vector2Int(4, 15), flipX = false, inFront = false, coverGrip = false }, // W
            new Hold { grip = new Vector2Int(-8, 15), flipX = true, inFront = false, coverGrip = false }, // SW
            new Hold { grip = new Vector2Int(-8, 11), flipX = true, inFront = true, coverGrip = true },   // S
            new Hold { grip = new Vector2Int(-8, 11), flipX = true, inFront = true, coverGrip = true },   // SE
        };
    }
}
