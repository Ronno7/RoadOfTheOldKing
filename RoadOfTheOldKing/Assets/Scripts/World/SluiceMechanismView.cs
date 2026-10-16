using UnityEngine;
using RoadOfTheOldKing.UI;

namespace RoadOfTheOldKing.World
{
    // Only visual transforms live here. Receiver colliders and the axe carriage remain gameplay-owned.
    [DisallowMultipleComponent]
    public sealed class SluiceMechanismView : MonoBehaviour
    {
        [SerializeField] private Transform bolt;
        [SerializeField] private Transform brake;
        [SerializeField] private Transform counterweight;
        [SerializeField] private LineRenderer suspension;
        [SerializeField] private Transform suspensionTop;
        [SerializeField] private Transform leftShutter;
        [SerializeField] private Transform rightShutter;
        [SerializeField] private Transform shutterPivot;
        [SerializeField] private LineRenderer leftRod;
        [SerializeField] private LineRenderer rightRod;
        [SerializeField] private Transform outletGate;
        [SerializeField] private Vector3 outletTravel = Vector3.up * 1.5f;
        [Header("Loaded fork confirmation")]
        [SerializeField] private SpriteRenderer distributorForkVisual;
        [SerializeField] private SpriteRenderer mainForkVisual;
        [SerializeField] private Sprite forkSignalSprite;
        private readonly SpriteRenderer[] forkMarks = new SpriteRenderer[4];
        private readonly bool[] forkArmed = new bool[2];
        private readonly float[] forkAge = new float[2];
        private RecallSluice owner;
        private Vector3 boltRest, brakeRest, weightRest;
        private Vector3 leftRest, rightRest, outletRest;
        private float boltAmount, brakeAmount, weightAmount, strainRemaining;
        private bool released, raised, dropped, cached;

        // Exterior water/gate presentation shares the counterweight's actual stroke.
        public float Opening => weightAmount * weightAmount;

        private void Awake()
        {
            owner = GetComponent<RecallSluice>();
            if (forkSignalSprite == null) return;
            for (int i = 0; i < forkMarks.Length; i++)
            {
                var visual = i < 2 ? distributorForkVisual : mainForkVisual;
                if (visual == null) continue;
                forkMarks[i] = PickupPresentation.MakeSprite("Loaded fork inlay", visual.transform, forkSignalSprite, visual);
                PickupPresentation.BindSolidTexture(forkMarks[i]);
            }
        }

        private void Cache()
        {
            if (cached) return;
            cached = true;
            if (bolt != null) boltRest = bolt.localPosition;
            if (brake != null) brakeRest = brake.localPosition;
            if (counterweight != null) weightRest = counterweight.localPosition;
            if (leftShutter != null) leftRest = leftShutter.localPosition;
            if (rightShutter != null) rightRest = rightShutter.localPosition;
            if (outletGate != null) outletRest = outletGate.localPosition;
        }

        public void Present(SluiceStage stage, bool brakeRaised, bool animate)
        {
            Cache();
            released = stage >= SluiceStage.DistributorReleased;
            dropped = stage == SluiceStage.PondLow;
            raised = brakeRaised || dropped;
            if (!animate)
            {
                boltAmount = released ? 1f : 0f;
                brakeAmount = raised ? 1f : 0f;
                weightAmount = dropped ? 1f : 0f;
                strainRemaining = 0f;
            }
            Draw();
        }

        public void SetBrake(bool value)
        {
            Cache();
            raised = value;
        }

        public void Strain()
        {
            Cache();
            if (!dropped) strainRemaining = .24f;
        }

        private void LateUpdate() => Advance(Time.deltaTime);

        private void Advance(float seconds)
        {
            if (seconds <= 0f) return;
            Cache();
            boltAmount = Mathf.MoveTowards(boltAmount, released ? 1f : 0f, seconds / .24f);
            brakeAmount = Mathf.MoveTowards(brakeAmount, raised ? 1f : 0f, seconds / (raised ? .12f : .18f));
            weightAmount = Mathf.MoveTowards(weightAmount, dropped ? 1f : 0f, seconds / .48f);
            strainRemaining = Mathf.Max(0f, strainRemaining - seconds);
            for (int i = 0; i < forkAge.Length; i++) forkAge[i] += seconds;
            Draw();
        }

        private void Draw()
        {
            DrawForkMarks();
            // The gate uses the counterweight's actual presentation stroke, not a second clock.
            float opening = Opening;
            DrawShutter(leftShutter, leftRod, leftRest, -opening);
            DrawShutter(rightShutter, rightRod, rightRest, opening);
            if (outletGate != null) outletGate.localPosition = outletRest + outletTravel * opening;
            if (bolt != null)
            {
                // Retain the retracted bolt as physical evidence of the released mechanism.
                bolt.gameObject.SetActive(true);
                bolt.localPosition = boltRest + Vector3.left * (.5f * Mathf.SmoothStep(0f, 1f, boltAmount));
            }
            if (brake != null)
                brake.localPosition = brakeRest + Vector3.up * (.35f * Mathf.SmoothStep(0f, 1f, brakeAmount));
            if (counterweight != null)
            {
                float tug = strainRemaining > 0f ? Mathf.Sin((.24f - strainRemaining) * Mathf.PI / .24f) * .0625f : 0f;
                counterweight.localPosition = weightRest + Vector3.down * (1.25f * weightAmount * weightAmount + tug);
                if (suspension != null && suspensionTop != null)
                {
                    suspension.SetPosition(0, suspensionTop.position);
                    suspension.SetPosition(1, counterweight.position + Vector3.up * .875f);
                }
            }
        }

        private void DrawForkMarks()
        {
            for (int i = 0; i < forkArmed.Length; i++)
            {
                var visual = i == 0 ? distributorForkVisual : mainForkVisual;
                bool armed = owner != null && owner.IsForkArmed(i == 0 ? SluicePart.DistributorFork : SluicePart.MainFork);
                if (armed && !forkArmed[i]) forkAge[i] = 0f;
                forkArmed[i] = armed;
                // Two steady native-pixel inlays remain readable with flashes reduced/off.
                // A single short settling glint confirms lodging; it never repeats or points out a solution.
                float glint = Mathf.Clamp01(1f - forkAge[i] / .28f) * Mathf.Clamp01(FeedbackSettings.FlashScale);
                for (int side = 0; side < 2; side++)
                {
                    var mark = forkMarks[i * 2 + side];
                    if (mark == null) continue;
                    mark.enabled = armed && visual != null && visual.enabled && !visual.forceRenderingOff;
                    if (!mark.enabled) continue;
                    mark.transform.localPosition = new Vector3((side == 0 ? -5f : 5f) / 16f, 7f / 16f, 0f);
                    PickupPresentation.Size(mark, new Vector2(1f, glint > .5f ? 4f : 2f) / 16f);
                    mark.color = Color.Lerp(new Color32(108, 185, 191, 255), new Color32(207, 248, 255, 255), glint);
                    mark.sortingLayerID = visual.sortingLayerID;
                    mark.sortingOrder = visual.sortingOrder + 1;
                }
            }
        }

        private void DrawShutter(Transform shutter, LineRenderer rod, Vector3 rest, float stroke)
        {
            if (shutter == null) return;
            shutter.localPosition = rest + Vector3.right * (.4375f * stroke);
            if (rod == null || shutterPivot == null) return;
            rod.SetPosition(0, shutterPivot.position);
            rod.SetPosition(1, shutter.position + Vector3.up * .3125f);
        }

        private void OnEnable()
        {
            var sluice = GetComponent<RecallSluice>();
            if (sluice != null) Present(sluice.Stage, sluice.BrakeLifted, false);
        }

        private void OnDisable()
        {
            var sluice = GetComponent<RecallSluice>();
            if (sluice != null) Present(sluice.Stage, sluice.BrakeLifted, false);
            foreach (var mark in forkMarks) if (mark != null) mark.enabled = false;
        }

        private void OnDestroy()
        {
            foreach (var mark in forkMarks) if (mark != null) Destroy(mark.gameObject);
        }
    }
}
