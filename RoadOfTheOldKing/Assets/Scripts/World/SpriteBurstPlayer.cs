using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Runs one SpriteBurst: steps its frames on scaled time (so pauses hold it) and destroys itself after the last.
    [DisallowMultipleComponent]
    public sealed class SpriteBurstPlayer : MonoBehaviour
    {
        private SpriteBurst burst;
        private SpriteRenderer art;
        private float age;

        internal void Begin(SpriteBurst source, bool flipX)
        {
            burst = source;
            art = gameObject.AddComponent<SpriteRenderer>();
            art.sprite = burst.frames[0];
            art.flipX = flipX;
            art.sortingLayerName = burst.sortingLayer;
            art.sortingOrder = burst.sortingOrder;
            art.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        private void Update()
        {
            if (burst == null) { Destroy(gameObject); return; }
            age += Time.deltaTime;
            int frame = (int)(age * burst.framesPerSecond);
            if (frame >= burst.frames.Length) { Destroy(gameObject); return; }
            art.sprite = burst.frames[frame];
        }
    }
}
