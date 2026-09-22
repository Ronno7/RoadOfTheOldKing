using System;
using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent]
    public sealed class ShieldProtection : MonoBehaviour, IHitProtection
    {
        [SerializeField] private GameObject shieldVisual;
        [Tooltip("Optional orientation transform. Uses this object's rotation when unassigned.")]
        [SerializeField] private Transform facingReference;
        [Tooltip("The front direction in the orientation transform's local space.")]
        [SerializeField] private Vector2 localForward = Vector2.down;

        public Vector2 FacingDirection => (Vector2)(facingReference != null ? facingReference : transform)
            .TransformDirection(localForward.sqrMagnitude > 0.001f ? localForward.normalized : Vector2.down);
        public bool IsIntact { get; private set; } = true;
        public event Action Blocked;
        public event Action Broken;

        public bool Blocks(CombatHit hit)
        {
            if (!IsIntact)
                return false;
            if (hit.BreaksGuard)
            {
                IsIntact = false;
                if (shieldVisual != null)
                    shieldVisual.SetActive(false);
                Broken?.Invoke();
                return false;
            }
            if (IsRearRecallHit(hit))
                return false;
            Blocked?.Invoke();
            return true;
        }

        public bool IsRearRecallHit(CombatHit hit)
        {
            if (hit.Kind != AttackKind.Recall || !hit.ImpactPoint.HasValue)
                return false;

            // Use where the hatchet actually contacts us, not its owner's position.
            Vector2 toImpact = hit.ImpactPoint.Value - (Vector2)transform.position;
            if (toImpact.sqrMagnitude <= 0.000001f)
                return false;

            // The rear half is vulnerable; exact side hits remain guarded.
            return Vector2.Dot(FacingDirection, toImpact.normalized) < -0.001f;
        }

        public void Restore()
        {
            IsIntact = true;
            if (shieldVisual != null)
                shieldVisual.SetActive(true);
        }
    }
}
