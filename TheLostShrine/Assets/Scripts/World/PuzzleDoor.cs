using UnityEngine;

namespace TheLostShrine.World
{
    public sealed class PuzzleDoor : MonoBehaviour
    {
        [SerializeField] private GameObject barrier;
        [SerializeField] private TextMesh label;
        public bool IsOpen { get; private set; }
        public void SetOpen(bool open)
        {
            IsOpen = open;
            if (barrier != null)
                barrier.SetActive(!open);
            if (label != null)
                label.text = open ? "OPEN" : "SEALED";
        }
    }
}
