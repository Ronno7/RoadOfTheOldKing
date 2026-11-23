using RoadOfTheOldKing.World;
using UnityEngine;

namespace RoadOfTheOldKing.Player
{
    // Kicks up a puff of dust at the feet when a dodge starts. Presentation only: it samples the dash's sequence.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerDash))]
    public sealed class DashDust : MonoBehaviour
    {
        [SerializeField] private SpriteBurst dust;
        private PlayerDash dash;
        private uint seen;

        private void Awake() { dash = GetComponent<PlayerDash>(); seen = dash.Sequence; }

        private void LateUpdate()
        {
            if (dash.Sequence == seen) return;
            seen = dash.Sequence;
            if (dust != null && dash.IsDashing) dust.Play(transform.position, dash.Direction.x > 0f);
        }
    }
}
