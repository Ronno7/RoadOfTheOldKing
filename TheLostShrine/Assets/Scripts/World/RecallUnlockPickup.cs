using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class RecallUnlockPickup : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer crystal;
        [SerializeField] private TextMesh label;
        private bool activated;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activated)
                return;
            var player = other.GetComponentInParent<PlayerCombatController>();
            if (player == null)
                return;
            player.UnlockRecall();
            activated = true;
            if (crystal != null)
                crystal.color = new Color(0.7f, 1f, 0.8f);
            if (label != null)
                label.text = "RECALL AWAKENED";
        }
    }
}
