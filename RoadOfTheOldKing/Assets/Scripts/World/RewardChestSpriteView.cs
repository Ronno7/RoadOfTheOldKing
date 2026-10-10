using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Presentation only: the reward commits immediately; loading never replays the opening.
    [DisallowMultipleComponent]
    public sealed class RewardChestSpriteView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite closed, halfOpen, open;
        private float clock;
        private bool opened;

        public void SetOpen(bool value)
        {
            opened = value;
            if (body != null) body.sprite = value ? open : closed;
            enabled = false;
        }

        public void PlayOpening()
        {
            opened = true;
            clock = 0;
            if (body != null) body.sprite = closed;
            enabled = true;
        }

        private void Update() => Advance(Time.deltaTime);
        private void Advance(float elapsed)
        {
            clock += elapsed;
            if (clock >= .22f) SetOpen(true);
            else if (body != null) body.sprite = clock < .07f ? closed : halfOpen;
        }

        private void OnDisable()
        {
            if (body != null) body.sprite = opened ? open : closed;
        }
    }
}
