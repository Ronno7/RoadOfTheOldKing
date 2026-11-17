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
        private static readonly int HoodieId = Shader.PropertyToID("_HoodieColor");

        [SerializeField] private SpriteRenderer body;
        [Tooltip("Cloak fold drawn over the grip, authored for a weapon left of the body and mirrored " +
            "on the right. Its pivot sits on the haft's centre line at the grip.")]
        [SerializeField] private Sprite gripFold;
        [Tooltip("Octants counterclockwise from east: E, NE, N, NW, W, SW, S, SE.")]
        [SerializeField] private Hold[] holds = DefaultHolds();
        [Tooltip("Columns of the weapon sprite on the body's side of the haft. In the facings ticked below they draw " +
            "behind the body, so the double-bitted axe's inner blade tucks behind the hood, as if slung over his " +
            "shoulder. The holds' mirroring puts this side at the sprite's left in every facing. 0 turns the split off.")]
        [SerializeField, Min(0)] private int innerColumns = 9;
        [Tooltip("Per octant (E, NE, N, NW, W, SW, S, SE): tuck the inner blade behind the body. Only the views " +
            "facing the camera (S, SE); in E and NE the axe stays whole in his right hand, in front of him (user, 16 Nov).")]
        [SerializeField] private bool[] tuckInnerBlade = { false, false, false, false, false, false, true, true };

        private PlayerSpriteAnimator animator;
        private SpriteRenderer fold, innerBlade, swapped;
        private Sprite whole, outer, inner;
        private int appliedFrame = -1;
        private MaterialPropertyBlock bodyBlock, foldBlock;

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
            innerBlade = new GameObject("Weapon Inner Blade").AddComponent<SpriteRenderer>();
            innerBlade.transform.SetParent(transform, false);
            innerBlade.enabled = false;
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
            int octant = animator.Octant;
            SplitInnerBlade(weaponSprite, hold.inFront && tuckInnerBlade != null && octant < tuckInnerBlade.Length && tuckInnerBlade[octant]);

            bool folded = !resting && hold.inFront && hold.coverGrip && gripFold != null;
            fold.enabled = folded;
            if (folded)
            {
                fold.transform.SetPositionAndRotation(grip, body.transform.rotation);
                fold.flipX = hold.grip.x > 0; // Outer side of the fold faces away from the body.
                fold.color = body.color;
                fold.sortingLayerID = body.sortingLayerID;
                fold.sortingOrder = body.sortingOrder + 2;
                MatchHoodie();
            }
            appliedFrame = Time.frameCount;
            return true;
        }

        // The fold is cloak, so it takes the hoodie colour the body carries (HoodieColor).
        private void MatchHoodie()
        {
            bodyBlock ??= new MaterialPropertyBlock();
            foldBlock ??= new MaterialPropertyBlock();
            body.GetPropertyBlock(bodyBlock);
            fold.GetPropertyBlock(foldBlock);
            foldBlock.SetColor(HoodieId, bodyBlock.HasColor(HoodieId) ? bodyBlock.GetColor(HoodieId) : Color.clear);
            fold.SetPropertyBlock(foldBlock);
        }

        // Tucked, the weapon shows only the haft and its outer side in front; the inner side draws just behind
        // the body. Untucked, behind the body, or out of the carry, the weapon is whole again.
        private void SplitInnerBlade(SpriteRenderer weaponSprite, bool tuck)
        {
            if (!tuck || innerColumns <= 0 || !Prepare(weaponSprite))
            {
                RestoreWeapon();
                return;
            }
            swapped = weaponSprite;
            weaponSprite.sprite = outer;
            innerBlade.sprite = inner;
            innerBlade.flipX = weaponSprite.flipX;
            innerBlade.color = weaponSprite.color;
            innerBlade.sharedMaterial = weaponSprite.sharedMaterial;
            innerBlade.sortingLayerID = body.sortingLayerID;
            innerBlade.sortingOrder = body.sortingOrder - 1;
            var t = weaponSprite.transform;
            innerBlade.transform.SetPositionAndRotation(t.position, t.rotation);
            innerBlade.transform.localScale = Vector3.one;
            innerBlade.enabled = true;
        }

        // Cuts the weapon's own sprite at the haft once; both halves keep its pivot, so they line up when mirrored.
        private bool Prepare(SpriteRenderer weaponSprite)
        {
            var current = weaponSprite.sprite;
            if (current == null) return false;
            if (current == outer) return true;
            if (current != whole)
            {
                whole = current;
                var r = whole.rect;
                if (innerColumns >= r.width) return false;
                float ppu = whole.pixelsPerUnit, pivotX = whole.pivot.x, pivotY = whole.pivot.y;
                outer = Sprite.Create(whole.texture, new Rect(r.x + innerColumns, r.y, r.width - innerColumns, r.height),
                    new Vector2((pivotX - innerColumns) / (r.width - innerColumns), pivotY / r.height), ppu, 0, SpriteMeshType.FullRect);
                inner = Sprite.Create(whole.texture, new Rect(r.x, r.y, innerColumns, r.height),
                    new Vector2(pivotX / innerColumns, pivotY / r.height), ppu, 0, SpriteMeshType.FullRect);
                outer.name = whole.name + " (outer)";
                inner.name = whole.name + " (inner)";
            }
            return true;
        }

        private void RestoreWeapon()
        {
            if (swapped != null && swapped.sprite == outer) swapped.sprite = whole;
            swapped = null;
            if (innerBlade != null) innerBlade.enabled = false;
        }

        // Script order puts this after the weapon's view: hide the fold and make the weapon whole on frames it was not carried.
        private void LateUpdate()
        {
            if (appliedFrame == Time.frameCount) return;
            if (fold != null) fold.enabled = false;
            RestoreWeapon();
        }

        private void OnDisable()
        {
            if (fold != null) fold.enabled = false;
            RestoreWeapon();
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
