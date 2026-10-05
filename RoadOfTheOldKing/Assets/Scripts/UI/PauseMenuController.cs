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
        private IDisposable pause;
        private IReadOnlyList<PauseMenuAction> actions;
        private Coroutine restoring;
        private bool customActions, inSettings;
        public bool IsOpen => pause != null;
        public bool BlocksGameplay => IsOpen || restoring != null;

        private void Awake()
        {
            input = GetComponent<PauseMenuInput>(); view = GetComponent<PauseMenuView>();
            health = GetComponent<PlayerHealth>(); locks = GetComponent<PlayerControlLocks>();
            dash = GetComponent<PlayerDash>();
            actions = PauseMenuCommands.CreateDefault(() => ShowSettings(0));
        }

        private void OnEnable()
        {
            input.ToggleRequested += Toggle; input.NavigationRequested += RouteNavigate; input.SubmitRequested += RouteSubmit;
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

        private void RouteNavigate(int direction) => MenuStack.Top?.Navigate(direction);
        private void RouteSubmit() => MenuStack.Top?.Submit();

        public bool Open()
        {
            if (!isActiveAndEnabled || BlocksGameplay || health == null || !health.IsAlive || MenuStack.IsAnyOpen) return false;
            // Rebuilt per open so setting toggles show their current state.
            if (!customActions) actions = PauseMenuCommands.CreateDefault(() => ShowSettings(0));
            inSettings = false;
            if (!view.Show(actions, Execute)) return false;
            if (locks != null) locks.Lock(this);
            if (dash != null) dash.Cancel();
            pause = SimulationPause.Acquire(true);
            MenuStack.Push(this);
            // Pausing is a likely moment to leave (or close the browser tab): remember the exact spot.
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
            else Close();
        }

        // Settings is the second entry of the default list; Back from its page returns there.
        private const int SettingsRow = 1;

        private void ShowMain(int selected)
        {
            inSettings = false;
            if (!customActions) actions = PauseMenuCommands.CreateDefault(() => ShowSettings(0));
            view.Show(actions, Execute, selected);
        }

        // Rebuilt per show so toggles name their current state.
        private void ShowSettings(int selected)
        {
            inSettings = true;
            actions = SettingsCommands.Create(() => ShowMain(SettingsRow));
            view.Show(actions, Execute, selected, "SETTINGS");
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
            // A setting stays on its row; a command that opened another page already rebuilt the view.
            if (!IsOpen || !ReferenceEquals(previous, actions)) return;
            if (inSettings) ShowSettings(selected);
            else ShowMain(selected);
        }

        private void OnDisable()
        {
            input.ToggleRequested -= Toggle; input.NavigationRequested -= RouteNavigate; input.SubmitRequested -= RouteSubmit;
            if (restoring != null) { StopCoroutine(restoring); restoring = null; }
            MenuStack.Remove(this);
            view.Hide(); pause?.Dispose(); pause = null;
            if (locks != null) locks.Unlock(this);
        }
    }
}
