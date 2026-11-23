using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A one-shot pixel effect made in PixelLab's VFX tool (user, 23 Nov: leaves from a cut bush, dust from a dodge or a
    // landing, chips from split stone). Its frames play once where it is called, sorted with the world by their pivot,
    // then the object removes itself. Presentation only: it never touches gameplay.
    [CreateAssetMenu(menuName = "Road of the Old King/Sprite Burst")]
    public sealed class SpriteBurst : ScriptableObject
    {
        [Tooltip("Played once, in order. Each frame's pivot is the effect's anchor (the ground point for dust and debris).")]
        public Sprite[] frames;
        [Min(1f)] public float framesPerSecond = 14f;
        public string sortingLayer = "Player";
        public int sortingOrder;

        public void Play(Vector3 position, bool flipX = false)
        {
            if (frames == null || frames.Length == 0) return;
            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<SpriteBurstPlayer>().Begin(this, flipX);
        }
    }
}
