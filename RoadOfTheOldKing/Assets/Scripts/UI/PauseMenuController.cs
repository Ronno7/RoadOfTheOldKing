using System;
using System.Collections;
using System.Collections.Generic;
using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.UI
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
        public bool IsOpen => pause != null;
        public bool BlocksGameplay => IsOpen || restoring != null;

        private void Awake()
        {
            input = GetComponent<PauseMenuInput>(); view = GetComponent<PauseMenuView>();
            health = GetComponent<PlayerHealth>(); locks = GetComponent<PlayerControlLocks>();
            dash = GetComponent<PlayerDash>();
            actions = PauseMenuCommands.CreateDefault();
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
            if (!view.Show(actions, Execute)) return false;
            if (locks != null) locks.Lock(this);
            if (dash != null) dash.Cancel();
            pause = SimulationPause.Acquire(true);
            MenuStack.Push(this);
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

        public void Back() => Close();
        public void Navigate(int direction) => view.Navigate(direction);
        public void Submit() => view.Submit();

        private void Execute(PauseMenuAction action)
        {
            if (!IsOpen || !action.Enabled) return;
            Close(); action.Execute();
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
