using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // One observer per authored clump, never a component or reward roll per blade.
    [DisallowMultipleComponent, RequireComponent(typeof(CoinSource))]
    public sealed class VegetationCoinPatch : MonoBehaviour
    {
        [SerializeField] private InteractiveVegetation vegetation;
        [SerializeField, Min(.1f)] private float radius = 1.25f;
        private CoinSource source;

        private void OnEnable()
        {
            source = GetComponent<CoinSource>();
            if (vegetation != null) vegetation.PlantCut += OnPlantCut;
        }

        private void OnDisable()
        {
            if (vegetation != null) vegetation.PlantCut -= OnPlantCut;
        }

        private void OnPlantCut(Vector2 position)
        {
            if ((position - (Vector2)transform.position).sqrMagnitude > radius * radius) return;
            // Includes an empty saved result. Rest, scene visits and reloads never reroll it.
            if (source.Reveal()) vegetation.PlantCut -= OnPlantCut;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(.7f, .55f, .2f, .8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
