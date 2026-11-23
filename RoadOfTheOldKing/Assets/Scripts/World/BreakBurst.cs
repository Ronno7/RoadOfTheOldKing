using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Plays a SpriteBurst where a Breakable breaks (leaves from a bush, chips from cracked stone). The Breakable hides its
    // object right after the event, so the burst lives on its own object.
    [DisallowMultipleComponent, RequireComponent(typeof(Breakable))]
    public sealed class BreakBurst : MonoBehaviour
    {
        [SerializeField] private SpriteBurst burst;
        [Tooltip("From this object's position (its foot) to the burst's anchor.")]
        [SerializeField] private Vector2 offset;
        private Breakable breakable;

        private void Awake() => breakable = GetComponent<Breakable>();
        private void OnEnable() { if (breakable != null) breakable.Broken += OnBroken; }
        private void OnDisable() { if (breakable != null) breakable.Broken -= OnBroken; }

        private void OnBroken(CombatHit hit)
        {
            if (burst != null) burst.Play(transform.position + (Vector3)offset, hit.Direction.x < 0f);
        }
    }
}
