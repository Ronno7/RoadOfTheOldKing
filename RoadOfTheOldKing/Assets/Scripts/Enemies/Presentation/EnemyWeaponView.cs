using System;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // A separate weapon sprite for armless enemies (the hero's pattern), posed from the brain's facing and
    // attack clock so it always lies along the telegraphed lane. Two drawings, level (pointing east) and
    // diagonal (pointing north-east), cover all eight directions through exact 90-degree turns and mirrors,
    // never resampled. Presentation only: the brain and its actions own timing, reach and hits.
    [DisallowMultipleComponent, RequireComponent(typeof(PursuitBrain))]
    [DefaultExecutionOrder(210)]
    public sealed class EnemyWeaponView : MonoBehaviour
    {
        [Serializable]
        private struct Hold
        {
            [Tooltip("Grip in whole pixels from the body's ground pivot.")]
            public Vector2Int grip;
            [Tooltip("Direction the weapon points while carried, as an octant (0 east, counterclockwise).")]
            public int pointing;
            [Tooltip("Draw over the body.")]
            public bool inFront;
        }

        private const float PixelsPerUnit = 16f;

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer weapon;
        [Tooltip("Weapon pointing east, pivot on the grip pixel's centre.")]
        [SerializeField] private Sprite level;
        [Tooltip("Weapon pointing north-east, pivot on the grip pixel's centre.")]
        [SerializeField] private Sprite diagonal;
        [Tooltip("Grip height above the feet while attacking, in pixels.")]
        [SerializeField] private int attackHeight = 9;
        [Tooltip("Pixels the main attack draws the weapon back during its windup.")]
        [SerializeField] private int windupPull = 3;
        [Tooltip("Pixels the weapon extends along the lane when a blow lands.")]
        [SerializeField] private int thrustReach = 7;
        [Tooltip("Pixels the close attack raises the weapon overhead during its windup.")]
        [SerializeField] private int overheadRaise = 8;
        [Tooltip("Octants counterclockwise from east: E, NE, N, NW, W, SW, S, SE.")]
        [SerializeField] private Hold[] holds = DefaultHolds();

        private PursuitBrain brain;

        private void Awake()
        {
            brain = GetComponent<PursuitBrain>();
            if (body == null || weapon == null || level == null || diagonal == null || holds == null || holds.Length != 8)
            {
                Debug.LogError("Enemy weapon view needs the body, weapon renderer, both drawings and eight holds.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (weapon != null) weapon.enabled = false;
        }

        private void LateUpdate()
        {
            int facing = DirectionalSpriteAnimation.Octant(brain.FacingDirection);
            Pose(facing, out Vector2Int grip, out int pointing, out bool inFront);
            Apply(grip, pointing, inFront);
        }

        // The pose for this frame, in body pixels. Recovery keeps the locked lane: the opening shows.
        private void Pose(int facing, out Vector2Int grip, out int pointing, out bool inFront)
        {
            var hold = holds[facing];
            grip = hold.grip; pointing = hold.pointing; inFront = hold.inFront;
            if (brain.IsDefeated)
            {
                // Dropped beside the body, lying level.
                bool west = facing >= 3 && facing <= 5;
                grip = new Vector2Int(west ? 3 : -3, 1); pointing = west ? 4 : 0; inFront = false;
                return;
            }
            var state = brain.State;
            if (state != PursuitState.Windup && state != PursuitState.Striking && state != PursuitState.Recovering) return;

            Vector2 lane = Step(facing);
            float t = Mathf.Clamp01(brain.StateProgress);
            Vector2 at = new Vector2(0, attackHeight);
            pointing = facing;
            inFront = facing == 0 || facing >= 4; // the far (north) half is drawn behind the body
            if (brain.IsCloseAttack)
            {
                if (state == PursuitState.Windup)
                {
                    // Raised straight up overhead; the full height is reached as the direction locks.
                    pointing = 2; inFront = false;
                    at += new Vector2(0, Mathf.Round(overheadRaise * Mathf.Clamp01(t * 2f))) - lane;
                }
                else if (state == PursuitState.Striking) at += lane * (thrustReach * .6f) + Vector2.down * 2;
                else at += lane * 2 + Vector2.down * 3; // slammed low and left there through the long recovery
            }
            else if (state == PursuitState.Windup) at -= lane * (windupPull * Mathf.Clamp01(t * 2f));
            else if (state == PursuitState.Striking) at += lane * thrustReach;
            else at += lane * (thrustReach * (1f - t));
            grip = Vector2Int.RoundToInt(at);
        }

        private void Apply(Vector2Int grip, int pointing, bool inFront)
        {
            weapon.enabled = body.enabled;
            weapon.color = body.color; // follows the corpse fade
            weapon.sortingLayerID = body.sortingLayerID;
            weapon.sortingOrder = body.sortingOrder + (inFront ? 1 : -1);
            // Level points east, diagonal north-east; mirrors and quarter turns reach the rest exactly.
            bool odd = (pointing & 1) == 1;
            weapon.sprite = odd ? diagonal : level;
            float angle = 0f;
            bool flipX = false, flipY = false;
            switch (pointing & 7)
            {
                case 2: angle = 90f; break;
                case 4: flipX = true; break;
                case 6: angle = -90f; break;
                case 3: flipX = true; break;
                case 5: flipX = flipY = true; break;
                case 7: flipY = true; break;
            }
            weapon.flipX = flipX; weapon.flipY = flipY;
            // Body pixels centre on (n + .5) / 16 from its ground pivot; the weapon's pivot is a pixel centre too.
            Vector3 position = body.transform.TransformPoint(((Vector2)grip + new Vector2(.5f, .5f)) / PixelsPerUnit);
            weapon.transform.SetPositionAndRotation(position, body.transform.rotation * Quaternion.Euler(0, 0, angle));
        }

        private static Vector2 Step(int octant)
        {
            float radians = octant * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Round(Mathf.Cos(radians) * 100f) / 100f, Mathf.Round(Mathf.Sin(radians) * 100f) / 100f);
        }

        // Carried upright in the right hand, fist visible on the near-side facings and behind the body otherwise.
        private static Hold[] DefaultHolds() => new[]
        {
            new Hold { grip = new Vector2Int(-3, 8), pointing = 2, inFront = true },   // E
            new Hold { grip = new Vector2Int(4, 8), pointing = 2, inFront = true },    // NE
            new Hold { grip = new Vector2Int(7, 8), pointing = 2, inFront = true },    // N
            new Hold { grip = new Vector2Int(6, 9), pointing = 2, inFront = false },   // NW
            new Hold { grip = new Vector2Int(4, 9), pointing = 2, inFront = false },   // W
            new Hold { grip = new Vector2Int(4, 8), pointing = 2, inFront = false },   // SW
            new Hold { grip = new Vector2Int(-7, 8), pointing = 2, inFront = true },   // S
            new Hold { grip = new Vector2Int(-5, 8), pointing = 2, inFront = true },   // SE
        };
    }
}
