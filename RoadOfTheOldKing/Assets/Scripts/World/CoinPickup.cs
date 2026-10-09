using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Child trigger of CoinSource; proximity collection never competes for the F prompt.
    [RequireComponent(typeof(Collider2D))]
    public sealed class CoinPickup : MonoBehaviour
    {
        private CoinSource source;
        private void Awake() => source = GetComponentInParent<CoinSource>();
        private void OnTriggerEnter2D(Collider2D other) => Collect(other);
        private void OnTriggerStay2D(Collider2D other) => Collect(other);
        private void Collect(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null && source != null) source.TryCollect(player);
        }
    }
}
