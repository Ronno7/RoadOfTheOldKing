using System;
using System.Text.RegularExpressions;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Title screen (foundation): Continue resumes at the last rest, New Game starts over (confirmed when a
    // save exists), Settings opens the shared settings page, Quit leaves. Presentation only; GameSession loads the scenes.
    // Keyboard/controller input comes from the PauseMenuInput beside it; Escape or B steps back from the confirmation.
    // Opening (user, 17 Oct): when the game boots into this scene, the logo waits for any button before the menu
    // appears. New Game plays the text prologue (pages from a TextAsset) before the run starts; Escape/Start skips it.
    // The prologue touches no save: an existing one is only erased when the run actually starts.
    // After it, the hoodie picker (user, 22 Oct; reworked 15 Nov): swatches plus hue, saturation and brightness bars over a
    // live preview of the hero, fixed for the run. Settings and the picker are option pages (OptionList).
    [DisallowMultipleComponent, RequireComponent(typeof(UIDocument), typeof(PauseMenuInput))]
    public sealed class TitleScreen : MonoBehaviour, IModalMenu
    {
        [Tooltip("Prologue pages: paragraphs separated by blank lines; lines starting with # are ignored.")]
        [SerializeField] private TextAsset prologue;
        [Tooltip("Seconds for a prologue page to fade in (and for the last one to fade out).")]
        [SerializeField, Min(.05f)] private float pageFade = .8f;
        [Tooltip("The hero's standing south view, recoloured live by the hoodie picker. Empty skips the picker.")]
        [SerializeField] private Sprite hoodPreview;
        [Tooltip("Screen pixels per art pixel for the preview, in UI points (the kit draws 2 points per pixel).")]
        [SerializeField, Min(1)] private int hoodPreviewScale = 4;

        [System.Serializable]
        private sealed class Banner
        {
            public Texture2D[] frames = System.Array.Empty<Texture2D>();
            [Tooltip("Seconds each frame shows, in order. A shorter list repeats its last value.")]
            public float[] seconds = { .17f };
        }

        [Header("Backdrop")]
        [Tooltip("The README paintings, cycling behind the logo and menu (user, 15 Nov), at the whole screen-pixel scale that fills the width.")]
        [SerializeField] private Banner[] backdrop = System.Array.Empty<Banner>();
        [SerializeField, Min(1f)] private float backdropHold = 8f;
        [SerializeField, Min(.1f)] private float backdropFade = 1.2f;
        [Tooltip("The painting is the title's centrepiece; the menu reads over a stepped shade instead of dimming it.")]
        [SerializeField, Range(0f, 1f)] private float backdropOpacity = 1f;

        private PauseMenuInput input;
        private MenuList list;
        private enum Page { Press, Main, Confirm, Settings, Prologue, Hood }
        private Page page;
        private PauseMenuAction[] current = new PauseMenuAction[0];
        private VisualElement panel, prologueRoot, hoodView, logo, actionsArea, optionsArea;
        private Label press, prologueText, panelHeading, hint;
        private OptionList options;
        private BannerLayer backdropFront, backdropBack;
        private int backdropIndex;
        private float backdropTime;
        private bool backdropFading;
        private Texture2D shadeTexture;
        private VisualElement titleRoot, backdropRoot, shadeRoot;
        private Label descriptionLabel;
        // The hood as hue, saturation and brightness, the bars Stardew Valley's creator uses, plus swatches for
        // quick, sensible picks. Track segments divide the 200-point bar evenly: 25 for hue, 10 for the others.
        private const int HueSteps = 24, ShadeSteps = 9;
        private float hoodHue, hoodSaturation, hoodBrightness;
        private int hoodSwatch;
        // Untouched (or back on the first swatch), the run keeps the drawn blue exactly: its ramp isn't a pure shade.
        private bool hoodChanged;
        private static readonly Color[] HoodSwatches =
        {
            HoodieColor.Drawn, new Color32(0xA2, 0x3B, 0x3B, 255), new Color32(0xB5, 0x60, 0x2F, 255), new Color32(0xC4, 0x9A, 0x3C, 255),
            new Color32(0x5F, 0x7A, 0x3A, 255), new Color32(0x2F, 0x5E, 0x3E, 255), new Color32(0x2E, 0x7C, 0x7A, 255),
            new Color32(0x6E, 0x4A, 0x80, 255), new Color32(0x5B, 0x66, 0x70, 255), new Color32(0x34, 0x36, 0x3C, 255)
        };
        private Texture2D hoodSource, hoodShown;
        private Color32[] hoodPixels;
        private IDisposable anyButton;
        private int revealedFrame = -1;
        private UnityEngine.InputSystem.Controls.ButtonControl revealOnRelease;
        private string[] pages = new string[0];
        private int pageIndex;
        private float pageShownAt, leavingAt = -1f;
        private bool runStarted;

        // The "press any button" gate shows once, and only when the game started in this scene (not on a return
        // to the title, nor in a fixture that boots elsewhere).
        private static string bootScene;
        private static bool pressed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { bootScene = null; pressed = false; }

        // Before the first scene's Awake/OnEnable: an AfterSceneLoad hook ran too late, so the gate never showed.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RecordBootScene() => bootScene = SceneManager.GetActiveScene().path;

        private void OnEnable()
        {
            input = GetComponent<PauseMenuInput>();
            var root = GetComponent<UIDocument>().rootVisualElement;
            list = new MenuList(root.Q("actions"), root.Q<Label>("description"), Run);
            panel = root.Q("title-panel");
            press = root.Q<Label>("press");
            prologueRoot = root.Q("prologue");
            prologueText = root.Q<Label>("prologue-text");
            hoodView = root.Q("hood-preview");
            logo = root.Q("title");
            actionsArea = root.Q("actions");
            optionsArea = root.Q("options");
            panelHeading = root.Q<Label>("panel-heading");
            hint = root.Q<Label>("hint");
            descriptionLabel = root.Q<Label>("description");
            options = new OptionList(optionsArea, descriptionLabel);
            titleRoot = root.Q("title-root");
            backdropRoot = root.Q("backdrop"); shadeRoot = root.Q("title-shade");
            var panelSettings = GetComponent<UIDocument>().panelSettings;
            backdropFront = new BannerLayer(root.Q("backdrop-a"), panelSettings);
            backdropBack = new BannerLayer(root.Q("backdrop-b"), panelSettings);
            BuildShade(root.Q("title-shade"));
            backdropIndex = 0; backdropTime = 0f; backdropFading = false;
            if (backdrop.Length > 0) backdropFront.Show(backdrop[0], backdropOpacity);
            // The prologue covers the menu and takes clicks too: a click turns the page like A/Enter.
            prologueRoot.RegisterCallback<PointerDownEvent>(_ => { if (page == Page.Prologue) AdvancePrologue(); });
            // Any key, button or click opens the menu (onAnyButtonPress), so the prompt says so (user, 22 Nov: "start screen says press enter but any button works").
            ControlGlyphs.Bind(press, "Press {anybutton}");
            ControlGlyphs.Bind(root.Q<Label>("prologue-hint"), "{select} Next");
            ControlGlyphs.Bind(root.Q<Label>("prologue-skip"), "{skip} Skip");
            var version = root.Q<Label>("version");
            if (version != null) version.text = "v" + Application.version;
            input.ToggleRequested += OnToggle;
            input.BackRequested += Back;
            input.NavigationRequested += Navigate;
            input.SubmitRequested += Submit;
            input.AdjustRequested += Adjust;
            input.AdjustRepeated += AdjustRepeat;
            MenuStack.Push(this);
            if (!pressed && bootScene == gameObject.scene.path) ShowPress();
            else ShowMain();
        }

        private void OnDisable()
        {
            input.ToggleRequested -= OnToggle;
            input.BackRequested -= Back;
            input.NavigationRequested -= Navigate;
            input.SubmitRequested -= Submit;
            input.AdjustRequested -= Adjust;
            input.AdjustRepeated -= AdjustRepeat;
            anyButton?.Dispose(); anyButton = null;
            if (hoodSource != null) Destroy(hoodSource);
            if (hoodShown != null) Destroy(hoodShown);
            if (shadeTexture != null) Destroy(shadeTexture);
            MenuStack.Remove(this);
        }

        private static GameSession Session => GameSession.Instance;
        private static bool HasSave => Session != null && Session.HasSave;

        // Rows of the main page that other pages return to.
        private const int NewGameRow = 1, SettingsRow = 2;

        private void ShowPress()
        {
            ShowMain(); // build the rows now, so the hidden panel already has its final height
            page = Page.Press;
            // Hidden but still laid out, so the logo stays put when the menu appears.
            panel.style.visibility = Visibility.Hidden; SetVisible(press, true);
            // A mouse button reveals on its release: revealed on the press, the release landed on the row now under the
            // cursor (18 Nov). Keys and pad buttons reveal at once; their submit is guarded by revealedFrame.
            anyButton = InputSystem.onAnyButtonPress.CallOnce(control =>
            {
                if (control.device is Mouse && control is UnityEngine.InputSystem.Controls.ButtonControl button) revealOnRelease = button;
                else Reveal();
            });
        }

        // The button that reveals the menu must not also choose from it this frame (PauseMenuInput reads it too).
        private void Reveal()
        {
            if (page != Page.Press) return;
            pressed = true; revealedFrame = Time.frameCount;
            anyButton?.Dispose(); anyButton = null;
            SetVisible(press, false); panel.style.visibility = Visibility.Visible;
            ShowMain();
        }

        private void ShowMain(int select = -1)
        {
            Show(Page.Main, new[]
            {
                new PauseMenuAction("Continue", Session != null && Session.HasResumePoint ? "Resume where you left off." : "Resume at your last rest.",
                    () => Session.Continue(), HasSave),
                new PauseMenuAction("New Game", "", NewGame, Session != null),
                new PauseMenuAction("Settings", "", () => ShowSettings(0)),
                new PauseMenuAction("Quit", "", Quit)
            }, select >= 0 ? select : HasSave ? 0 : NewGameRow);
        }

        private void NewGame()
        {
            // A save this version can't read still belongs to the player (it may be from a newer build): confirm too.
            bool unreadable = Session != null && Session.SaveUnreadable;
            if (!HasSave && !unreadable) { BeginPrologue(); return; }
            Show(Page.Confirm, new[]
            {
                new PauseMenuAction("Keep my save", unreadable ? "This version can't read your save." : "", () => ShowMain(NewGameRow)),
                new PauseMenuAction("Erase and begin", unreadable ? "This replaces the save it can't read." : "This erases your save.", BeginPrologue)
            }, 0);
        }

        private void ShowSettings(int select)
        {
            page = Page.Settings;
            ShowOptionsPage("SETTINGS", SettingsOptions.Create(() => ShowMain(SettingsRow)), select, false);
        }

        private void Show(Page next, PauseMenuAction[] actions, int select)
        {
            page = next;
            current = actions;
            ShowListLayout();
            list.Show(actions, select);
        }

        private void Run(PauseMenuAction action) => action.Execute();

        private bool OnOptionsPage => page == Page.Settings || page == Page.Hood;

        // Option pages (settings, the hood) drop the logo for a heading and a wider panel; list pages bring it back.
        private void ShowOptionsPage(string heading, OptionList.Row[] rows, int select, bool preview)
        {
            SetVisible(logo, false);
            SetVisible(panelHeading, true); panelHeading.text = heading;
            SetVisible(hoodView, preview);
            SetVisible(actionsArea, false); SetVisible(optionsArea, true);
            // A page whose rows explain nothing keeps no empty space for an explanation.
            SetVisible(descriptionLabel, System.Array.Exists(rows, r => !string.IsNullOrEmpty(r.Description)));
            panel.AddToClassList("options-open");
            panel.RemoveFromClassList("title-plain");
            titleRoot?.AddToClassList("options-mode");
            options.Show(rows, select);
            ControlGlyphs.Bind(hint, ""); // Rows show their own ‹ › arrows; confirm and back follow convention.
        }

        private void ShowListLayout()
        {
            SetVisible(logo, true); SetVisible(panelHeading, false); SetVisible(hoodView, false);
            SetVisible(actionsArea, true); SetVisible(optionsArea, false); SetVisible(descriptionLabel, true);
            panel.RemoveFromClassList("options-open");
            // The main and confirm pages are plain text over the painting's shade, as title screens usually are.
            panel.AddToClassList("title-plain");
            titleRoot?.RemoveFromClassList("options-mode");
            ControlGlyphs.Bind(hint, "");
        }

        private void BeginPrologue()
        {
            pages = Pages(prologue);
            if (pages.Length == 0) { ShowHood(); return; }
            page = Page.Prologue;
            pageIndex = 0; leavingAt = -1f;
            SetVisible(prologueRoot, true);
            ShowPage();
        }

        private void ShowPage()
        {
            prologueText.text = pages[pageIndex];
            prologueText.style.opacity = 0f;
            pageShownAt = Time.unscaledTime;
        }

        // A/Enter first completes a fading page, then turns it; after the last page the text fades and the run starts.
        private void AdvancePrologue()
        {
            if (leavingAt >= 0f) return;
            // Completing a fade marks the page as shown long ago, so a second press always turns it.
            if (Time.unscaledTime - pageShownAt < pageFade) { pageShownAt = float.NegativeInfinity; return; }
            if (pageIndex < pages.Length - 1) { pageIndex++; ShowPage(); }
            else leavingAt = Time.unscaledTime;
        }

        private void Update()
        {
            AnimateBackdrop(Time.unscaledDeltaTime);
            if (revealOnRelease != null && !revealOnRelease.isPressed) { revealOnRelease = null; Reveal(); }
            if (page == Page.Press)
            {
                // A slow blink, as on old title screens.
                press.style.opacity = Mathf.Repeat(Time.unscaledTime, 1.2f) < .8f ? 1f : 0f;
                return;
            }
            if (page != Page.Prologue) return;
            if (leavingAt >= 0f)
            {
                float t = (Time.unscaledTime - leavingAt) / pageFade;
                prologueText.style.opacity = Mathf.Clamp01(1f - t);
                if (t >= 1f) ShowHood();
                return;
            }
            prologueText.style.opacity = Mathf.Clamp01((Time.unscaledTime - pageShownAt) / pageFade);
        }

        private void StartRun()
        {
            if (runStarted || Session == null) return;
            // A refused start (a load already under way, a save that can't be cleared) leaves Begin pressable again.
            runStarted = Session.StartNewRun(page == Page.Hood && hoodChanged ? HoodieColor.ToHex(HoodColor) : "");
        }

        // The hoodie picker: the hero in the colour being chosen, over swatches, hue, saturation and brightness, and Begin.
        private void ShowHood()
        {
            if (runStarted || Session == null) return;
            if (hoodPreview == null || !BuildHoodPreview()) { StartRun(); return; }
            SetSwatch(0);
            SetVisible(prologueRoot, false);
            // The hero alone on the ink field: the paintings belong to the title, not the choice.
            SetVisible(backdropRoot, false); SetVisible(shadeRoot, false);
            page = Page.Hood;
            ShowOptionsPage("YOUR CLOAK", new OptionList.Row[]
            {
                new OptionList.Swatches("Colour", "", HoodSwatches, () => hoodSwatch, SetSwatch),
                new OptionList.Slider("Hue", "", () => hoodHue, v => SetHood(v, hoodSaturation, hoodBrightness), HueSteps,
                    null, i => Color.HSVToRGB(i / (float)HueSteps % 1f, 1f, 1f)),
                new OptionList.Slider("Saturation", "", () => hoodSaturation, v => SetHood(hoodHue, v, hoodBrightness), ShadeSteps,
                    null, i => Color.HSVToRGB(hoodHue, i / (float)ShadeSteps, Mathf.Max(.4f, hoodBrightness))),
                new OptionList.Slider("Brightness", "", () => hoodBrightness, v => SetHood(hoodHue, hoodSaturation, v), ShadeSteps,
                    null, i => Color.HSVToRGB(hoodHue, hoodSaturation, i / (float)ShadeSteps)),
                new OptionList.Command("Random", "", RandomHood),
                new OptionList.Command("Begin", "", StartRun)
            }, 0, true);
            RecolorHood();
        }

        private Color HoodColor => Color.HSVToRGB(hoodHue, hoodSaturation, hoodBrightness);

        private void SetSwatch(int index)
        {
            hoodSwatch = index;
            Color.RGBToHSV(HoodSwatches[index], out hoodHue, out hoodSaturation, out hoodBrightness);
            hoodChanged = index != 0;
            RecolorHood();
        }

        // A random cloak (user, 18 Nov: "random color button on color selection screen"), on the bars' own steps and out of
        // their grey and near-black ends, so every roll looks like a pick.
        private void RandomHood()
        {
            SetHood(UnityEngine.Random.Range(0, HueSteps) / (float)HueSteps,
                UnityEngine.Random.Range(3, ShadeSteps + 1) / (float)ShadeSteps,
                UnityEngine.Random.Range(4, ShadeSteps + 1) / (float)ShadeSteps);
            options.RefreshAll();
        }

        private void SetHood(float hue, float saturation, float brightness)
        {
            hoodHue = Mathf.Clamp01(hue); hoodSaturation = Mathf.Clamp01(saturation); hoodBrightness = Mathf.Clamp01(brightness);
            hoodSwatch = -1;
            hoodChanged = true;
            RecolorHood();
        }

        private void Adjust(int direction) => Adjust(direction, false);
        private void AdjustRepeat(int direction) => Adjust(direction, true);

        private void Adjust(int direction, bool repeat)
        {
            if (!OnOptionsPage || MenuStack.Top != (IModalMenu)this) return;
            options.Adjust(direction, repeat);
        }

        // The sprite isn't readable on the CPU, so it's copied once through a render texture.
        private bool BuildHoodPreview()
        {
            if (hoodView == null) return false;
            if (hoodSource == null)
            {
                var texture = hoodPreview.texture;
                var rect = hoodPreview.textureRect;
                var copy = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var previous = RenderTexture.active;
                Graphics.Blit(texture, copy);
                RenderTexture.active = copy;
                hoodSource = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                hoodSource.ReadPixels(new Rect(rect.x, rect.y, rect.width, rect.height), 0, 0);
                hoodSource.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(copy);
                hoodShown = new Texture2D(hoodSource.width, hoodSource.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                hoodPixels = new Color32[hoodSource.width * hoodSource.height];
            }
            hoodView.style.width = hoodSource.width * hoodPreviewScale;
            hoodView.style.height = hoodSource.height * hoodPreviewScale;
            hoodView.style.backgroundImage = new StyleBackground(hoodShown);
            return true;
        }

        private void RecolorHood()
        {
            if (hoodSource == null) return;
            var source = hoodSource.GetPixels32();
            var chosen = HoodColor;
            for (int i = 0; i < source.Length; i++) hoodPixels[i] = hoodChanged ? HoodieColor.Recolor(source[i], chosen) : source[i];
            hoodShown.SetPixels32(hoodPixels);
            hoodShown.Apply();
            hoodView.MarkDirtyRepaint();
        }

        // A stepped ink fade up from the bottom edge, so the plain menu reads over the painting: eight hard bands, no blur.
        private void BuildShade(VisualElement shade)
        {
            if (shade == null) return;
            const int bands = 8;
            shadeTexture = new Texture2D(1, bands, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < bands; y++)
                shadeTexture.SetPixel(0, y, new Color(23f / 255f, 28f / 255f, 27f / 255f, Mathf.Lerp(.9f, 0f, y / (float)(bands - 1))));
            shadeTexture.Apply();
            shade.style.backgroundImage = new StyleBackground(shadeTexture);
        }

        // One painting animates for backdropHold seconds, then the next fades in over it.
        private void AnimateBackdrop(float seconds)
        {
            if (backdrop.Length == 0 || backdropFront == null) return;
            backdropTime += seconds;
            backdropFront.Step(seconds);
            if (backdrop.Length < 2 || backdropTime < backdropHold) return;
            if (!backdropFading)
            {
                backdropFading = true;
                backdropBack.Show(backdrop[(backdropIndex + 1) % backdrop.Length], 0f);
            }
            backdropBack.Step(seconds);
            float fade = Mathf.Clamp01((backdropTime - backdropHold) / backdropFade);
            backdropFront.SetOpacity(backdropOpacity * (1f - fade));
            backdropBack.SetOpacity(backdropOpacity * fade);
            if (fade < 1f) return;
            (backdropFront, backdropBack) = (backdropBack, backdropFront);
            backdropBack.Hide();
            backdropIndex = (backdropIndex + 1) % backdrop.Length;
            backdropTime = 0f;
            backdropFading = false;
        }

        private sealed class BannerLayer
        {
            private readonly VisualElement element;
            private readonly PanelSettings panelSettings;
            private Banner banner;
            private int frame;
            private float time;
            private Vector2Int sizedFor;

            public BannerLayer(VisualElement element, PanelSettings panelSettings) { this.element = element; this.panelSettings = panelSettings; Hide(); }

            public void Show(Banner shown, float opacity)
            {
                if (element == null || shown == null || shown.frames.Length == 0 || shown.frames[0] == null) return;
                banner = shown; frame = 0; time = 0f;
                Resize();
                element.style.display = DisplayStyle.Flex;
                SetOpacity(opacity);
                Apply();
            }

            public void Step(float seconds)
            {
                if (banner == null) return;
                // A window or fullscreen change resizes the painting on screen now, not at the next one.
                if (sizedFor.x != Screen.width || sizedFor.y != Screen.height) Resize();
                time += seconds;
                float hold = Hold();
                while (time >= hold) { time -= hold; frame = (frame + 1) % banner.frames.Length; hold = Hold(); }
                Apply();
            }

            public void SetOpacity(float opacity) { if (element != null) element.style.opacity = opacity; }
            public void Hide() { banner = null; if (element != null) element.style.display = DisplayStyle.None; }

            // The smallest whole number of screen pixels per art pixel that fills the width (5 at 1080p); a wider painting
            // crops a little at the sides rather than blur.
            private void Resize()
            {
                var first = banner.frames[0];
                int scale = Mathf.Max(1, Mathf.CeilToInt(Screen.width / (float)first.width));
                float perPoint = PixelsPerPoint();
                element.style.width = first.width * scale / perPoint;
                element.style.height = first.height * scale / perPoint;
                sizedFor = new Vector2Int(Screen.width, Screen.height);
            }

            // The panel's own scale when it has one; otherwise worked out from its settings the way Unity does, including
            // the screen match mode (the title's panel uses Expand, the smaller of the two ratios; on 21:9 the
            // width-matched figure left bars at the sides, found 15 Nov).
            private float PixelsPerPoint()
            {
                if (element.panel != null && element.panel.scaledPixelsPerPoint > 0f) return element.panel.scaledPixelsPerPoint;
                if (panelSettings == null) return 2f;
                if (panelSettings.scaleMode == PanelScaleMode.ConstantPixelSize) return Mathf.Max(.01f, panelSettings.scale);
                if (panelSettings.scaleMode != PanelScaleMode.ScaleWithScreenSize) return 2f;
                var reference = panelSettings.referenceResolution;
                float wide = Screen.width / (float)Mathf.Max(1, reference.x), tall = Screen.height / (float)Mathf.Max(1, reference.y);
                float ratio = panelSettings.screenMatchMode == PanelScreenMatchMode.Expand ? Mathf.Min(wide, tall) :
                    panelSettings.screenMatchMode == PanelScreenMatchMode.Shrink ? Mathf.Max(wide, tall) :
                    Mathf.Pow(2f, Mathf.Lerp(Mathf.Log(wide, 2f), Mathf.Log(tall, 2f), panelSettings.match));
                return ratio * panelSettings.scale;
            }

            private float Hold() => banner.seconds.Length > 0 ? Mathf.Max(.02f, banner.seconds[Mathf.Min(frame, banner.seconds.Length - 1)]) : .17f;
            private void Apply() { if (banner.frames[frame] != null) element.style.backgroundImage = new StyleBackground(banner.frames[frame]); }
        }

        // Pages are paragraphs; '#' lines are notes for writers, and line breaks inside a page are only wrapping.
        private static string[] Pages(TextAsset source)
        {
            if (source == null) return new string[0];
            var kept = Regex.Replace(source.text.Replace("\r\n", "\n"), @"^#.*\n?", "", RegexOptions.Multiline);
            var blocks = Regex.Split(kept.Trim(), @"\n\s*\n");
            var result = new System.Collections.Generic.List<string>();
            foreach (var block in blocks)
            {
                string text = Regex.Replace(block.Trim(), @"\s*\n\s*", " ");
                if (text.Length > 0) result.Add(text);
            }
            return result.ToArray();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Escape/Start skips the prologue; elsewhere it steps back like B.
        private void OnToggle()
        {
            if (page == Page.Prologue) { if (leavingAt < 0f) leavingAt = Time.unscaledTime; }
            else Back();
        }

        public void Back()
        {
            if (page == Page.Confirm) ShowMain(NewGameRow);
            else if (page == Page.Settings) ShowMain(SettingsRow);
            // The hood page has no way back: the prologue is over and the run is about to begin.
        }
        public void Navigate(int direction)
        {
            if (MenuStack.Top != (IModalMenu)this || Time.frameCount == revealedFrame) return;
            if (OnOptionsPage) options.Navigate(direction);
            else if (page != Page.Press && page != Page.Prologue) list.Navigate(direction);
        }
        public void Submit()
        {
            if (MenuStack.Top != (IModalMenu)this || Session == null || Session.IsLoading || Time.frameCount == revealedFrame) return;
            if (page == Page.Prologue) AdvancePrologue();
            else if (OnOptionsPage) options.Submit();
            else if (page != Page.Press) list.Submit();
        }
    }
}
