using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Grip points belong to the drawn frames, not the movement input or mouse aim.
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponGrip : MonoBehaviour
    {
        [Serializable]
        private struct FrameGrip
        {
            public Sprite sprite;
            public Vector2 position;
            public float angle;
            public bool flipX;
        }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private FrameGrip[] frames = Array.Empty<FrameGrip>();
        private readonly Dictionary<Sprite, FrameGrip> grips = new Dictionary<Sprite, FrameGrip>();

        private void Awake()
        {
            foreach (var frame in frames)
                if (frame.sprite != null)
                    grips[frame.sprite] = frame;
        }

        public bool TryApply(Transform weaponModel, SpriteRenderer weaponSprite)
        {
            if (!isActiveAndEnabled || body == null || body.sprite == null ||
                weaponModel == null || weaponSprite == null ||
                !grips.TryGetValue(body.sprite, out var grip))
                return false;

            // Sample after the player Animator. Do not smooth independently: that
            // would make the haft slip through the hand whenever the sprite steps.
            weaponModel.SetPositionAndRotation(body.transform.TransformPoint(grip.position),
                body.transform.rotation * Quaternion.Euler(0f, 0f, grip.angle));
            weaponSprite.flipX = grip.flipX;
            weaponSprite.sortingLayerID = body.sortingLayerID;
            // Draw the actual hand over the grip, including the rear-facing poses.
            weaponSprite.sortingOrder = body.sortingOrder - 1;
            return true;
        }
    }
}
