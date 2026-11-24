using System;
using System.Collections;
using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.UI
{
    // Owns the pause modal and routes menu input. Escape steps back in whichever menu is on top of the
    // MenuStack (bonfire, defeat...) and opens pause only when none is open.
    [DisallowMultipleComponent, RequireComponent(typeof(PauseMenuView), typeof(PauseMenuInput))]
    public sealed class PauseMenuController : MonoBehaviour, IModalMenu
    {
        private PauseMenuInput input;
        private PauseMenuView view;
        private PlayerHealth health;
        private PlayerControlLocks locks;
        private PlayerDash dash;
        private PlayerCombatController combat;
        private IDisposable pause;
        private IReadOnlyList<PauseMenuAction> actions;
        private Coroutine restoring;
        private bool customActions, inSettings, inControls;
        public bool IsOpen => pause != null;
        public bool BlocksGameplay => IsOpen || restoring != null;

        private void Awake()
        {
            input = GetComponent<PauseMenuInput>(); view = GetComponent<PauseMenuView>();
            health = GetComponent<PlayerHealth>(); locks = GetComponent<PlayerControlLocks>();
            dash = GetComponent<PlayerDash>(); combat = GetComponent<PlayerCombatController>();
            actions = MainActions();
        }

        private PauseMenuAction[] MainActions() => PauseMenuCommands.CreateDefault(() => ShowSettings(0), ShowControls);

        private void OnEnable()
        {
            input.ToggleRequested += Toggle; input.NavigationRequested += RouteNavigate; input.SubmitRequested += RouteSubmit;
            input.BackRequested += RouteBack; input.AdjustRequested += RouteAdjust; input.AdjustRepeated += RouteAdjustRepeat;
        }

        // Composition seam for additional commands and save-free verification.
        public void ConfigureActions(IReadOnlyList<PauseMenuAction> commands)
        {
            if (BlocksGameplay) throw new InvalidOperationException("Close the menu before replacing its actions.");
            actions = commands ?? throw new ArgumentNullException(nameof(commands));
            customActions = true;
        }

        public void Toggle()
        {
            if (restoring != null) return;
            var top = MenuStack.Top;
            // Escape belongs to the top menu: step back or close it, never open two menus.
            if (top != null) { top.Back(); return; }
            Open();
        }

        // Controller B: step back in the top menu; with nothing open it is a dodge, never pause.
        private void RouteBack() { if (restoring == null) MenuStack.Top?.Back(); }
        private void RouteNavigate(int direction) => MenuStack.Top?.Navigate(direction);
        private void RouteSubmit() => MenuStack.Top?.Submit();
        // Left/right: only pages with values to change (settings) take it.
        private void RouteAdjust(int direction) { if (MenuStack.Top is PauseMenuController menu) menu.Adjust(direction); }
        private void RouteAdjustRepeat(int direction) { if (MenuStack.Top is PauseMenuController menu) menu.Adjust(direction, true); }
        public void Adjust(int direction, bool repeat = false) { if (IsOpen && inSettings) view.Adjust(direction, repeat); }

        public bool Open()
        {
            if (!isActiveAndEnabled || BlocksGameplay || health == null || !health.IsAlive || MenuStack.IsAnyOpen) return false;
            // Rebuilt per open so setting toggles show their current state.
            if (!customActions) actions = MainActions();
            inSettings = inControls = false;
            if (!view.Show(actions, Execute)) return false;
            if (locks != null) locks.Lock(this);
            if (dash != null) dash.Cancel();
            pause = SimulationPause.Acquire(true);
            MenuStack.Push(this);
            // Pausing is a likely moment to leave: remember the exact spot.
            GameSession.Instance?.RecordResume();
            return true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            MenuStack.Remove(this);
            view.Hide(); pause.Dispose(); pause = null;
            // Keep the closing key/click out of gameplay for the rest of this frame.
            restoring = StartCoroutine(UnlockNextFrame());
        }

        private IEnumerator UnlockNextFrame() { yield return null; if (locks != null) locks.Unlock(this); restoring = null; }

        public void Back()
        {
            if (inSettings) ShowMain(SettingsRow);
            else if (inControls) ShowMain(ControlsRow);
            else Close();
        }

        // Their rows in the default list; Back from either page returns there.
        private const int ControlsRow = 1, SettingsRow = 2;

        private void ShowMain(int selected)
        {
            inSettings = inControls = false;
            if (!customActions) actions = MainActions();
            view.Show(actions, Execute, selected);
        }

        private void ShowControls()
        {
            inControls = true;
            actions = new[] { new PauseMenuAction("Back", "", () => ShowMain(ControlsRow), closeOnExecute: false) };
            view.ShowControls(actions, Execute, combat != null && combat.CanRecall);
        }

        // Option rows that show and change their own values; Back returns to the Settings row.
        private void ShowSettings(int selected)
        {
            inSettings = true;
            view.ShowOptions(SettingsOptions.Create(() => ShowMain(SettingsRow)), "SETTINGS", selected);
        }
        public void Navigate(int direction) => view.Navigate(direction);
        public void Submit() => view.Submit();

        private void Execute(PauseMenuAction action)
        {
            if (!IsOpen || !action.Enabled) return;
            if (action.CloseOnExecute) { Close(); action.Execute(); return; }
            var previous = actions;
            int selected = 0;
            for (int i = 0; i < actions.Count; i++) if (ReferenceEquals(actions[i], action)) { selected = i; break; }
            action.Execute();
            // A command that opened another page (settings, controls) already rebuilt the view.
            if (!IsOpen || inSettings || !ReferenceEquals(previous, actions)) return;
            ShowMain(selected);
        }

        private void OnDisable()
        {
            input.ToggleRequested -= Toggle; input.NavigationRequested -= RouteNavigate; input.SubmitRequested -= RouteSubmit;
            input.BackRequested -= RouteBack; input.AdjustRequested -= RouteAdjust; input.AdjustRepeated -= RouteAdjustRepeat;
            if (restoring != null) { StopCoroutine(restoring); restoring = null; }
            MenuStack.Remove(this);
            view.Hide(); pause?.Dispose(); pause = null;
            if (locks != null) locks.Unlock(this);
        }
    }
}
