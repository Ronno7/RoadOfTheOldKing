using UnityEngine;

namespace RoadOfTheOldKing.Combat
{
    // Corpses are ground-level props. Restore each renderer's living order when rest revives it.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class EnemyCorpseSorting : MonoBehaviour
    {
        private Damageable health;
        private SpriteRenderer[] sprites;
        private int[] layers, orders;
        private bool lowered;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            layers = new int[sprites.Length];
            orders = new int[sprites.Length];
        }

        private void OnEnable() => health.Defeated += Lower;
        private void OnDisable() => health.Defeated -= Lower;

        private void LateUpdate()
        {
            if (!health.IsAlive) Lower();
            else if (lowered)
            {
                for (int i = 0; i < sprites.Length; i++)
                    if (sprites[i] != null)
                    {
                        sprites[i].sortingLayerID = layers[i];
                        sprites[i].sortingOrder = orders[i];
                    }
                lowered = false;
            }
        }

        private void Lower()
        {
            if (lowered) return;
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null)
                {
                    layers[i] = sprites[i].sortingLayerID;
                    orders[i] = sprites[i].sortingOrder;
                    sprites[i].sortingLayerName = "World";
                    sprites[i].sortingOrder = 2;
                }
            lowered = true;
        }
    }
}
