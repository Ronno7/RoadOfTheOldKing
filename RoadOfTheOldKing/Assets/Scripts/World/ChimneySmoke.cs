using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A lived-in home (22 Oct): a thin column of smoke from the chimney, using the bonfire's emitter and smoke
    // material. Place this on a child at the chimney top; the smoke draws just above the roof it rises from.
    [DisallowMultipleComponent]
    public sealed class ChimneySmoke : MonoBehaviour
    {
        [SerializeField] private Material smokeMaterial;
        [Tooltip("The roof (overhead) renderer; smoke draws one order above it.")]
        [SerializeField] private Renderer roof;
        private BonfireSmoke smoke;

        private void OnEnable()
        {
            if (!Application.isPlaying || smokeMaterial == null) return;
            smoke ??= new BonfireSmoke(transform, smokeMaterial, "Chimney smoke", Vector3.zero);
            if (roof != null) smoke.Refresh(true, roof.sortingLayerID, roof.sortingOrder + 1);
            else smoke.Refresh(true, SortingLayer.NameToID("Player"), 101);
        }

        private void OnDisable() => smoke?.Stop();
        private void OnDestroy() { smoke?.Dispose(); smoke = null; }
    }
}
