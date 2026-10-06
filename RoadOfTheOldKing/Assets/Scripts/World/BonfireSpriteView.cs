using UnityEngine;

namespace RoadOfTheOldKing.World
{
    /// <summary>Native frame animation for a discovered fire; the authored sprite scale stays fixed.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Bonfire))]
    public sealed class BonfireSpriteView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flame;
        [SerializeField] private Sprite[] litFrames;
        [SerializeField, Min(0.1f)] private float framesPerSecond = 6f;
        private Bonfire fire;
        private float frameClock;

        private void OnEnable()
        {
            fire = GetComponent<Bonfire>();
            frameClock = 0f;
            Refresh(0f);
        }

        private void LateUpdate() => Refresh(Time.deltaTime);

        private void Refresh(float elapsed)
        {
            if (flame == null) return;
            bool lit = fire != null && fire.IsDiscovered && litFrames != null && litFrames.Length > 0;
            flame.enabled = lit;
            if (!lit)
            {
                frameClock = 0f;
                return;
            }
            frameClock = (frameClock + elapsed * Mathf.Max(0.1f, framesPerSecond)) % litFrames.Length;
            flame.sprite = litFrames[Mathf.FloorToInt(frameClock)];
        }
    }
}
