// Ported 18 Nov from the Tools/Verification snippet ThrowAimPlayModeChecks.cs.txt, logic unchanged: the test runner gives each
// test an empty Play Mode scene with no GameSession, the setup these checks always needed. Run it from
// Window > General > Test Runner > PlayMode. The body still reads like the snippet; tidy it when this area is next touched.
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public sealed class ThrowAimTests
{
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator ThrowAndAim()
    {
        yield return CheckRun.Settle();
        CheckRun.Expect(Run());
    }

    // The controller pass (user, 18 Nov: "this game should feel better to play on controller than it does on mouse and
    // keyboard"): walking speed follows the stick's tilt, B taps dodge and holds sprint, controller-only aim assist,
    // rumble, and PlayStation button names. A virtual pad and keyboard; nothing saved but the Vibration setting,
    // which is put back.
    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator ControllerFeel()
    {
        yield return CheckRun.Settle();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var statics = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
        var created = new List<GameObject>();
        var pad = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        var oldRouting = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
        bool oldGamepad = RoadOfTheOldKing.Input.ActiveInput.UsingGamepad, oldPlayStation = RoadOfTheOldKing.Input.ActiveInput.PlayStationPad;
        const string vibrationKey = "RoadOfTheOldKing.Settings.Vibration";
        bool hadVibration = PlayerPrefs.HasKey(vibrationKey);
        float oldVibration = RoadOfTheOldKing.Input.Rumble.Scale;
        var focus = typeof(RoadOfTheOldKing.Input.Rumble).GetField("focused", statics);
        object oldFocus = focus.GetValue(null);
        Func<Vector2> motors = () =>
        {
            object rumble = typeof(UnityEngine.InputSystem.Gamepad).GetField("m_Rumble", flags).GetValue(pad);
            return new Vector2((float)rumble.GetType().GetProperty("lowFrequencyMotorSpeed").GetValue(rumble, null),
                (float)rumble.GetType().GetProperty("highFrequencyMotorSpeed").GetValue(rumble, null));
        };
        try
        {
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var hero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab"));
            created.Add(hero);
            var body = hero.GetComponent<Rigidbody2D>();
            Vector2 origin = new Vector2(2000, 2000);
            body.position = origin; hero.transform.position = origin; Physics2D.SyncTransforms();
            var input = hero.GetComponent<RoadOfTheOldKing.Input.PlayerMovementInput>();
            var movement = hero.GetComponent<RoadOfTheOldKing.Player.PlayerMovement>();
            var combat = hero.GetComponent<RoadOfTheOldKing.Player.PlayerCombatController>();
            var dash = hero.GetComponent<RoadOfTheOldKing.Player.PlayerDash>();
            var map = (UnityEngine.InputSystem.InputActionMap)input.GetType().GetField("actions", flags).GetValue(input);
            map.devices = new UnityEngine.InputSystem.InputDevice[] { pad, keyboard };
            input.GetType().GetField("hasFocus", flags).SetValue(input, true);
            var read = input.GetType().GetMethod("Update", flags);
            var step = movement.GetType().GetMethod("FixedUpdate", flags);
            Action<Vector2, bool> padState = (tilt, east) =>
            {
                var state = new UnityEngine.InputSystem.LowLevel.GamepadState { leftStick = tilt };
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(pad, east ? state.WithButton(UnityEngine.InputSystem.LowLevel.GamepadButton.East) : state);
                UnityEngine.InputSystem.InputSystem.Update();
                read.Invoke(input, null);
            };
            // Each step is a new input update, so a press counts once and a held button stays held.
            Action<int> run = steps => { for (int i = 0; i < steps; i++) { UnityEngine.InputSystem.InputSystem.Update(); read.Invoke(input, null); step.Invoke(movement, null); } };

            // Tilt: the pad's own deadzone and then ours rescale it to 0..1 (t); speed is Lerp(0.2, 1, t²) of walking speed.
            padState(new Vector2(.5f, 0f), false);
            float t = map["Move"].ReadValue<Vector2>().magnitude, expected = Mathf.Lerp(.2f, 1f, t * t);
            NUnit.Framework.Assert.That(t > .2f && t < .8f, "half tilt reads as part tilt (" + t + ")");
            NUnit.Framework.Assert.AreEqual(expected, input.MoveAmount, .01f, "half tilt walks at the curve's share");
            NUnit.Framework.Assert.AreEqual(Vector2.right, input.MoveDirection, "the direction stays a unit vector");
            run(15);
            NUnit.Framework.Assert.AreEqual(movement.MoveSpeed * expected, body.linearVelocity.x, .05f, "the hero walks at that share of walking speed");
            padState(new Vector2(.3f, 0f), false);
            NUnit.Framework.Assert.AreEqual(.2f, input.MoveAmount, .01f, "just past the deadzone is the slowest walk");
            padState(Vector2.right, false);
            run(15);
            NUnit.Framework.Assert.AreEqual(1f, input.MoveAmount, .001f, "full tilt is full speed");
            NUnit.Framework.Assert.AreEqual(movement.MoveSpeed, body.linearVelocity.x, .05f, "full tilt walks at full speed");
            padState(Vector2.zero, false);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.D));
            UnityEngine.InputSystem.InputSystem.Update(); read.Invoke(input, null);
            NUnit.Framework.Assert.AreEqual(1f, input.MoveAmount, .001f, "keys always walk at full speed");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();

            // B, as in Dark Souls (user, 18 Nov): a tap dodges as it's let go; held past the tap time it sprints, never dodging.
            var pressedAt = input.GetType().GetField("padPressedAt", flags);
            padState(Vector2.right, true); step.Invoke(movement, null);
            NUnit.Framework.Assert.IsFalse(dash.IsDashing || input.SprintHeld, "pressing B neither dodges nor sprints yet");
            padState(Vector2.right, false); step.Invoke(movement, null);
            NUnit.Framework.Assert.IsTrue(dash.IsDashing, "a tap dodges as it's let go");
            for (int i = 0; i < 40 && dash.IsDashing; i++) run(1);
            run(5);
            padState(Vector2.right, true);
            pressedAt.SetValue(input, Time.unscaledTime - 1f); // as if held for a second
            run(10);
            NUnit.Framework.Assert.IsTrue(input.SprintHeld && movement.IsSprinting && !dash.IsDashing, "holding B sprints without a dodge");
            NUnit.Framework.Assert.AreEqual(movement.SprintSpeed, body.linearVelocity.magnitude, .1f, "at sprint speed");
            padState(Vector2.right, false); step.Invoke(movement, null);
            NUnit.Framework.Assert.IsFalse(input.SprintHeld || movement.IsSprinting || dash.IsDashing, "letting go after a sprint walks, with no dodge");
            input.GetType().GetField("hasFocus", flags).SetValue(input, false);
            padState(Vector2.right, true);
            input.GetType().GetField("hasFocus", flags).SetValue(input, true);
            UnityEngine.InputSystem.InputSystem.Update(); read.Invoke(input, null);
            NUnit.Framework.Assert.IsFalse(input.SprintHeld, "B already held when play resumes (closing a menu) doesn't sprint");
            padState(Vector2.right, false); step.Invoke(movement, null);
            NUnit.Framework.Assert.IsFalse(dash.IsDashing, "and letting it go doesn't dodge");
            padState(Vector2.zero, false);

            // Aim assist: controller only, live enemies only, a wide cone in reach for melee, a narrow long one for throws.
            var wolf = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Combat/Wolf.prefab"));
            created.Add(wolf);
            var hurtbox = wolf.GetComponentInChildren<RoadOfTheOldKing.Enemies.EnemyHurtbox>().GetComponent<Collider2D>();
            Func<float, float, Vector2> place = (degrees, distance) =>
            {
                Vector2 centre = (Vector2)hurtbox.bounds.center - (Vector2)wolf.transform.position;
                Vector2 at = origin + (Vector2)(Quaternion.Euler(0, 0, degrees) * Vector2.right) * distance - centre;
                wolf.transform.position = at; Physics2D.SyncTransforms();
                return ((Vector2)hurtbox.bounds.center - origin).normalized;
            };
            body.position = origin; hero.transform.position = origin; Physics2D.SyncTransforms();
            combat.GetType().GetField("<AimDirection>k__BackingField", flags).SetValue(combat, Vector2.right);
            var assisted = combat.GetType().GetMethod("Assisted", flags);
            Func<bool, Vector2> commit = throwing => (Vector2)assisted.Invoke(combat, new object[] { throwing });
            RoadOfTheOldKing.Input.ActiveInput.Set(true);
            Vector2 toWolf = place(30f, 2f);
            NUnit.Framework.Assert.Less(Vector2.Angle(commit(false), toWolf), .5f, "a swing bends to a wolf 30° off the aim in reach");
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(true), "a throw doesn't bend 30°");
            toWolf = place(10f, 5f);
            NUnit.Framework.Assert.Less(Vector2.Angle(commit(true), toWolf), .5f, "a throw bends to a wolf 10° off the aim within its range");
            // While the throw is aimed, the guide shows the assisted line the release will take (review, 23 Nov).
            var intent = combat.GetType().GetField("throwIntent", flags);
            object idle = intent.GetValue(combat);
            intent.SetValue(combat, System.Enum.Parse(intent.FieldType, "Throw"));
            var shown = (Vector2)combat.GetType().GetProperty("ShownAim", flags).GetValue(combat);
            intent.SetValue(combat, idle);
            NUnit.Framework.Assert.Less(Vector2.Angle(shown, toWolf), .5f, "an aimed throw shows the assisted direction");
            // A wall between the hero and the wolf: the axe couldn't reach it, so assist leaves the aim alone.
            var wall = new GameObject("Assist wall");
            created.Add(wall);
            wall.transform.position = origin + (Vector2)(Quaternion.Euler(0, 0, 10f) * Vector2.right) * 2.5f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.4f, 2f);
            Physics2D.SyncTransforms();
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(true), "a throw doesn't bend to a wolf behind a wall");
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(false), "a swing ignores a wolf out of reach");
            place(60f, 2f);
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(false), "a swing ignores a wolf 60° off the aim");
            toWolf = place(30f, 2f);
            RoadOfTheOldKing.Input.ActiveInput.Set(false);
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(false), "no assist on mouse and keyboard");
            RoadOfTheOldKing.Input.ActiveInput.Set(true);
            wolf.SetActive(false);
            NUnit.Framework.Assert.AreEqual(Vector2.right, commit(false), "no assist without a live enemy");

            // Button names: Xbox by default, the PlayStation pad's own words when one is in use.
            RoadOfTheOldKing.Input.ActiveInput.SetPlayStation(false);
            NUnit.Framework.Assert.AreEqual("Hold B", RoadOfTheOldKing.UI.ControlLabels.Get("sprint"));
            NUnit.Framework.Assert.AreEqual("B Back", RoadOfTheOldKing.UI.ControlLabels.Pick("Esc Back", "B Back"));
            RoadOfTheOldKing.Input.ActiveInput.SetPlayStation(true);
            NUnit.Framework.Assert.AreEqual("Circle", RoadOfTheOldKing.UI.ControlLabels.Get("dash"));
            NUnit.Framework.Assert.AreEqual("Hold Circle", RoadOfTheOldKing.UI.ControlLabels.Get("sprint"));
            NUnit.Framework.Assert.AreEqual("L1 + RS", RoadOfTheOldKing.UI.ControlLabels.Get("look"));
            NUnit.Framework.Assert.AreEqual("D-pad Choose · Cross Select", RoadOfTheOldKing.UI.ControlLabels.Pick("", "D-pad Choose · A Select"));
            NUnit.Framework.Assert.AreEqual("Cross Next · Options Skip", RoadOfTheOldKing.UI.ControlLabels.Pick("", "A Next · Start Skip"));
            NUnit.Framework.Assert.AreEqual("Create / Circle", RoadOfTheOldKing.UI.ControlLabels.Pick("", "View / B"));
            NUnit.Framework.StringAssert.Contains("[Circle]", RoadOfTheOldKing.UI.ControlLabels.Format("Dodge {dash}"));
            RoadOfTheOldKing.Input.ActiveInput.Set(false);
            NUnit.Framework.Assert.AreEqual("Space", RoadOfTheOldKing.UI.ControlLabels.Get("dash"), "keyboard names are untouched");
            RoadOfTheOldKing.Input.ActiveInput.SetPlayStation(false);
            foreach (var go in created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            created.Clear();

            // Rumble: on the active pad at the setting's strength; a pause silences it; it ends on time; off is off.
            pad.MakeCurrent();
            RoadOfTheOldKing.Input.ActiveInput.Set(true);
            focus.SetValue(null, true);
            RoadOfTheOldKing.Input.Rumble.SetScale(1f);
            RoadOfTheOldKing.Input.Rumble.Pulse(.5f, .7f, .4f);
            NUnit.Framework.Assert.AreEqual(new Vector2(.5f, .7f), motors(), "a pulse drives both motors");
            RoadOfTheOldKing.Input.Rumble.SetScale(.5f);
            RoadOfTheOldKing.Input.Rumble.Pulse(.2f, .2f, .1f);
            NUnit.Framework.Assert.AreEqual(new Vector2(.25f, .35f), motors(), "a weaker pulse keeps the stronger one, at the setting's strength");
            using (RoadOfTheOldKing.UI.SimulationPause.Acquire())
            {
                UnityEngine.InputSystem.InputSystem.Update();
                NUnit.Framework.Assert.AreEqual(Vector2.zero, motors(), "a pause silences it");
            }
            Time.timeScale = 1f;
            UnityEngine.InputSystem.InputSystem.Update();
            NUnit.Framework.Assert.AreEqual(new Vector2(.25f, .35f), motors(), "unpausing resumes it");
            yield return new WaitForSecondsRealtime(.5f);
            UnityEngine.InputSystem.InputSystem.Update();
            NUnit.Framework.Assert.AreEqual(Vector2.zero, motors(), "it ends on time");
            focus.SetValue(null, true);
            RoadOfTheOldKing.Input.ActiveInput.Set(true);
            RoadOfTheOldKing.Input.Rumble.SetScale(0f);
            RoadOfTheOldKing.Input.Rumble.Pulse(1f, 1f, .2f);
            NUnit.Framework.Assert.AreEqual(Vector2.zero, motors(), "Vibration off never rumbles");
            RoadOfTheOldKing.Input.Rumble.SetScale(1f);
            RoadOfTheOldKing.Input.ActiveInput.Set(false);
            RoadOfTheOldKing.Input.Rumble.Pulse(1f, 1f, .2f);
            NUnit.Framework.Assert.AreEqual(Vector2.zero, motors(), "no rumble while playing on mouse and keyboard");
            Debug.Log("PASS controller feel");
        }
        finally
        {
            RoadOfTheOldKing.Input.Rumble.Stop();
            foreach (var go in created) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = oldRouting;
            RoadOfTheOldKing.Input.ActiveInput.SetPlayStation(oldPlayStation);
            RoadOfTheOldKing.Input.ActiveInput.Set(oldGamepad);
            RoadOfTheOldKing.Input.Rumble.SetScale(oldVibration);
            if (!hadVibration) { PlayerPrefs.DeleteKey(vibrationKey); PlayerPrefs.Save(); }
            focus.SetValue(null, oldFocus);
        }
    }

    private static object Run()
    {
        // Run in Play Mode in an empty, unsaved scene. Uses the standard Player and Tutorial weapon prefabs, no save session.
        // All fixtures and synthetic input devices are removed in finally. Stop Play Mode afterward.
        if (!Application.isPlaying || UnityEngine.Object.FindFirstObjectByType<RoadOfTheOldKing.Progression.GameSession>() != null)
            throw new Exception("Use an empty Play Mode scene without a checkpoint session.");
        var results = new System.Collections.Generic.List<string>();
        System.Func<float> currentStamina = () => float.NaN;
        System.Action<bool,string> check = (ok,name) => results.Add(ok ? "PASS " + name : "FAIL " + name + " (stamina " + currentStamina() + ")");
        System.Func<float,float,bool> near = (a,b) => Mathf.Abs(a-b) < .001f;
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var created = new System.Collections.Generic.List<GameObject>();
        System.Func<string,GameObject> spawn = path => {
            var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path)); created.Add(go); return go;
        };
        var goPlayer = spawn("Assets/Prefabs/Player/Player.prefab");
        var player = goPlayer.GetComponent<RoadOfTheOldKing.Player.PlayerCombatController>();
        var weapon = spawn("Assets/Prefabs/Weapons/Axe.prefab").GetComponent<RoadOfTheOldKing.Weapons.AxeWeapon>();
        var stamina = player.GetComponent<RoadOfTheOldKing.Player.PlayerStamina>();
        var dash = player.GetComponent<RoadOfTheOldKing.Player.PlayerDash>();
        // Costs come from the current tuning, not literals (22 Oct: throw 20, dash 18, light 16).
        float throwCost = weapon.Settings.throwStaminaCost, lightCost = weapon.Settings.lightStaminaCost, recallCost = weapon.Settings.recallStaminaCost;
        currentStamina = () => stamina.Current;
        float dashCost = (float)dash.GetType().GetField("staminaCost", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(dash);
        var reaction = player.GetComponent<RoadOfTheOldKing.Combat.HitReaction>();
        var body = player.GetComponent<Rigidbody2D>();
        var combatInput = player.GetComponent<RoadOfTheOldKing.Input.PlayerCombatInput>();
        var moveInput = player.GetComponent<RoadOfTheOldKing.Input.PlayerMovementInput>();
        var movement = player.GetComponent<RoadOfTheOldKing.Player.PlayerMovement>();
        var sim = weapon.GetType().GetMethod("Simulate",flags);
        var process = player.GetType().GetMethod("ProcessInput",flags);
        var moveTick = movement.GetType().GetMethod("FixedUpdate",flags);
        var moveRead = moveInput.GetType().GetMethod("Update",flags);
        var view = weapon.GetComponent<RoadOfTheOldKing.Weapons.AxeView>();
        var viewTick = view.GetType().GetMethod("LateUpdate",flags);
        var recover = stamina.GetType().GetMethod("Tick",flags);
        var combatMap = (UnityEngine.InputSystem.InputActionMap)combatInput.GetType().GetField("actions",flags).GetValue(combatInput);
        var moveMap = (UnityEngine.InputSystem.InputActionMap)moveInput.GetType().GetField("actions",flags).GetValue(moveInput);
        var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
        var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
        var oldRouting = UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldSimulation = Physics2D.simulationMode;
        System.Action<float> tick = seconds => sim.Invoke(weapon,new object[]{seconds});
        System.Action<RoadOfTheOldKing.Input.CombatInputFrame> send = frame => process.Invoke(player,new object[]{frame});
        System.Action neutral = () => send(new RoadOfTheOldKing.Input.CombatInputFrame { Active = true });
        System.Action press = () => send(new RoadOfTheOldKing.Input.CombatInputFrame { Active = true, ThrowPressed = true, ThrowHeld = true });
        System.Action release = () => send(new RoadOfTheOldKing.Input.CombatInputFrame { Active = true, ThrowReleased = true });
        System.Action<Vector2> move = position => { body.position = position; player.transform.position = position; body.linearVelocity = Vector2.zero; Physics2D.SyncTransforms(); };
        System.Action reset = () => {
            weapon.CancelAction(); stamina.Restore(); dash.ResetAtRest(); reaction.Clear(); neutral();
            player.GetType().GetField("<AimDirection>k__BackingField",flags).SetValue(player,Vector2.right);
            player.GetType().GetField("recallUnlocked",flags).SetValue(player,false);
            move(new Vector2(100,100));
        };
        try {
            Physics2D.simulationMode = SimulationMode2D.Script;
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            combatMap.devices = new UnityEngine.InputSystem.InputDevice[]{ keyboard, mouse };
            moveMap.devices = new UnityEngine.InputSystem.InputDevice[]{ keyboard };
            combatInput.GetType().GetField("focused",flags).SetValue(combatInput,true);
            moveInput.GetType().GetField("hasFocus",flags).SetValue(moveInput,true);
            check(player.TryEquip(weapon), "Tutorial prefabs equip without scene dependencies"); reset();
            press(); tick(10f);
            check(weapon.CanCancelThrowAim && !weapon.IsAway && near(stamina.Current,100), "Ten-second held aim is free and does not launch");
            stamina.TrySpend(60); recover.Invoke(stamina,new object[]{1f});
            check(stamina.Current > 40f, "Aiming permits stamina recovery");
            check(!weapon.TryLightChop(Vector2.right) && !weapon.TryBeginCharge(), "Aim excludes melee and cleave");
            float beforeRelease = stamina.Current; release(); float paid = stamina.Current;
            check(near(paid,beforeRelease-throwCost) && weapon.ThrowCelIndex == 4, "Key-up spends throw cost once and starts release cels");
            release(); tick(.039f);
            check(!weapon.IsAway && near(stamina.Current,paid), "Duplicate key-up cannot spend again or launch early");
            tick(.001f);
            check(weapon.State == RoadOfTheOldKing.Weapons.AxeState.Flying && weapon.ThrowCelIndex == 5, "Physical launch shares cel-6 marker");

            reset(); send(new RoadOfTheOldKing.Input.CombatInputFrame { Active=true, ThrowPressed=true, ThrowReleased=true });
            tick(.119f); check(!weapon.IsAway && near(stamina.Current,100-throwCost), "Same-sample down/up preserves quick tap wind-up");
            tick(.001f); check(weapon.IsAway, "Same-sample tap launches at 120 ms");
            reset(); weapon.TryThrow(Vector2.right); tick(.5f);
            check(near(weapon.transform.position.x,100.00f+.38f*weapon.Settings.throwSpeed), "Large step advances only post-marker flight distance");
            reset(); weapon.TryThrow(Vector2.right); for(int i=0;i<25;i++) tick(.02f);
            check(near(weapon.transform.position.x,100.00f+.38f*weapon.Settings.throwSpeed), "Partitioned physics steps match large-step flight");

            reset(); press(); tick(.08f); release(); move(new Vector2(101,100)); weapon.SetAim(Vector2.up); tick(.14f);
            check(weapon.AttackDirection == Vector2.right && near(weapon.transform.position.x,101f+.1f*weapon.Settings.throwSpeed) && near(weapon.transform.position.y,100),
                "Aim locks at key-up; launch origin follows player until marker");
            reset(); stamina.TrySpend(100); press(); tick(.08f); release(); tick(.5f); // any positive stamina can act; zero cannot
            check(!weapon.IsThrowing && !weapon.IsAway && near(stamina.Current,0), "Unaffordable release returns to ready without launch");

            reset(); press(); tick(.08f);
            send(new RoadOfTheOldKing.Input.CombatInputFrame { Active=true, ThrowHeld=true, ChargePressed=true, ChargeHeld=true });
            send(new RoadOfTheOldKing.Input.CombatInputFrame { Active=true, ThrowReleased=true, ChargeReleased=true }); tick(.5f);
            check(!weapon.IsThrowing && weapon.State == RoadOfTheOldKing.Weapons.AxeState.Held && near(stamina.Current,100),
                "RMB cancels aim and consumes both stale release edges without cleave cost");
            reset(); press(); stamina.TrySpend(100);
            check(!dash.TryStart(Vector2.left) && weapon.CanCancelThrowAim, "Unaffordable dash leaves aim intact");
            stamina.Restore(); check(dash.TryStart(Vector2.left) && !weapon.IsThrowing && near(stamina.Current,100-dashCost), "Accepted dash cancels aim and pays only dash cost");
            dash.ResetAtRest(); release(); tick(.5f);
            check(!weapon.IsAway && near(stamina.Current,100-dashCost), "Key-up after dash cannot throw");
            reset(); press(); release(); check(!dash.TryStart(Vector2.left), "Committed release cannot be dash-cancelled");

            reset(); weapon.TryLightChop(Vector2.right); press(); tick(1f); release(); tick(.5f);
            check(!weapon.IsAway && !weapon.IsThrowing && near(stamina.Current,100-lightCost), "Rejected E during swing cannot become a queued throw");
            reset(); weapon.TryBeginCharge(); tick(.3f);
            send(new RoadOfTheOldKing.Input.CombatInputFrame { Active=true, ThrowPressed=true, ThrowHeld=true, ChargeReleased=true });
            check(weapon.State == RoadOfTheOldKing.Weapons.AxeState.Cleaving, "Rejected E does not swallow ongoing cleave release");

            reset(); press(); tick(.08f); send(default(RoadOfTheOldKing.Input.CombatInputFrame)); release(); tick(.5f);
            check(!weapon.IsAway && !weapon.IsThrowing && near(stamina.Current,100), "Control loss cancels held aim and ignores stale release");
            reset(); press(); release(); send(default(RoadOfTheOldKing.Input.CombatInputFrame)); tick(.5f);
            check(!weapon.IsAway && near(stamina.Current,100-throwCost), "Interrupted committed throw retains cost but never spawns");
            reset(); press(); release(); reaction.GetType().GetField("staggerUntil",flags).SetValue(reaction,Time.time+1f); tick(.5f);
            check(!weapon.IsAway && !weapon.IsThrowing && near(stamina.Current,100-throwCost), "Stagger before marker prevents launch even without an Update");
            reset(); press(); combatInput.enabled=false; tick(.5f); combatInput.enabled=true; neutral(); release();
            check(!weapon.IsThrowing && !weapon.IsAway, "Disabled input cancels aim before the physics marker");
            reset(); press(); release(); player.enabled=false; tick(.5f); player.enabled=true; neutral();
            check(!weapon.IsAway && !weapon.IsThrowing, "Disabled combat controller cannot launch a pending throw");
            reset(); press(); player.GetType().GetMethod("OnApplicationFocus",flags).Invoke(player,new object[]{false}); tick(.5f); neutral(); release();
            check(!weapon.IsAway && !weapon.IsThrowing, "Focus loss consumes the outstanding E cycle");
            reset(); press(); Time.timeScale=0f; tick(.5f); Time.timeScale=1f; neutral(); release();
            check(!weapon.IsAway && !weapon.IsThrowing, "Pause cannot launch a pending throw");

            reset(); player.UnlockRecall(); weapon.TryThrow(Vector2.right); tick(.22f); press(); tick(.2f);
            check(weapon.State == RoadOfTheOldKing.Weapons.AxeState.Held, "E press recalls an away weapon during throw recovery");
            release(); tick(.5f);
            check(!weapon.IsThrowing && !weapon.IsAway && near(stamina.Current,100-throwCost-recallCost), "Holding Recall through catch then key-up never rethrows");

            reset();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update(); neutral();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E));
            UnityEngine.InputSystem.InputSystem.Update(); var down=combatInput.Read(); send(down);
            check(down.ThrowPressed && down.ThrowHeld && !down.ThrowReleased && weapon.CanCancelThrowAim, "Actual E binding reports press and held state");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.E,UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.D,UnityEngine.InputSystem.Key.LeftShift));
            UnityEngine.InputSystem.InputSystem.Update(); moveRead.Invoke(moveInput,null); moveTick.Invoke(movement,null);
            check(near(body.linearVelocity.magnitude,4.5f*.55f) && body.linearVelocity.x>0 && body.linearVelocity.y>0 && !movement.IsSprinting && near(stamina.Current,100),
                "Actual WASD moves diagonally at 55% walk while aiming; Shift spends nothing");
            check(Vector2.Dot(movement.FacingDirection,Vector2.right)>.999f, "Aim facing stays independent of world-relative travel");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update(); var up=combatInput.Read(); send(up);
            check(up.ThrowReleased && !up.ThrowHeld && near(stamina.Current,100-throwCost), "Actual E key-up commits once");

            reset(); var wall=new GameObject("Throw verification wall"); created.Add(wall); wall.transform.position=new Vector3(100.6f,100,0);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,1f); Physics2D.SyncTransforms(); press(); tick(.08f); viewTick.Invoke(view,null);
            var guide=(RoadOfTheOldKing.Weapons.ThrowAimGuide)view.GetType().GetField("guide",flags).GetValue(view);
            // Box2D's contact tolerance can shift the cast by a few millimetres.
            check(weapon.AimDistance>.35f && weapon.AimDistance<.40f && guide!=null && guide.Showing && guide.Blocked &&
                near(guide.Reach,weapon.AimDistance), "Aim guide uses flight radius and marks the nearby wall it stops at");
            release(); tick(.5f);
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Stuck && weapon.transform.position.x<100.6f, "Delayed launch cannot skip a wall next to the player");
            wall.SetActive(false); reset();
            var target=spawn("Assets/Prefabs/Tutorial/TutorialPracticeDummy.prefab"); target.transform.position=new Vector3(103,100,0); target.GetComponent<Rigidbody2D>().position=target.transform.position;
            var extra=new GameObject("Duplicate collider"); extra.transform.SetParent(target.transform,false); extra.AddComponent<CircleCollider2D>().radius=.2f;
            var targetHealth=target.GetComponent<RoadOfTheOldKing.Combat.Damageable>(); Physics2D.SyncTransforms(); int before=targetHealth.Health;
            press(); tick(.5f); viewTick.Invoke(view,null);
            check(targetHealth.Health==before, "Aim preview never damages a target");
            release(); tick(.5f); tick(.2f);
            check(targetHealth.Health==before-weapon.Settings.throwDamage && weapon.State==RoadOfTheOldKing.Weapons.AxeState.Stuck,
                "Throw still hits once with multiple colliders and sticks to its target");
            move(weapon.transform.position); tick(.02f);
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held, "Walk-over retrieval works before Recall unlock");
            target.SetActive(false); reset(); player.UnlockRecall(); weapon.TryThrow(Vector2.right); tick(.8f); move(new Vector2(90,100)); tick(.02f);
            check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Returning, "Distance-based automatic Recall still starts");
            tick(2f); check(weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held, "Automatic Recall still reaches its owner");
            // Material feedback (3 Nov): a tagged object answers once, by its material; untagged stays silent; a break says so.
            var strikes=new System.Collections.Generic.List<RoadOfTheOldKing.World.SurfaceStrike>();
            weapon.SurfaceStruck+=strike=>strikes.Add(strike);
            reset();
            var struck=new GameObject("Tagged wall");created.Add(struck);struck.transform.position=new Vector2(101.5f,100);struck.AddComponent<BoxCollider2D>().size=new Vector2(.5f,4);
            System.Action<RoadOfTheOldKing.World.SurfaceMaterial> tagWall=m=>{var s=struck.GetComponent<RoadOfTheOldKing.World.SurfaceTag>()??struck.AddComponent<RoadOfTheOldKing.World.SurfaceTag>();s.GetType().GetField("material",flags).SetValue(s,m);};
            tagWall(RoadOfTheOldKing.World.SurfaceMaterial.Stone);Physics2D.SyncTransforms();
            System.Action chop=()=>{reset();strikes.Clear();weapon.TryLightChop(Vector2.right);for(int i=0;i<30;i++)tick(.02f);};
            chop();
            check(strikes.Count==1&&strikes[0].Material==RoadOfTheOldKing.World.SurfaceMaterial.Stone&&!strikes[0].Broke,"A chop at a tagged wall answers once, by its material");
            UnityEngine.Object.DestroyImmediate(struck.GetComponent<RoadOfTheOldKing.World.SurfaceTag>());chop();
            check(strikes.Count==0,"An untagged wall stays silent");
            struck.SetActive(false);
            var strikeBush=spawn("Assets/Prefabs/World/Shared/BreakableBush.prefab");strikeBush.transform.position=new Vector2(101.5f,100);Physics2D.SyncTransforms();chop();
            check(strikes.Count==1&&strikes[0].Material==RoadOfTheOldKing.World.SurfaceMaterial.Brush&&strikes[0].Broke&&!strikeBush.activeSelf,"Breaking a bush answers once, as brush, and says it broke");
            var strikeStone=spawn("Assets/Prefabs/World/Shared/CrackedStone.prefab");strikeStone.transform.position=new Vector2(101.5f,100);Physics2D.SyncTransforms();chop();
            check(strikes.Count==1&&strikes[0].Material==RoadOfTheOldKing.World.SurfaceMaterial.Stone&&!strikes[0].Broke&&strikeStone.activeSelf,"A cracked stone clanks at a chop it refuses");
            strikeStone.SetActive(false);
            var strikePot=spawn("Assets/Prefabs/World/Shared/ClayPot.prefab");strikePot.transform.position=new Vector2(101.5f,100);Physics2D.SyncTransforms();chop();
            check(strikes.Count==1&&strikes[0].Material==RoadOfTheOldKing.World.SurfaceMaterial.Pottery&&strikes[0].Broke,"A pot break plays one ceramic answer");
            strikePot.SetActive(false);
            struck.transform.position=new Vector2(104f,100);struck.SetActive(true);tagWall(RoadOfTheOldKing.World.SurfaceMaterial.Wood);Physics2D.SyncTransforms();reset();strikes.Clear();weapon.TryThrow(Vector2.right);for(int i=0;i<40;i++)tick(.02f);
            check(strikes.Count==1&&strikes[0].Material==RoadOfTheOldKing.World.SurfaceMaterial.Wood&&weapon.State==RoadOfTheOldKing.Weapons.AxeState.Stuck,"A thrown axe lodging in a tagged wall answers once");
            move(weapon.transform.position);tick(.02f);struck.SetActive(false);
            reset(); press(); release();
            player.GetComponent<RoadOfTheOldKing.Combat.Damageable>().ReceiveHit(new RoadOfTheOldKing.Combat.CombatHit(target,RoadOfTheOldKing.Combat.AttackKind.EnemyMelee,1000,Vector2.right,0f,.1f)); tick(.5f);
            check(!player.enabled && !weapon.IsThrowing && weapon.State==RoadOfTheOldKing.Weapons.AxeState.Held, "Defeat cancels pending launch and restores safe weapon state");
        } finally {
            Time.timeScale=1f; Physics2D.simulationMode=oldSimulation;
            foreach(var go in created) if(go!=null) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
            UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode=oldRouting;
        }
        return string.Join("\n",results.ToArray());
    }
}
#endif
