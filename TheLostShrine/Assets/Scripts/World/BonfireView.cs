using UnityEngine;

namespace TheLostShrine.World
{
    [RequireComponent(typeof(Bonfire))]
    public sealed class BonfireView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer flame;
        [SerializeField] private TextMesh label;
        private Bonfire fire;
        private Vector3 flameScale;
        private void Awake()
        {
            fire = GetComponent<Bonfire>();
            if (flame != null)
                flameScale = flame.transform.localScale;
        }
        private void LateUpdate()
        {
            if (flame != null)
            {
                flame.color = fire.IsDiscovered ? new Color(1f, 0.7f, 0.15f) : new Color(0.6f, 0.35f, 0.15f);
                flame.transform.localScale = flameScale * (1f + Mathf.Sin(Time.time * 8f) * 0.06f);
            }
            if (label != null)
                label.text = fire.DisplayName + (fire.IsDiscovered ? "\nF - REST / TRAVEL" : "\nF - LIGHT & REST");
        }
    }
}
