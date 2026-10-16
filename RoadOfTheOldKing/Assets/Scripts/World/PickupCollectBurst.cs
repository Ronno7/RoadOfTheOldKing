using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Short-lived world feedback. Reward ownership is already committed; this never moves a pickup.
    public sealed class PickupCollectBurst : MonoBehaviour
    {
        private SpriteRenderer ghost;
        private readonly SpriteRenderer[] motes = new SpriteRenderer[6];
        private PickupStyle style;
        private Color accent;
        private Color originalColor;
        private Vector3 start, target, originalScale;
        private float age;

        internal static void Play(SpriteRenderer art, Sprite spark, PickupStyle style, PlayerHealth player)
        {
            var go = new GameObject("Pickup collection");
            go.transform.position = art.transform.position;
            var effect = go.AddComponent<PickupCollectBurst>();
            effect.style = style;
            effect.start = go.transform.position;
            effect.target = style == PickupStyle.Coin && player != null
                ? player.transform.position + Vector3.up * .65f : effect.start + Vector3.up * .65f;
            effect.accent = style == PickupStyle.Heart ? PickupPresentation.Heart : PickupPresentation.Warm;
            if (style != PickupStyle.Grounded)
            {
                effect.ghost = PickupPresentation.MakeSprite("Collected item", go.transform, art.sprite, art);
                effect.ghost.transform.localScale = art.transform.lossyScale;
                effect.originalScale = effect.ghost.transform.localScale;
                effect.ghost.color = effect.originalColor = art.color;
                effect.ghost.flipX = art.flipX;
                effect.ghost.flipY = art.flipY;
                effect.ghost.enabled = true;
            }
            if (spark != null)
                for (int i = 0; i < effect.motes.Length; i++)
                {
                    var mote = PickupPresentation.MakeSprite("Reward mote", go.transform, spark, art);
                    PickupPresentation.BindSolidTexture(mote);
                    PickupPresentation.Size(mote, Vector2.one * (i % 3 == 0 ? 2f : 1f) / 16f);
                    mote.sortingOrder = art.sortingOrder + 1;
                    mote.enabled = true;
                    effect.motes[i] = mote;
                }
        }

        private void Update() => Advance(Time.deltaTime);

        private void Advance(float elapsed)
        {
            age += elapsed;
            const float duration = .55f;
            if (age >= duration) { Destroy(gameObject); return; }
            float t = age / duration;
            bool readable = DarkInterior.CanRead(start);
            if (ghost != null)
            {
                ghost.enabled = readable;
                Vector3 offset = (target - start) * (1f - (1f - t) * (1f - t));
                offset.y += Mathf.Sin(t * Mathf.PI) * .2f;
                ghost.transform.localPosition = new Vector3(Mathf.Round(offset.x * 16f) / 16f, Mathf.Round(offset.y * 16f) / 16f, 0);
                ghost.transform.localScale = originalScale * (style == PickupStyle.Coin ? 1f - t * .65f : 1f);
                Color color = originalColor;
                color.a *= Mathf.Clamp01((1f - t) * 2f);
                ghost.color = color;
            }
            for (int i = 0; i < motes.Length; i++)
            {
                var mote = motes[i];
                if (mote == null) continue;
                mote.enabled = readable && FeedbackSettings.FlashScale > 0f;
                float angle = i * Mathf.PI / 3f + Mathf.PI / 6f;
                float radius = .12f + t * (style == PickupStyle.Coin ? .35f : .6f);
                var offset = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius + t * .25f);
                mote.transform.localPosition = new Vector3(Mathf.Round(offset.x * 16f) / 16f, Mathf.Round(offset.y * 16f) / 16f, 0);
                Color color = accent;
                color.a = (1f - t) * Mathf.Clamp01(FeedbackSettings.FlashScale);
                mote.color = color;
            }
        }
    }
}
