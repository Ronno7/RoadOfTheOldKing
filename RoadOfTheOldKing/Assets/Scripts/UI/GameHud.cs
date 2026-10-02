using System.Text;
using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.Weapons;
using TheLostShrine.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    // Contextual HUD (placeholder art, production structure). Lives on the Player prefab and only
    // presents: it reads gameplay components and events and never edits gameplay or save state.
    //  Vitals appear on damage, spending, recovery or combat and hide after a hold once full and safe.
    //  One interaction prompt beside the object the interaction selector actually chose.
    //  Weapon-away chip, charge bar near the player, reward receipts, one Tutorial hint, Tab status.
    // Modal menus (bonfire, defeat) remain in TutorialCombatHUD for now.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
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
        private VisualElement root, vignette, vitals, healthFill, staminaFill, staminaBar, weaponAway, prompt, charge, chargeFill, receipts, hint, status;
        private Label staminaWarning, weaponAwayText, promptText, hintText, statusText;
        private PlayerHealth player;
        private Damageable playerDamageable;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private PlayerBonfireInteraction interaction;
        private PauseMenuController pauseMenu;
        private CheckpointSession session;
        private float vitalsUntil, hurtAmount;
        private Texture2D vignetteTexture;
        private int receiptsVersion = -1, hintVersion = -1;
        private bool statusOpen;
        private readonly StringBuilder text = new StringBuilder(256);

        private void Awake()
        {
            player = GetComponent<PlayerHealth>();
            // Read directly: PlayerHealth.Health is only set in its own Awake, which may run after this OnEnable.
            playerDamageable = GetComponent<Damageable>();
            stamina = GetComponent<PlayerStamina>();
            combat = GetComponent<PlayerCombatController>();
            interaction = GetComponent<PlayerBonfireInteraction>();
            pauseMenu = GetComponent<PauseMenuController>();
        }

        private void OnEnable()
        {
            Active = this;
            if (!Build()) { enabled = false; return; }
            if (playerDamageable != null) playerDamageable.HitReceived += OnPlayerHit;
            Subscribe();
            Reveal();
        }

        private void OnDisable()
        {
            if (Active == this) Active = null;
            if (playerDamageable != null) playerDamageable.HitReceived -= OnPlayerHit;
            Unsubscribe();
            if (root != null) root.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (document != null) Destroy(document.gameObject);
            if (vignetteTexture != null) Destroy(vignetteTexture);
        }

        private bool Build()
        {
            if (document != null) { root.style.display = DisplayStyle.Flex; return true; }
            if (layout == null || panelSettings == null) { Debug.LogError("GameHud needs its layout and panel settings.", this); return false; }
            var child = new GameObject("HUD UI"); child.SetActive(false); child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
            child.SetActive(true);
            root = document.rootVisualElement.Q("hud-root");
            vitals = root.Q("vitals"); healthFill = root.Q("health-fill"); staminaFill = root.Q("stamina-fill");
            staminaBar = root.Q("stamina-bar"); staminaWarning = root.Q<Label>("stamina-warning");
            weaponAway = root.Q("weapon-away"); weaponAwayText = root.Q<Label>("weapon-away-text");
            prompt = root.Q("prompt"); promptText = root.Q<Label>("prompt-text");
            charge = root.Q("charge"); chargeFill = root.Q("charge-fill");
            receipts = root.Q("receipts");
            hint = root.Q("hint"); hintText = root.Q<Label>("hint-text");
            status = root.Q("status"); statusText = root.Q<Label>("status-text");
            foreach (var label in new[] { staminaWarning, weaponAwayText, promptText, hintText, statusText }) label.enableRichText = true;
            SetVisible(prompt, false); SetVisible(charge, false); SetVisible(hint, false); SetVisible(status, false);
            SetVisible(weaponAway, false); SetVisible(staminaWarning, false);
            BuildVignette();
            return true;
        }

        private void Subscribe()
        {
            if (session != null || CheckpointSession.Instance == null) return;
            session = CheckpointSession.Instance;
            session.ShardCollected += OnShard;
            session.HeartFragmentCollected += OnFragment;
        }

        private void Unsubscribe()
        {
            if (session == null) return;
            session.ShardCollected -= OnShard;
            session.HeartFragmentCollected -= OnFragment;
            session = null;
        }

        private void OnPlayerHit(CombatHit hit) { EncounterState.ReportThreat(); Reveal(); if (hit.Damage > 0) hurtAmount = 1f; }
        private void OnShard(int total) => HudNotifications.Post("Sun Shard +1 · " + total + (total == 1 ? " shard" : " shards"));
        private void OnFragment(int total, bool completedHeart) => HudNotifications.Post(completedHeart
            ? "Heart complete · Maximum health " + player.Health.MaxHealth
            : "Heart fragment · " + total % HeartFragmentProgression.FragmentsPerHeart + " / " + HeartFragmentProgression.FragmentsPerHeart, 4.5f);

        private void Reveal() => vitalsUntil = Time.time + vitalsHold;

        private void Update()
        {
            if (document == null) return;
            Subscribe();
            bool menuOpen = (interaction != null && interaction.IsOpen) || (pauseMenu != null && pauseMenu.BlocksGameplay);
            bool alive = player.IsAlive;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && !menuOpen && alive && Time.timeScale > 0f) statusOpen = !statusOpen;
            if (menuOpen || !alive) statusOpen = false;

            UpdateVitals();
            UpdateVignette(alive);
            UpdateWeaponAway(alive && !menuOpen);
            UpdatePrompt(alive && !menuOpen);
            UpdateCharge(alive && !menuOpen);
            UpdateReceipts();
            UpdateHint(alive && !menuOpen);
            SetVisible(status, statusOpen);
            if (statusOpen) UpdateStatus();
        }

        private void UpdateVitals()
        {
            var health = player.Health;
            float healthRatio = health != null && health.MaxHealth > 0 ? (float)health.Health / health.MaxHealth : 0f;
            float staminaRatio = stamina != null ? stamina.Normalized : 1f;
            // Very low but nonzero health never renders as an empty (dead-looking) bar.
            healthFill.style.width = Length.Percent(health != null && health.Health > 0 ? Mathf.Max(4f, healthRatio * 100f) : 0f);
            staminaFill.style.width = Length.Percent(staminaRatio * 100f);
            staminaFill.EnableInClassList("low", staminaRatio < 0.25f);
            bool rejected = stamina != null && stamina.WasSpendRejected;
            staminaBar.EnableInClassList("rejected", rejected);
            SetVisible(staminaWarning, rejected);
            if (healthRatio < 0.999f || staminaRatio < 0.999f || EncounterState.InCombat || !player.IsAlive || statusOpen) Reveal();
            SetVisible(vitals, Time.time < vitalsUntil);
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
            string state = weapon.State == AxeState.Returning ? "Axe returning" : "Axe away";
            weaponAwayText.text = weapon.State == AxeState.Returning ? state
                : combat.CanRecall ? state + " · " + ControlLabels.Format("{recall} Recall") : state + " · walk over it";
        }

        private void UpdatePrompt(bool allowed)
        {
            Transform target = null; string verb = null;
            // Mirror the interaction selector exactly: pickups before fires, one at a time.
            if (allowed && interaction != null && !string.IsNullOrEmpty(interaction.Prompt))
            {
                if (interaction.NearbyPickup != null) { target = interaction.NearbyPickup.transform; verb = interaction.NearbyPickup.Prompt; }
                else if (interaction.Nearby != null) { target = interaction.Nearby.transform; verb = "Rest at " + interaction.Nearby.DisplayName; }
            }
            if (target == null || !Anchor(prompt, target.position + Vector3.up * promptHeight, true)) { SetVisible(prompt, false); return; }
            promptText.text = ControlLabels.Format("{interact} ") + verb;
            SetVisible(prompt, true);
        }

        private void UpdateCharge(bool allowed)
        {
            var weapon = combat != null ? combat.Weapon : null;
            bool charging = allowed && weapon != null && weapon.State == AxeState.Charging;
            if (!charging || !Anchor(charge, transform.position + Vector3.up * chargeOffset, false)) { SetVisible(charge, false); return; }
            chargeFill.style.width = Length.Percent(weapon.Charge01 * 100f);
            chargeFill.EnableInClassList("ready", weapon.Charge01 >= 1f);
            SetVisible(charge, true);
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
            bool show = allowed && !string.IsNullOrEmpty(HudNotifications.Hint);
            SetVisible(hint, show);
            if (!show || hintVersion == HudNotifications.HintVersion) return;
            hintVersion = HudNotifications.HintVersion;
            hint.EnableInClassList("completed", HudNotifications.HintCompleted);
            // Plain-text marker: the default font may lack a check glyph; the gold styling carries it.
            hintText.text = HudNotifications.HintCompleted ? "Done: " + HudNotifications.Hint : HudNotifications.Hint;
        }

        private void UpdateStatus()
        {
            text.Length = 0;
            var health = player.Health;
            if (health != null) text.Append("Health  ").Append(health.Health).Append(" / ").Append(health.MaxHealth).Append('\n');
            if (stamina != null) text.Append("Stamina  ").Append(Mathf.FloorToInt(stamina.Current)).Append(" / ").Append(Mathf.RoundToInt(stamina.Maximum)).Append('\n');
            var progress = CheckpointSession.Instance != null ? CheckpointSession.Instance.Progress : null;
            if (progress != null)
            {
                text.Append("Sun Shards  ").Append(progress.sunShards).Append('\n');
                int fragments = HeartFragmentProgression.Count(progress);
                text.Append("Heart fragments  ").Append(fragments % HeartFragmentProgression.FragmentsPerHeart).Append(" / ").Append(HeartFragmentProgression.FragmentsPerHeart);
                int bonus = HeartFragmentProgression.BonusHealth(progress);
                if (bonus > 0) text.Append("  (+").Append(bonus).Append(" max)");
                text.Append('\n');
            }
            var weapon = combat != null ? combat.Weapon : null;
            text.Append("Axe  ").Append(weapon == null ? "not yet found" : weapon.IsAway ? "away" : "in hand").Append('\n');
            text.Append("Recall  ").Append(combat != null && combat.CanRecall ? "awakened" : "dormant").Append('\n');
            if (CheckpointSession.Instance != null)
            {
                bool any = false;
                foreach (var upgrade in CheckpointSession.Instance.Upgrades.Selected)
                {
                    text.Append(any ? ", " : "Upgrades  ").Append(upgrade.displayName);
                    any = true;
                }
                if (!any) text.Append("Upgrades  none yet");
            }
            statusText.text = text.ToString();
        }

        // Places a world-anchored element; returns false when the point is off screen.
        private bool Anchor(VisualElement element, Vector3 world, bool above)
        {
            var camera = Camera.main;
            if (camera == null || element.panel == null) return false;
            Vector3 viewport = camera.WorldToViewportPoint(world);
            if (viewport.z < 0f || viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) return false;
            Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(element.panel, world, camera);
            float width = float.IsNaN(element.resolvedStyle.width) ? 0f : element.resolvedStyle.width;
            float height = float.IsNaN(element.resolvedStyle.height) ? 0f : element.resolvedStyle.height;
            // Keep prompts inside the viewport.
            var bounds = root.layout;
            float left = Mathf.Clamp(point.x - width * 0.5f, 4f, Mathf.Max(4f, bounds.width - width - 4f));
            float top = Mathf.Clamp(above ? point.y - height : point.y, 4f, Mathf.Max(4f, bounds.height - height - 4f));
            element.style.left = left; element.style.top = top;
            return true;
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (element.style.display != display) element.style.display = display;
        }
    }
}
