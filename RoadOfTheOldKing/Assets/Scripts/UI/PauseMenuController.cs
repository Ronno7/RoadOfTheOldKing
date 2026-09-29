using System;
using System.Collections;
using System.Collections.Generic;
using TheLostShrine.Input;
using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(PauseMenuView), typeof(PauseMenuInput))]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private PauseMenuInput input;
        private PauseMenuView view;
        private PlayerHealth health;
        private PlayerBonfireInteraction bonfire;
        private IDisposable pause;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private IReadOnlyList<PauseMenuAction> actions;
        private Coroutine restoring;
        public bool IsOpen => pause != null;
        public bool BlocksGameplay => IsOpen || restoring != null;

        private void Awake()
        {
            input = GetComponent<PauseMenuInput>(); view = GetComponent<PauseMenuView>();
            health = GetComponent<PlayerHealth>(); bonfire = GetComponent<PlayerBonfireInteraction>();
            actions = PauseMenuCommands.CreateDefault();
        }
        private void OnEnable()
        {
            input.ToggleRequested += Toggle; input.NavigationRequested += Navigate; input.SubmitRequested += Submit;
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
            if (IsOpen) { Close(); return; }
            // Escape dismisses the current bonfire modal first, without opening two menus.
            if (bonfire != null && bonfire.IsOpen) { bonfire.Close(); return; }
            Open();
        }
        public bool Open()
        {
            if (!isActiveAndEnabled || BlocksGameplay || health == null || !health.IsAlive) return false;
            if (bonfire != null && bonfire.IsOpen) return false;
            if (!view.Show(actions, Execute)) return false;
            Suspend(GetComponent<PlayerMovementInput>()); Suspend(GetComponent<PlayerCombatInput>());
            Suspend(GetComponent<PlayerCombatController>()); Suspend(bonfire);
            GetComponent<PlayerDash>()?.Cancel();
            pause = SimulationPause.Acquire(true);
            return true;
        }
        private void Suspend(Behaviour component)
        {
            if (component != null && component.enabled) { suspended.Add(component); component.enabled = false; }
        }
        private void RestoreInputs()
        {
            if (health != null && health.IsAlive)
                foreach (var component in suspended) if (component != null) component.enabled = true;
            suspended.Clear();
        }
        public void Close()
        {
            if (!IsOpen) return;
            view.Hide(); pause.Dispose(); pause = null;
            // Keep the closing key/click out of gameplay for the rest of this frame.
            restoring = StartCoroutine(RestoreNextFrame());
        }
        private IEnumerator RestoreNextFrame() { yield return null; RestoreInputs(); restoring = null; }
        private void Execute(PauseMenuAction action)
        {
            if (!IsOpen || !action.Enabled) return;
            Close(); action.Execute();
        }
        private void Navigate(int direction) { if (IsOpen) view.Navigate(direction); }
        private void Submit() { if (IsOpen) view.Submit(); }
        private void OnDisable()
        {
            input.ToggleRequested -= Toggle; input.NavigationRequested -= Navigate; input.SubmitRequested -= Submit;
            if (restoring != null) { StopCoroutine(restoring); restoring = null; }
            view.Hide(); pause?.Dispose(); pause = null; RestoreInputs();
        }
    }
}
