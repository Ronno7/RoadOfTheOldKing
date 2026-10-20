using RoadOfTheOldKing.Enemies;
using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Contextual HUD (placeholder art, production structure). Lives on the Player prefab and only
    // presents: it reads gameplay components and events and never edits gameplay or save state.
    //  Corner vitals (pixel art: health, flasks; stamina too with HudSettings.AlwaysShowVitals) appear on
    //  damage, healing or combat and hide after a hold once full and safe.
    //  Stamina otherwise is a pixel arc over the hero, shown only in combat.
    //  One interaction prompt above the object the interaction selector actually chose.
    //  Weapon-away chip, charge bar near the player, reward receipts, one Tutorial hint. Inventory lives in CollectionMenu.
    // The charge bar and stamina arc are world-space documents (WorldUIDocument); the rest is a screen panel. The prompt
    // and the tutorial cue are screen chips anchored to world points, so they keep the kit's UI pixel size at every zoom
    // (user, 17 Oct); LateUpdate places them after the camera has moved (CameraFollow2D), hence the late execution order.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth)), DefaultExecutionOrder(1000)]
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        [Header("World space")]
        [SerializeField] private PanelSettings worldPanel;
        [SerializeField] private VisualTreeAsset chargeLayout;
        [SerializeField] private VisualTreeAsset staminaArcLayout;
        [Tooltip("Height above the player's pivot of the stamina arc's lowest pixel (world units). 44 px clears " +
            "the carried halberd by 1 px in every hold, walk bob included, for the 18 px / 85° arc (the far-side holds set the limit).")]
        [SerializeField] private float arcBottomHeight = 44f / 16f;
        [Tooltip("Seconds for the stamina arc to fade in, then out.")]
        [SerializeField] private Vector2 arcFade = new Vector2(.15f, .4f);
        [Header("Corner vitals")]
        [Tooltip("Bar length in art pixels per point of maximum health or stamina, so bars grow with their maximum.")]
        [SerializeField, Min(.05f)] private float barPixelsPerPoint = .5f;
        [Tooltip("Seconds the vitals stay after everything is full and no threat remains.")]
        [SerializeField, Min(0f)] private float vitalsHold = 3f;
        [Tooltip("World offset above an interactable where its prompt appears.")]
        [SerializeField] private float promptHeight = 1.25f;
        [Tooltip("World offset below the player's pivot for the charge bar.")]
        [SerializeField] private float chargeOffset = -0.35f;
        [Header("Hurt vignette")]
        [Tooltip("Screen-edge flash strength when the player takes damage.")]
        [SerializeField, Range(0f, 1f)] private float hurtFlash = .55f;
        [SerializeField, Min(.05f)] private float hurtFade = .45f;
        [Tooltip("Health share below which the edges pulse like a heartbeat.")]
        [SerializeField, Range(0f, 1f)] private float lowHealth = .3f;
        [SerializeField, Range(0f, 1f)] private float lowHealthPulse = .3f;
        [SerializeField, Min(.2f)] private float heartbeatPeriod = 1.1f;

        public static GameHud Active { get; private set; }

        private UIDocument document;
        // Corner art is authored in art pixels and drawn at 2 reference px each: the kit's UI pixel (Kit.uss).
        private const float HudPixel = 2f;
        private VisualElement root, vignette, vitals, healthBar, healthFill, staminaRow, staminaBar, staminaFill, staminaDebt, flaskRow;
        private VisualElement weaponAway, chargeFill, receipts, hint, banner;
        private Label bannerText;
        // Milestone banner timing in seconds (unscaled: lighting a fire does not pause the world).
        private const float BannerIn = .3f, BannerHold = 1.6f, BannerOut = .7f;
        private readonly List<VisualElement> flaskIcons = new List<VisualElement>();
        private WorldUIDocument charge, staminaArc;
        private VisualElement promptChip, cueChip, speechChip;
        private Label speechText;
        private Vector3? promptAnchor, cueAnchor;
        private Label tutorialText;
        private bool promptVisible;
        private PixelArc arc;
        private PlayerFlask flask;
        private Label weaponAwayText, promptText, hintText;
        private PlayerHealth player;
        private Damageable playerDamageable;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private PlayerBonfireInteraction interaction;
        private PauseMenuController pauseMenu;
        private GameSession session;
        private float vitalsUntil, hurtAmount;
        private Texture2D vignetteTexture;
        private int receiptsVersion = -1;
        private float arcAlpha;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            // Read directly: PlayerHealth.Health is only set in its own Awake, which may run after this OnEnable.
            playerDamageable = GetComponent<Damageable>();
            stamina = GetComponent<PlayerStamina>();
            flask = GetComponent<PlayerFlask>();
            combat = GetComponent<PlayerCombatController>();
            interaction = GetComponent<PlayerBonfireInteraction>();
            pauseMenu = GetComponent<PauseMenuController>();
        }

        private void OnEnable()
        {
            Active = this;
            if (!Build()) { enabled = false; return; }
            if (playerDamageable != null) playerDamageable.HitReceived += OnPlayerHit;
            if (flask != null) flask.DrinkRefused += OnFlaskEmpty;
            Subscribe();
            Reveal();
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            if (playerDamageable != null) playerDamageable.HitReceived -= OnPlayerHit;
            if (flask != null) flask.DrinkRefused -= OnFlaskEmpty;
            Unsubscribe();
            if (root != null) root.style.display = DisplayStyle.None;
            if (charge != null) charge.Show(false);
            promptAnchor = cueAnchor = null;
            if (staminaArc != null) staminaArc.Show(false);
            arcAlpha = 0f;
        }

        private void OnDestroy()
        {
            if (document != null) Destroy(document.gameObject);
            if (vignetteTexture != null) Destroy(vignetteTexture);
        }

        private bool Build()
        {
            if (document != null) { root.style.display = DisplayStyle.Flex; return true; }
            if (layout == null || panelSettings == null || worldPanel == null || chargeLayout == null || staminaArcLayout == null)
            { Debug.LogError("GameHud needs its layouts and panel settings.", this); return false; }
            var child = new GameObject("HUD UI"); child.SetActive(false); child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
            child.SetActive(true);
            root = document.rootVisualElement.Q("hud-root");
            vitals = root.Q("vitals"); healthBar = root.Q("health-bar"); healthFill = root.Q("health-fill");
            staminaRow = root.Q("stamina-row"); staminaBar = root.Q("stamina-bar"); staminaFill = root.Q("stamina-fill"); staminaDebt = root.Q("stamina-debt");
            flaskRow = root.Q("flask-row");
            weaponAway = root.Q("weapon-away"); weaponAwayText = root.Q<Label>("weapon-away-text");
            charge = WorldUIDocument.Create("Charge bar", transform, worldPanel, chargeLayout, 30005, Pivot.TopCenter);
            chargeFill = charge.Q<VisualElement>("charge-fill");
            staminaArc = WorldUIDocument.Create("Stamina arc", transform, worldPanel, staminaArcLayout, 30008, Pivot.BottomCenter);
            arc = staminaArc.Q<PixelArc>("stamina-arc");
            receipts = root.Q("receipts");
            hint = root.Q("hint"); hintText = root.Q<Label>("hint-text");
            foreach (var label in new[] { weaponAwayText, hintText }) label.enableRichText = true;
            banner = new VisualElement { name = "banner", pickingMode = PickingMode.Ignore };
            bannerText = new Label { name = "banner-text", pickingMode = PickingMode.Ignore };
            banner.Add(bannerText); root.Add(banner);
            promptChip = AnchoredChip("prompt-chip", out promptText);
            cueChip = AnchoredChip("cue-chip", out tutorialText);
            speechChip = AnchoredChip("speech-chip", out speechText);
            charge.Show(false); staminaArc.Show(false); SetVisible(hint, false);
            SetVisible(weaponAway, false); SetVisible(banner, false);
            BuildVignette();
            return true;
        }

        // A screen chip for a world-anchored label (prompt, tutorial cue): kit art at UI pixel size; LateUpdate places it.
        private VisualElement AnchoredChip(string chipName, out Label label)
        {
            var chip = new VisualElement { name = chipName, pickingMode = PickingMode.Ignore };
            chip.AddToClassList("kit-chip"); chip.AddToClassList("anchored-chip");
            label = new Label { pickingMode = PickingMode.Ignore, enableRichText = true };
            chip.Add(label); root.Add(chip); SetVisible(chip, false);
            return chip;
        }

        // After the camera moved and the body's interpolation was applied: the hero's meters follow the drawn
        // sprite, and each chip's bottom centre (USS translate) goes on its world point.
        private void LateUpdate()
        {
            if (document == null) return;
            if (arcAlpha > 0f) staminaArc.Place(transform.position + Vector3.up * arcBottomHeight);
            charge.Place(transform.position + Vector3.up * chargeOffset);
            var view = Camera.main;
            if (root?.panel == null || view == null) return;
            PlaceAnchored(promptChip, promptAnchor, view);
            PlaceAnchored(cueChip, cueAnchor, view);
            if (HudNotifications.Speaker != null)
                PlaceAnchored(speechChip, HudNotifications.Speaker.position + Vector3.up * HudNotifications.SpeechHeight, view);
        }

        private void PlaceAnchored(VisualElement chip, Vector3? anchor, Camera view)
        {
            if (chip == null || !anchor.HasValue) return;
            Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(root.panel, anchor.Value, view);
            // Whole screen pixels, not the 2 px UI grid: the camera glides, so the world moves a screen pixel at a
            // time and a chip stepping 2 reference px (4 screen px at 1080p) visibly vibrates against it.
            float screenPerPanel = Screen.height / Mathf.Max(1f, root.panel.visualTree.layout.height);
            chip.style.left = Mathf.Round(point.x * screenPerPanel) / screenPerPanel;
            chip.style.top = Mathf.Round(point.y * screenPerPanel) / screenPerPanel;
        }

        private void Subscribe()
        {
            if (session != null || GameSession.Instance == null) return;
            session = GameSession.Instance;
            session.Rewards.ShardCollected += OnShard;
            session.Rewards.CoinsCollected += OnCoins;
            session.Rewards.ToolCollected += OnTool;
            session.Rewards.HeartFragmentCollected += OnFragment;
        }

        private void Unsubscribe()
        {
            if (session == null) return;
            session.Rewards.ShardCollected -= OnShard;
            session.Rewards.CoinsCollected -= OnCoins;
            session.Rewards.ToolCollected -= OnTool;
            session.Rewards.HeartFragmentCollected -= OnFragment;
            session = null;
        }

        private void OnPlayerHit(CombatHit hit) { EncounterState.ReportThreat(); Reveal(); if (hit.Damage > 0) hurtAmount = 1f; }
        private void OnCoins(int amount, int total) => HudNotifications.Post("Bronze Coins +" + amount + " · " + total);
        private void OnTool(WorldTool tool) => HudNotifications.Post(WorldToolNames.Display(tool) + " acquired", 4.5f);
        private void OnShard(int total) => HudNotifications.Post("Sun Shard +1 · " + total);
        private void OnFragment(int total, bool completedHeart) => HudNotifications.Post(completedHeart
            ? "Max health " + player.Health.MaxHealth
            : "Heart fragment " + total % HeartFragmentProgression.FragmentsPerHeart + "/" + HeartFragmentProgression.FragmentsPerHeart, 4.5f);

        private void Reveal() => vitalsUntil = Time.time + vitalsHold;
        private void OnFlaskEmpty() { Reveal(); HudNotifications.Post("No flasks. Rest to refill."); }

        private void Update()
        {
            if (document == null) return;
            Subscribe();
            bool menuOpen = MenuStack.IsAnyOpen || (interaction != null && interaction.IsOpen) || (pauseMenu != null && pauseMenu.BlocksGameplay);
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            menuOpen |= DevToolsPanel.CapturesInput;
#endif
            bool alive = player.IsAlive;

            UpdateVitals();
            UpdateStaminaArc(alive && !menuOpen);
            UpdateVignette(alive);
            bool scripted = GetComponent<PlayerControlLocks>()?.IsLocked == true && !menuOpen;
            UpdateWeaponAway(alive && !menuOpen && !scripted && !HudNotifications.HintCompleted && !HudNotifications.HintWorldPosition.HasValue);
            // The banner owns the moment: the next prompt (Rest after lighting) waits until it has faded.
            UpdatePrompt(alive && !menuOpen && !scripted && !BannerShowing());
            UpdateCharge(alive && !menuOpen);
            UpdateReceipts();
            SetVisible(receipts, alive && !menuOpen && !scripted && !EncounterState.InCombat && !HudNotifications.HintCompleted);
            UpdateHint(alive && !menuOpen && !scripted);
            UpdateSpeech(alive && !menuOpen && !scripted);
            UpdateBanner(alive);
        }

        private static bool BannerShowing()
        {
            float t = Time.unscaledTime - HudNotifications.BannerStartedAt;
            return !string.IsNullOrEmpty(HudNotifications.Banner) && t >= 0f && t <= BannerIn + BannerHold + BannerOut;
        }

        // Milestone banner: fade in, hold, fade out. A newer banner replaces the current one.
        private void UpdateBanner(bool alive)
        {
            float t = Time.unscaledTime - HudNotifications.BannerStartedAt;
            float alpha = !alive || !BannerShowing() ? 0f
                : t < BannerIn ? t / BannerIn
                : t < BannerIn + BannerHold ? 1f
                : 1f - (t - BannerIn - BannerHold) / BannerOut;
            SetVisible(banner, alpha > 0f);
            if (alpha <= 0f) return;
            bannerText.text = HudNotifications.Banner;
            banner.style.opacity = alpha;
        }

        // Corner cluster: health, then stamina (only with the always-visible setting; otherwise stamina is
        // the arc over the hero in combat), then one flask icon per charge.
        private void UpdateVitals()
        {
            bool always = HudSettings.AlwaysShowVitals;
            var health = player.Health;
            int maxHealth = health != null ? Mathf.Max(1, health.MaxHealth) : 1;
            float healthRatio = health != null ? (float)health.Health / maxHealth : 0f;
            // Very low but nonzero health never renders as an empty (dead-looking) bar.
            SetBar(healthBar, healthFill, maxHealth, healthRatio, health != null && health.Health > 0);
            SetVisible(staminaRow, always && stamina != null);
            if (always && stamina != null)
            {
                int track = SetBar(staminaBar, staminaFill, stamina.Maximum, stamina.Normalized, false);
                staminaDebt.style.width = Mathf.Round(Debt() * track) * HudPixel;
                staminaBar.EnableInClassList("rejected", stamina.WasSpendRejected);
                staminaBar.EnableInClassList("exhausted", stamina.IsExhausted);
            }
            UpdateFlasks();
            if (healthRatio < 0.999f || EncounterState.InCombat || !player.IsAlive) Reveal();
            SetVisible(vitals, always || Time.time < vitalsUntil);
        }

        // Sizes a framed bar to its maximum and fills it in whole art pixels; returns the track length.
        private int SetBar(VisualElement bar, VisualElement fill, float maximum, float ratio, bool keepSliver)
        {
            int track = Mathf.Max(4, Mathf.RoundToInt(maximum * barPixelsPerPoint));
            bar.style.width = (track + 2) * HudPixel;
            int filled = Mathf.RoundToInt(Mathf.Clamp01(ratio) * track);
            if (keepSliver && ratio > 0f) filled = Mathf.Max(1, filled);
            fill.style.width = filled * HudPixel;
            return track;
        }

        private float Debt() => stamina != null && stamina.IsExhausted ? -stamina.Current / Mathf.Max(1f, stamina.DeficitLimit) : 0f;

        private void UpdateFlasks()
        {
            int max = flask != null ? flask.MaxCharges : 0;
            SetVisible(flaskRow, max > 0);
            if (max == 0) return;
            if (flaskIcons.Count != max)
            {
                flaskRow.Clear(); flaskIcons.Clear();
                for (int i = 0; i < max; i++)
                {
                    var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("hud-icon"); icon.AddToClassList("flask-icon");
                    flaskRow.Add(icon); flaskIcons.Add(icon);
                }
            }
            for (int i = 0; i < max; i++) flaskIcons[i].EnableInClassList("empty", i >= flask.Charges);
            if (flask.IsDrinking) Reveal();
        }

        // Spatial stamina: an arc over the hero, only while in combat; red while in deficit, and its
        // outline flashes when an action is refused. Fades in and out (real time, so hit-stop and pause
        // don't freeze it) and keeps tracking the hero while it fades.
        private void UpdateStaminaArc(bool allowed)
        {
            if (stamina == null || arc == null) return;
            bool wanted = allowed && EncounterState.InCombat;
            float fade = wanted ? arcFade.x : arcFade.y;
            arcAlpha = Mathf.MoveTowards(arcAlpha, wanted ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(.01f, fade));
            staminaArc.Show(arcAlpha > 0f);
            if (arcAlpha <= 0f) return;
            staminaArc.Root.style.opacity = arcAlpha;
            arc.SetValue(stamina.Normalized, Debt(), stamina.WasSpendRejected);
        }

        // A soft red frame behind the HUD: a flash on damage and a heartbeat pulse at low health.
        // Real time, so it reads through hit-stop; scaled by the flash accessibility setting.
        private void BuildVignette()
        {
            const int width = 64, height = 36;
            vignetteTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                { name = "Hurt vignette", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs((x + .5f) / width * 2f - 1f), dy = Mathf.Abs((y + .5f) / height * 2f - 1f);
                float edge = Mathf.Max(dx, dy) * .7f + Mathf.Sqrt(dx * dx + dy * dy) * .3f;
                float alpha = Mathf.Pow(Mathf.Clamp01((edge - .55f) / .45f), 1.6f);
                pixels[y * width + x] = new Color32(150, 20, 16, (byte)(alpha * 255f));
            }
            vignetteTexture.SetPixels32(pixels);
            vignetteTexture.Apply(false, true);
            vignette = new VisualElement { name = "hurt-vignette", pickingMode = PickingMode.Ignore };
            vignette.style.position = Position.Absolute;
            vignette.style.left = vignette.style.right = vignette.style.top = vignette.style.bottom = 0;
            vignette.style.backgroundImage = new StyleBackground(vignetteTexture);
            vignette.style.opacity = 0f;
            root.Insert(0, vignette);
        }

        private void UpdateVignette(bool alive)
        {
            if (vignette == null) return;
            hurtAmount = Mathf.Max(0f, hurtAmount - Time.unscaledDeltaTime / hurtFade);
            var health = player.Health;
            float ratio = health != null && health.MaxHealth > 0 ? (float)health.Health / health.MaxHealth : 1f;
            float pulse = 0f;
            if (alive && ratio < lowHealth)
            {
                // Two beats per period, like a heartbeat, stronger as health falls.
                float t = Mathf.Repeat(Time.unscaledTime / heartbeatPeriod, 1f);
                float beat = Mathf.Max(Mathf.Exp(-Mathf.Pow((t - .1f) / .06f, 2f)), .7f * Mathf.Exp(-Mathf.Pow((t - .3f) / .06f, 2f)));
                pulse = lowHealthPulse * (.5f + .5f * (1f - ratio / lowHealth)) * (.35f + .65f * beat);
            }
            float opacity = Mathf.Max(hurtAmount * hurtAmount * hurtFlash, pulse) * Mathf.Clamp01(FeedbackSettings.FlashScale);
            vignette.style.opacity = opacity;
        }

        private void UpdateWeaponAway(bool allowed)
        {
            var weapon = combat != null ? combat.Weapon : null;
            bool away = allowed && weapon != null && weapon.IsAway;
            SetVisible(weaponAway, away);
            if (!away) return;
            weaponAwayText.text = weapon.State == AxeState.Returning ? "Returning"
                : combat.CanRecall ? ControlLabels.Format("{recall} Recall") : "Axe away";
        }

        private void UpdatePrompt(bool allowed)
        {
            Transform target = null; string verb = null;
            // Mirror the interaction selector exactly: pickups before fires, one at a time.
            if (allowed && interaction != null && !string.IsNullOrEmpty(interaction.Prompt))
            {
                if (interaction.NearbyPickup != null) { target = interaction.NearbyPickup.transform; verb = interaction.NearbyPickup.Prompt; }
                else if (interaction.Nearby != null)
                {
                    // Souls-style: the first interaction only lights the fire; resting is a separate one.
                    target = interaction.Nearby.transform;
                    verb = interaction.Nearby.IsDiscovered ? "Rest" : "Light Bonfire";
                }
            }
            promptVisible = target != null;
            SetVisible(promptChip, promptVisible);
            if (target == null) { promptAnchor = null; return; }
            var fireView = interaction.NearbyPickup == null ? target.GetComponent<BonfireSpriteView>() : null;
            promptAnchor = fireView != null ? fireView.PromptPosition : target.position + Vector3.up * promptHeight;
            promptText.text = ControlLabels.Format("{interact} ") + verb;
        }

        private void UpdateCharge(bool allowed)
        {
            var weapon = combat != null ? combat.Weapon : null;
            bool charging = allowed && weapon != null && weapon.State == AxeState.Charging;
            if (!charging) { charge.Show(false); return; }
            chargeFill.style.width = Length.Percent(weapon.Charge01 * 100f);
            chargeFill.EnableInClassList("ready", weapon.Charge01 >= 1f);
            charge.Show(true);
        }

        private void UpdateReceipts()
        {
            HudNotifications.Expire();
            if (receiptsVersion == HudNotifications.Version) return;
            receiptsVersion = HudNotifications.Version;
            receipts.Clear();
            foreach (var receipt in HudNotifications.Receipts)
            {
                var label = new Label(receipt.Text) { enableRichText = true, pickingMode = PickingMode.Ignore };
                label.AddToClassList("receipt");
                receipts.Add(label);
            }
        }

        private void UpdateHint(bool allowed)
        {
            bool show = allowed && !promptVisible && !string.IsNullOrEmpty(HudNotifications.Hint);
            var anchor = HudNotifications.HintWorldPosition;
            bool spatial = show && anchor.HasValue;
            // Keep the whole chip on screen; distant destinations use the single screen hint instead.
            if (spatial && Camera.main != null)
            {
                Vector3 viewport = Camera.main.WorldToViewportPoint(anchor.Value);
                spatial = viewport.z > 0 && viewport.x > .2f && viewport.x < .8f && viewport.y > .15f && viewport.y < .86f;
            }
            SetVisible(cueChip, spatial);
            cueAnchor = spatial ? anchor : null;
            SetVisible(hint, show && !spatial);
            if (!show) return;
            if (spatial)
            {
                tutorialText.text = HudNotifications.Hint;
                cueChip.EnableInClassList("completed", HudNotifications.HintCompleted);
            }
            hint.EnableInClassList("completed", HudNotifications.HintCompleted);
            hintText.text = HudNotifications.Hint;

        }

        // Someone in the world speaking a short line (the trader's barks), over their head.
        private void UpdateSpeech(bool allowed)
        {
            bool show = allowed && HudNotifications.SpeechShowing;
            SetVisible(speechChip, show);
            if (show) speechText.text = HudNotifications.Speech;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display) element.style.display = display;
        }
    }
}
