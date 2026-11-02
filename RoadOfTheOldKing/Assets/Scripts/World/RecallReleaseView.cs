using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Presentation samples the mechanism's progress; it never runs a second timer or opens collision.
    [DisallowMultipleComponent]
    public sealed class RecallReleaseView : MonoBehaviour
    {
        [SerializeField] private RecallReleaseArt art;
        [SerializeField] private SpriteRenderer floor, backRail, frontRail, hinge, release;
        [SerializeField] private Transform groundAnchor;
        [SerializeField] private LineRenderer suspensionRope;
        [SerializeField] private Transform ropeWinch, ropeSupport, ropeRaisedTip;
        private RecallRelease mechanism;
        private PermanentPassage passage;
        private PlayerMovement player;
        private SpriteRenderer playerBody;
        private readonly Vector3[] ropePoints = new Vector3[4];

        private bool Released => mechanism != null ? mechanism.IsReleased : passage != null && passage.IsOpen;
        private float Progress => mechanism != null ? mechanism.ReleaseProgress : passage != null ? passage.OpeningProgress : 0f;
        public int FrameIndex => !Released ? 0 :
            Progress >= 1f ? 6 : 1 + Mathf.Min(4, Mathf.FloorToInt(Progress * 5f));

        private void OnEnable()
        {
            mechanism = GetComponent<RecallRelease>();
            passage = GetComponent<PermanentPassage>();
            player = FindFirstObjectByType<PlayerMovement>();
            if (player != null) playerBody = player.GetComponentInChildren<SpriteRenderer>();
            RefreshPresentation();
        }

        private void LateUpdate() => RefreshPresentation();

        public void RefreshPresentation()
        {
            if (mechanism == null) mechanism = GetComponent<RecallRelease>();
            if (passage == null) passage = GetComponent<PermanentPassage>();
            if (art == null) return;
            int frame = FrameIndex;
            Apply(floor, art.floor, frame);
            Apply(backRail, art.backRail, frame);
            Apply(frontRail, art.frontRail, frame);
            Apply(hinge, art.hinge, frame);
            Apply(release, art.release, frame);
            if (suspensionRope != null && ropeWinch != null && ropeSupport != null && ropeRaisedTip != null)
            {
                // The hand release lets the free end fall beside the post while the deck lowers.
                // Sample the owning mechanism so restore/load never leaves a taut raised cable.
                float slack = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Progress * 3f));
                Vector3 tip = Vector3.Lerp(ropeRaisedTip.position,
                    ropeSupport.position + new Vector3(-.25f, -1.25f), slack);
                Vector3 middle = Vector3.Lerp(ropeSupport.position, tip, .5f) + Vector3.down * (.3f * slack);
                suspensionRope.positionCount = 4;
                ropePoints[0] = ropeWinch.position;
                ropePoints[1] = ropeSupport.position;
                ropePoints[2] = middle;
                ropePoints[3] = tip;
                for (int i = 0; i < ropePoints.Length; i++)
                    suspensionRope.SetPosition(i, new Vector3(Mathf.Round(ropePoints[i].x * 16f) / 16f,
                        Mathf.Round(ropePoints[i].y * 16f) / 16f, ropePoints[i].z));
            }
            bool landed = Released && Progress >= 1f;
            if (backRail != null)
            {
                backRail.sortingLayerName = landed ? "World" : "Player";
                backRail.sortingOrder = landed ? 1 : 100;
            }
            if (floor != null)
            {
                floor.sortingLayerName = landed ? "World" : "Player";
                float feet = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
                floor.sortingOrder = landed ? 0 : playerBody == null ? 5 :
                    playerBody.sortingOrder + (player.transform.position.y > feet ? 5 : -3);
            }
        }

        private static void Apply(SpriteRenderer renderer, Sprite[] frames, int index)
        {
            if (renderer != null && frames != null && index < frames.Length)
                renderer.sprite = frames[index];
        }
    }
}
