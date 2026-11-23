using UnityEngine;
using RoadOfTheOldKing.UI;

namespace RoadOfTheOldKing.World
{
    public enum SluiceResponse { Loaded, Accepted, Rejected, Turned }

    // Local presentation only: never moves receivers, releases blockers or saves progress.
    [DisallowMultipleComponent, DefaultExecutionOrder(300)]
    public sealed class SluiceFeedback : MonoBehaviour
    {
        [System.Serializable]
        private sealed class Fitting
        {
            public Transform receiver;
            public SpriteRenderer visual;
            public bool crank;
            [Tooltip("Its latch sound's pitch, so parts struck a moment apart still sound like two.")]
            public float pitch = 1f;
            [System.NonSerialized] public AudioSource speaker;
            [Tooltip("A Recall target: it twinkles now and then while its stage still needs it (user, 15 Nov). Cranks twinkle " +
                "on their own while they can be turned.")]
            public bool hint;
            [Tooltip("A crank's hub, in pixels from its sprite's pivot: its lock pin and glint sit on it.")]
            public Vector2 axle;
            [Tooltip("A crank's turn, frame by frame about its hub, with the resting sprite first. Pixel frames, not a " +
                "rotated sprite, which sheared under its stretched parent (user, 22-23 Nov: \"weirdly skewed\").")]
            public Sprite[] turnFrames = System.Array.Empty<Sprite>();
            [System.NonSerialized] public SpriteRenderer pin, shadow;
            [System.NonSerialized] public Vector3 pinRest;
            [System.NonSerialized] public bool wasLatched;
            [System.NonSerialized] public float unlatchIn = -1f, pinAge = 99f;
            [System.NonSerialized] public SluiceCrank crankFitting;
            [System.NonSerialized] public SluiceTarget target;
            [System.NonSerialized] public SpriteRenderer[] hintMarks;
            [System.NonSerialized] public float hintOffset;
            [System.NonSerialized] public Sprite full;
            [System.NonSerialized] public Vector3 rest;
            [System.NonSerialized] public Quaternion rotation;
            [System.NonSerialized] public float age = 1f, cooldown;
            [System.NonSerialized] public SluiceResponse response;
            [System.NonSerialized] public SpriteRenderer[] sparks;
        }

        [SerializeField] private Fitting[] fittings = System.Array.Empty<Fitting>();
        [SerializeField] private Sprite sparkSprite;
        [Tooltip("A crank's bare socket, shown while the crank isn't in it.")]
        [SerializeField] private Sprite bareSocket;
        [Tooltip("Seconds between a Recall target's twinkles; each one lasts .42 s, like a pickup's.")]
        [SerializeField, Min(1f)] private float hintInterval = 3.6f;
        private float hintClock;
        [SerializeField] private SluiceWaterReveal serviceWater, pondWater;
        [SerializeField] private Transform counterweight;
        [SerializeField] private Transform[] passageLeaves = System.Array.Empty<Transform>();
        [SerializeField] private AudioClip latchSound, blockedSound, ratchetSound, weightSound, waterSound, gateSound;
        [Tooltip("A crank's iron lock pin, shown through its wheel until the puzzle frees it.")]
        [SerializeField] private Sprite crankLock;
        [Tooltip("The pin knocked free: a heavy knock and a ringing pin, unlike the parts' latch click.")]
        [SerializeField] private AudioClip unlatchSound;
        [Tooltip("Seconds after the stage frees a crank before its pin comes out, so it isn't lost in the targets' own click.")]
        [SerializeField, Min(0f)] private float unlatchDelay = .7f;
        private const float PinPop = .45f;
        // Where the pin's centre lies, from the hub: its ring outside the rim, its shaft through the hub (CrankLock.png,
        // 19 x 7, centre pivot; whole pixels with the wheel's).
        private static readonly Vector2 PinFromHub = new Vector2(-4.5f, 0f);
        private RecallSluice owner;
        private SluiceMechanismView mechanism;
        private AudioSource impacts, machinery, water;
        private Vector3[] leafPositions;
        private bool[] leafMoving;
        private bool initialized, wasMoving, weightFalling, paused;
        private SluiceStage previousStage;
        private float motionTail;

        private void Awake()
        {
            owner = GetComponent<RecallSluice>();
            mechanism = GetComponent<SluiceMechanismView>();
            impacts = Speaker("Waterworks impacts", false);
            machinery = Speaker("Waterworks gears", true);
            water = Speaker("Waterworks flow", true);
            machinery.clip = ratchetSound;
            water.clip = waterSound;
            leafPositions = new Vector3[passageLeaves.Length];
            leafMoving = new bool[passageLeaves.Length];
            foreach (var f in fittings)
            {
                if (f.visual == null) continue;
                f.rest = f.visual.transform.localPosition;
                f.full = f.visual.sprite;
                if (f.crank && f.receiver != null)
                {
                    f.crankFitting = f.receiver.GetComponent<SluiceCrank>();
                    // The crank's own shadow belongs to the crank: it goes when the crank is taken out.
                    foreach (var r in f.visual.GetComponentsInChildren<SpriteRenderer>(true))
                        if (r != f.visual && r.name.StartsWith("Fitting-shadow")) f.shadow = r;
                    if (crankLock != null)
                    {
                        f.pin = new GameObject("Crank lock pin") { hideFlags = HideFlags.DontSave }.AddComponent<SpriteRenderer>();
                        f.pin.transform.SetParent(f.visual.transform, false);
                        // In world pixels from the wheel's pivot, whatever the wheel's (stretched) scale; drawn at native size.
                        var lossy = f.visual.transform.lossyScale;
                        Vector2 offset = (f.axle + PinFromHub) / 16f;
                        f.pinRest = new Vector3(offset.x / lossy.x, offset.y / lossy.y, 0f);
                        f.pin.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f);
                        f.pin.sprite = crankLock;
                        f.pin.sortingLayerID = f.visual.sortingLayerID;
                        f.pin.sortingOrder = f.visual.sortingOrder + 1;
                        f.pin.sharedMaterial = f.visual.sharedMaterial;
                    }
                }
                f.rotation = f.visual.transform.localRotation;
                // Its own speaker: re-pitching the shared one would bend a latch that's still ringing.
                if (!Mathf.Approximately(f.pitch, 1f))
                {
                    f.speaker = Speaker("Fitting latch", false);
                    f.speaker.pitch = f.pitch;
                }
                if ((f.hint || f.crank) && f.receiver != null)
                {
                    f.target = f.receiver.GetComponent<SluiceTarget>();
                    f.hintMarks = new SpriteRenderer[2];
                    for (int i = 0; i < f.hintMarks.Length; i++)
                    {
                        f.hintMarks[i] = PickupPresentation.MakeSprite("Recall hint", transform, sparkSprite, f.visual);
                        PickupPresentation.BindSolidTexture(f.hintMarks[i]);
                    }
                    // Spread over the cycle, so neighbouring vanes don't blink in unison.
                    f.hintOffset = hintInterval * Mathf.Repeat(f.receiver.position.x * .37f + f.receiver.position.y * .13f, 1f);
                }
                f.sparks = new SpriteRenderer[4];
                for (int i = 0; i < f.sparks.Length; i++)
                {
                    f.sparks[i] = PickupPresentation.MakeSprite("Fitting spark", transform, sparkSprite, f.visual);
                    PickupPresentation.BindSolidTexture(f.sparks[i]);
                    PickupPresentation.Size(f.sparks[i], Vector2.one / 16f);
                }
            }
            initialized = true;
            ResetPresentation();
        }

        private AudioSource Speaker(string label, bool loop)
        {
            var go = new GameObject(label) { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            Audio.AudioChannels.Route(source, Audio.AudioChannel.Effects);
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D camera: attenuate in the map plane, not camera Z.
            return source;
        }

        private float Audibility(Vector3 at)
        {
            var camera = Camera.main;
            return camera == null ? 0f : Mathf.Clamp01(1f - Vector2.Distance(camera.transform.position, at) / 18f);
        }

        private void Sound(AudioClip clip, Vector3 at, float volume, AudioSource speaker = null)
        {
            if (speaker == null) speaker = impacts;
            if (clip == null || speaker == null || Time.timeScale <= 0f) return;
            float gain = Audibility(at) * volume;
            if (gain > .001f) speaker.PlayOneShot(clip, gain);
        }

        public void Respond(SluiceResponse response, Transform receiver)
        {
            if (!initialized || !isActiveAndEnabled || Time.timeScale <= 0f) return;
            foreach (var f in fittings)
            {
                if (f.receiver != receiver || f.visual == null) continue;
                if (f.cooldown > 0f && response == SluiceResponse.Rejected) return;
                f.age = 0f;
                f.cooldown = .18f;
                f.response = response;
                Sound(response == SluiceResponse.Rejected ? blockedSound : latchSound,
                    receiver.position, response == SluiceResponse.Rejected ? .55f : .65f,
                    response == SluiceResponse.Rejected ? null : f.speaker);
                if (response == SluiceResponse.Turned) motionTail = .65f;
                return;
            }
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (Time.timeScale <= 0f)
            {
                if (!paused) { impacts.Pause(); machinery.Pause(); water.Pause(); paused = true; }
                return;
            }
            if (paused) { impacts.UnPause(); machinery.UnPause(); water.UnPause(); paused = false; }
            Advance(Time.deltaTime);
        }

        private void Advance(float seconds)
        {
            if (!initialized || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            motionTail = Mathf.Max(0f, motionTail - seconds);
            hintClock += seconds;
            foreach (var f in fittings)
            {
                f.age += seconds;
                f.cooldown = Mathf.Max(0f, f.cooldown - seconds);
                Unlatch(f, seconds);
                Draw(f);
            }
            bool moving = owner != null && owner.IsMoving;
            if (wasMoving && !moving)
            {
                var at = owner != null ? owner.transform.position : transform.position;
                foreach (var f in fittings) if (f.crank && f.receiver != null) at = f.receiver.position;
                Sound(latchSound, at, .55f);
            }
            wasMoving = moving;
            if (owner != null && owner.Stage != previousStage)
            {
                weightFalling = owner.Stage == SluiceStage.PondLow;
                previousStage = owner.Stage;
            }
            if (weightFalling && mechanism != null && mechanism.Opening >= .999f)
            {
                weightFalling = false;
                Sound(weightSound, counterweight != null ? counterweight.position : transform.position, .8f);
            }
            bool gateMoving = false;
            for (int i = 0; i < passageLeaves.Length; i++)
            {
                var leaf = passageLeaves[i];
                if (leaf == null) continue;
                bool changed = (leaf.localPosition - leafPositions[i]).sqrMagnitude > .000001f;
                if (changed != leafMoving[i]) Sound(changed ? gateSound : weightSound, leaf.position, changed ? .45f : .3f);
                leafPositions[i] = leaf.localPosition;
                leafMoving[i] = changed;
                gateMoving |= changed;
            }
            Vector3 machineAt = counterweight != null ? counterweight.position : transform.position;
            Loop(machinery, moving || gateMoving || motionTail > 0f || weightFalling, .32f * Audibility(machineAt), seconds);
            bool draining = serviceWater != null && serviceWater.IsDraining || pondWater != null && pondWater.IsDraining;
            float flow = pondWater != null && pondWater.IsDraining ? .48f * (1f - .65f * pondWater.Progress) : .3f;
            Loop(water, draining, flow * Audibility(machineAt), seconds);
        }

        private static void Loop(AudioSource source, bool playing, float volume, float seconds)
        {
            if (source == null || source.clip == null) return;
            source.volume = Mathf.MoveTowards(source.volume, playing ? volume : 0f, seconds * 2f);
            if (playing && source.volume > .001f && !source.isPlaying) source.Play();
            if (!playing && source.volume <= .001f) source.Stop();
        }

        private void Draw(Fitting f)
        {
            if (f.visual == null) return;
            // A socket shows the crank only while the one crank sits in it; otherwise its bare hub.
            if (f.crankFitting != null && bareSocket != null) f.visual.sprite = f.crankFitting.HoldsCrank ? f.full : bareSocket;
            bool rejected = f.response == SluiceResponse.Rejected;
            // Only a real turn spins a crank; seating or taking it out just clicks.
            bool turning = f.crank && f.response == SluiceResponse.Turned;
            float duration = turning ? .6f : .32f;
            float t = Mathf.Clamp01(f.age / duration);
            // A click jolts the part sideways; a turn only spins, so the crank doesn't wobble off its axle.
            float recoil = t < 1f && !turning ? Mathf.Sin(t * Mathf.PI * (rejected ? 4f : 2f)) * (1f - t) : 0f;
            // Only the visual child moves. Pixel steps keep the fixed receiver honest.
            f.visual.transform.localPosition = f.rest + Vector3.right * (Mathf.Round(recoil * 2f) / 16f);
            f.visual.transform.localRotation = f.rotation;
            // A turn plays the crank's frames about its hub, once round.
            if (turning && t < 1f && f.turnFrames.Length > 0 && (f.crankFitting == null || f.crankFitting.HoldsCrank))
                f.visual.sprite = f.turnFrames[Mathf.Min(f.turnFrames.Length - 1, (int)(t * f.turnFrames.Length))];
            if (f.shadow != null) f.shadow.enabled = f.crankFitting == null || f.crankFitting.HoldsCrank;
            DrawPin(f);
            DrawHint(f);
            if (f.sparks == null) return;
            for (int i = 0; i < f.sparks.Length; i++)
            {
                var mark = f.sparks[i];
                bool visible = !rejected && f.age < .28f && f.visual.enabled && !f.visual.forceRenderingOff &&
                    f.visual.gameObject.activeInHierarchy && FeedbackSettings.FlashScale > 0f;
                mark.enabled = visible;
                if (!visible) continue;
                float step = Mathf.Floor(f.age * 20f);
                Vector3 ray = new Vector3(i % 2 == 0 ? -1f : 1f, i < 2 ? 1f : -.5f, 0f);
                var center = f.visual.bounds.center;
                mark.transform.position = new Vector3(Mathf.Round(center.x * 16f), Mathf.Round(center.y * 16f), center.z * 16f) / 16f +
                    ray * ((2f + step) / 16f);
                mark.sortingLayerID = f.visual.sortingLayerID;
                mark.sortingOrder = f.visual.sortingOrder + 2;
                mark.color = new Color(211f / 255f, 177f / 255f, 132f / 255f,
                    f.visual.color.a * Mathf.Clamp01(FeedbackSettings.FlashScale));
            }
        }

        // Is this target still part of the puzzle? Nothing glints while the guardian holds the controls, and the second
        // bay's two vanes glint together, never in order, so the order stays the player's to find.
        private bool HintWanted(Fitting f)
        {
            if (owner == null || owner.IsGuarded) return false;
            // A crank glints while its stage lets it turn: the puzzle's next step after its targets (user, 22 Nov).
            if (f.crank) return f.crankFitting != null && f.crankFitting.ReadyToTurn && !f.wasLatched && f.pinAge >= PinPop;
            if (f.target == null) return false;
            switch (f.target.Part)
            {
                case SluicePart.DistributorVane: return owner.Stage == SluiceStage.High;
                case SluicePart.BrakeVane:
                case SluicePart.CatchVane: return owner.Stage == SluiceStage.ServiceDiverted;
                default: return owner.Stage < SluiceStage.PondLow;
            }
        }

        // A pickup's twinkle (PickupPresentation): a warm cross at the part's upper corner, grown for a moment, then gone.
        private void DrawHint(Fitting f)
        {
            if (f.hintMarks == null) return;
            float cycle = Mathf.Repeat(hintClock + f.hintOffset, hintInterval);
            bool shine = sparkSprite != null && cycle < .42f && FeedbackSettings.FlashScale > 0f && HintWanted(f) &&
                f.visual.enabled && !f.visual.forceRenderingOff && f.visual.gameObject.activeInHierarchy;
            var bounds = f.visual.bounds;
            // A crank glints at its hub's upper edge, which the crank and the bare socket share; others at an upper corner.
            Vector3 at = f.crank ? f.visual.transform.TransformPoint((Vector3)(f.axle / 16f)) + new Vector3(2.5f, 2.5f) / 16f
                : bounds.center + Vector3.Scale(bounds.extents, new Vector3(.6f, .6f));
            Vector3 tip = new Vector3(Mathf.Round(at.x * 16f) / 16f, Mathf.Round(at.y * 16f) / 16f, bounds.center.z);
            int length = cycle > .09f && cycle < .28f ? 3 : 1;
            var accent = PickupPresentation.Warm;
            accent.a = Mathf.Clamp01(FeedbackSettings.FlashScale) * f.visual.color.a;
            for (int i = 0; i < f.hintMarks.Length; i++)
            {
                var mark = f.hintMarks[i];
                mark.enabled = shine;
                if (!shine) continue;
                mark.transform.position = tip;
                PickupPresentation.Size(mark, (i == 0 ? new Vector2(length, 1) : new Vector2(1, length)) / 16f);
                mark.color = accent;
                mark.sortingLayerID = f.visual.sortingLayerID;
                mark.sortingOrder = f.visual.sortingOrder + 2;
            }
        }

        // A stage that frees a crank knocks its lock pin out, a moment after the targets' own click, with its own heavier
        // sound and sparks; only then does the crank glint (user, 23 Nov: the click "plays at the same moment as the other
        // click and it still isnt clear that the crank has unlocked"). A rest that pins it again puts the pin back.
        private void Unlatch(Fitting f, float seconds)
        {
            if (!f.crank || f.crankFitting == null) return;
            f.pinAge += seconds;
            if (f.crankFitting.Latched) { f.wasLatched = true; f.unlatchIn = -1f; return; }
            if (!f.wasLatched) return;
            if (f.unlatchIn < 0f) f.unlatchIn = unlatchDelay;
            f.unlatchIn -= seconds;
            if (f.unlatchIn > 0f) return;
            f.wasLatched = false;
            f.unlatchIn = -1f;
            f.pinAge = 0f;
            f.age = 0f;
            f.cooldown = .18f;
            f.response = SluiceResponse.Accepted;
            Sound(unlatchSound != null ? unlatchSound : latchSound, f.receiver.position, .8f);
        }

        // In while latched; knocked out, it slides back through its ring and drops away, fading.
        private void DrawPin(Fitting f)
        {
            if (f.pin == null) return;
            bool popping = !f.wasLatched && f.pinAge < PinPop;
            f.pin.enabled = (f.wasLatched || popping) && f.visual.enabled && f.visual.gameObject.activeInHierarchy;
            if (!f.pin.enabled) return;
            float t = popping ? Mathf.Clamp01(f.pinAge / PinPop) : 0f;
            var lossy = f.visual.transform.lossyScale;
            f.pin.transform.localPosition = f.pinRest + new Vector3(-Mathf.Round(t * 7f) / 16f / lossy.x, -Mathf.Round(t * t * 5f) / 16f / lossy.y, 0f);
            var color = f.visual.color;
            color.a *= 1f - t * t;
            f.pin.color = color;
        }

        public void ResetPresentation()
        {
            if (!initialized) return;
            foreach (var f in fittings)
            {
                f.age = 1f;
                f.cooldown = 0f;
                // As the scene loads or resets: pinned or free as the stage stands, with nothing to animate.
                f.wasLatched = f.crankFitting != null && f.crankFitting.Latched;
                f.unlatchIn = -1f;
                f.pinAge = 99f;
                Draw(f);
            }
            impacts.Stop(); machinery.Stop(); water.Stop();
            foreach (var f in fittings) if (f.speaker != null) f.speaker.Stop();
            machinery.volume = water.volume = 0f;
            wasMoving = weightFalling = paused = false;
            motionTail = 0f;
            previousStage = owner != null ? owner.Stage : SluiceStage.High;
            for (int i = 0; i < passageLeaves.Length; i++)
            {
                if (passageLeaves[i] != null) leafPositions[i] = passageLeaves[i].localPosition;
                leafMoving[i] = false;
            }
        }

        private void OnDisable() => ResetPresentation();
        private void OnDestroy()
        {
            foreach (var f in fittings)
                if (f.sparks != null) foreach (var mark in f.sparks) if (mark != null) Destroy(mark.gameObject);
            foreach (var f in fittings)
                if (f.hintMarks != null) foreach (var mark in f.hintMarks) if (mark != null) Destroy(mark.gameObject);
            foreach (var f in fittings) if (f.pin != null) Destroy(f.pin.gameObject);
            if (impacts != null) Destroy(impacts.gameObject);
            if (machinery != null) Destroy(machinery.gameObject);
            if (water != null) Destroy(water.gameObject);
            foreach (var f in fittings) if (f.speaker != null) Destroy(f.speaker.gameObject);
        }
    }
}
