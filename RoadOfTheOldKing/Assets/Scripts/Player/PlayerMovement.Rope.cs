using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    public sealed partial class PlayerMovement
    {
        // Authored rope routes are the motor's only exception to ordinary wall collision.
        // The body stays kinematic along that fixed segment; swept body queries still reject
        // every solid except the explicitly assigned cliff. Landings ignore no colliders.
        private readonly List<RaycastHit2D> ropeHits = new List<RaycastHit2D>(16);
        private readonly List<Collider2D> ropeOverlaps = new List<Collider2D>(16);
        private BoxCollider2D ropeBody;
        private PlayerHealth ropeHealth;
        private PlayerControlLocks ropeLocks;
        private RopeRoute ropeRoute;
        private Collider2D ropeCliff;
        private Vector2 ropeOrigin, ropeStart, ropeEnd;
        private float ropeSpeed, ropeHealthAtStart;
        private bool ropeAligned;
        private RigidbodyType2D ropePreviousBodyType;
        public bool IsTraversingRope => ropeRoute != null;
        public bool IsTraversing(RopeRoute route) => route != null && ropeRoute == route;
        public Vector2 SafeResumePosition => IsTraversingRope ? ropeOrigin : (Vector2)transform.position;

        private void InitializeRopeTraversal()
        {
            ropeBody = GetComponent<BoxCollider2D>();
            ropeHealth = GetComponent<PlayerHealth>();
            ropeLocks = GetComponent<PlayerControlLocks>();
        }

        public bool CanStartRopeTraversal => isActiveAndEnabled && !IsTraversingRope &&
            ropeHealth != null && ropeHealth.IsAlive && ropeLocks != null && !ropeLocks.IsLocked &&
            movementInput != null && movementInput.IsActive && Time.timeScale > 0f &&
            !EncounterState.InCombat && (hitReaction == null || !hitReaction.IsStaggered) &&
            !dash.IsDashing && (flask == null || !flask.IsDrinking) &&
            (combat == null || combat.CanStartAttack && !combat.IsAttacking && !combat.ControlsMovement &&
                (combat.Weapon == null || !combat.Weapon.IsAway && !combat.Weapon.IsThrowing));

        public bool TryBeginRopeTraversal(RopeRoute route, Vector2 start, Vector2 end, Collider2D cliff, float speed)
        {
            if (!CanStartRopeTraversal || route == null || !route.isActiveAndEnabled || cliff == null ||
                speed <= 0f || Vector2.Distance(body.position, start) > 1.25f ||
                Vector2.Distance(start, end) < .5f || Vector2.Distance(start, end) > 12f ||
                !RopeLandingClear(body.position) || !RopeLandingClear(start) || !RopeLandingClear(end) ||
                !RopeSegmentClear(body.position, start, null) || !RopeSegmentClear(start, end, cliff)) return false;
            ropeOrigin = body.position;
            ropeStart = start;
            ropeEnd = end;
            ropeCliff = cliff;
            ropeSpeed = speed;
            ropeHealthAtStart = ropeHealth.Health.Health;
            ropeAligned = false;
            ropeRoute = route;
            ropePreviousBodyType = body.bodyType;
            ropeLocks.Lock(this);
            dash.Cancel();
            bounceRemaining = 0f;
            freeVelocity = Vector2.zero;
            IsSprinting = false;
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
            return true;
        }

        private Vector2 RopeBodySize => Vector2.Scale(ropeBody.size,
            new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y))) + Vector2.one * .02f;
        private Vector2 RopeBodyOffset => transform.TransformVector(ropeBody.offset);

        private bool RopeLandingClear(Vector2 position)
        {
            Physics2D.OverlapBox(position + RopeBodyOffset, RopeBodySize, transform.eulerAngles.z,
                new ContactFilter2D { useTriggers = false }, ropeOverlaps);
            foreach (var hit in ropeOverlaps)
                if (hit != null && !hit.transform.IsChildOf(transform)) return false;
            return true;
        }

        private bool RopeSegmentClear(Vector2 from, Vector2 to, Collider2D allowedCliff)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < .000001f) return true;
            Physics2D.BoxCast(from + RopeBodyOffset, RopeBodySize, transform.eulerAngles.z, delta.normalized,
                new ContactFilter2D { useTriggers = false }, ropeHits, delta.magnitude);
            foreach (var hit in ropeHits)
                if (hit.collider != null && hit.collider != allowedCliff &&
                    !hit.collider.transform.IsChildOf(transform)) return false;
            return true;
        }

        private void TickRopeTraversal(float deltaTime)
        {
            if (Time.timeScale <= 0f) { body.linearVelocity = Vector2.zero; return; }
            if (!ropeRoute.isActiveAndEnabled || ropeHealth == null || !ropeHealth.IsAlive ||
                ropeHealth.Health.Health < ropeHealthAtStart || (hitReaction != null && hitReaction.IsStaggered) ||
                (GameSession.Instance != null && GameSession.Instance.IsLoading) ||
                !RopeLandingClear(ropeEnd))
            {
                CancelRopeTraversal();
                return;
            }
            // A pause freezes FixedUpdate. A second input-lock owner never gets released here.
            Vector2 target = ropeAligned ? ropeEnd : ropeStart;
            Vector2 next = Vector2.MoveTowards(body.position, target, ropeSpeed * deltaTime);
            if (!RopeSegmentClear(body.position, next, ropeAligned ? ropeCliff : null))
            {
                CancelRopeTraversal();
                return;
            }
            Vector2 direction = target - body.position;
            if (direction.sqrMagnitude > .0001f) FacingDirection = direction.normalized;
            if (Vector2.Distance(body.position, target) < .01f)
            {
                if (!ropeAligned) ropeAligned = true;
                else FinishRopeTraversal(ropeEnd);
                return;
            }
            body.MovePosition(next);
        }

        public void CancelRopeTraversal()
        {
            if (!IsTraversingRope) return;
            // Routes must reserve clear landing aprons. Prefer the departure; if a moving
            // actor occupied it, finish on the other clear landing instead.
            FinishRopeTraversal(RopeLandingClear(ropeOrigin) || !RopeLandingClear(ropeEnd) ? ropeOrigin : ropeEnd);
        }

        private void FinishRopeTraversal(Vector2 position)
        {
            // Cancellation may jump back from inside the cliff. Keep the Transform and
            // physics pose atomic for interaction/resume queries in this same frame.
            transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            body.bodyType = ropePreviousBodyType;
            ropeRoute = null;
            ropeCliff = null;
            leavingAction = false;
            bounceRemaining = 0f;
            freeVelocity = Vector2.zero;
            ropeLocks.Unlock(this);
        }
    }
}
