#if UNITY_EDITOR
using TheLostShrine.Input;
using TheLostShrine.Player;
using TheLostShrine.Prototype;
using TheLostShrine.Weapons;
using UnityEditor;
using UnityEngine;

namespace TheLostShrine.Editor
{
    // Editor-only input source. The actual player, physics, weapon and effects do the work.
    [DefaultExecutionOrder(-300)]
    public sealed class CombatPreviewDriver : MonoBehaviour, ICombatInput
    {
        public enum Action { Chop, Combo, Cleave, ThrowRecall }
        public PlayerCombatController Player;
        public HatchetWeapon Weapon;
        public Camera Camera;
        public PracticeTarget Target;
        public GameObject Wall;
        public float Angle, TargetDistance = 1.4f;
        public bool ShowTarget = true, ShowWall, Loop, PauseAtActive;
        public Action SelectedAction;
        public bool Running { get; private set; }
        public int Hits { get; private set; }
        private CombatInputFrame frame;
        private int stage, clearInputFrames;
        private float started, repeatAt;
        private bool pauseArmed;
        private Action runningAction;
        public Vector2 Direction => new Vector2(Mathf.Cos(Angle * Mathf.Deg2Rad), Mathf.Sin(Angle * Mathf.Deg2Rad));

        public void Initialize()
        {
            Player.TryEquip(Weapon);
            Player.UnlockRecall();
            Weapon.HitConfirmed += OnHit;
            ResetPreview();
        }

        private void OnHit(TheLostShrine.Combat.CombatHit hit) => Hits++;
        private void OnDestroy() { if (Weapon != null) Weapon.HitConfirmed -= OnHit; }

        public void ResetPreview()
        {
            Running = false; repeatAt = float.PositiveInfinity; stage = 0; Hits = 0;
            clearInputFrames = 2;
            Weapon.CancelAction();
            Player.GetComponent<PlayerStamina>().Restore();
            var rb = Player.GetComponent<Rigidbody2D>();
            rb.position = Vector2.zero; rb.linearVelocity = Vector2.zero;
            Player.transform.position = Vector3.zero;
            if (Target != null)
            {
                Target.gameObject.SetActive(true);
                Target.ResetTarget();
                Target.transform.position = Direction * TargetDistance;
                Target.GetComponent<Rigidbody2D>().position = Target.transform.position;
                Target.gameObject.SetActive(ShowTarget);
            }
            Wall.SetActive(ShowWall);
            Wall.transform.SetPositionAndRotation(Direction * .9f, Quaternion.Euler(0, 0, Angle));
            Physics2D.SyncTransforms();
        }

        public void Replay()
        {
            ResetPreview();
            Running = true; started = Time.time; pauseArmed = PauseAtActive;
            runningAction = SelectedAction;
        }

        public CombatInputFrame Read() => frame;

        private void Update()
        {
            frame = new CombatInputFrame { Active = true,
                PointerPosition = Camera.WorldToScreenPoint(Player.transform.position + (Vector3)Direction * 4f) };
            // Cancel buffered input, then send an active neutral frame to release held buttons.
            if (clearInputFrames > 0) { frame.Active = clearInputFrames == 1; clearInputFrames--; return; }
            if (!Running)
            {
                if (Loop && Time.time >= repeatAt) { Replay(); return; }
                else return;
            }
            switch (runningAction)
            {
                case Action.Chop:
                case Action.Combo:
                    int count = runningAction == Action.Combo ? 3 : 1;
                    if (Weapon.State == HatchetState.Held && stage < count)
                    { frame.LightPressed = true; stage++; }
                    else if (stage == count && Weapon.State == HatchetState.Held) Finish();
                    break;
                case Action.Cleave:
                    if (stage == 0) { frame.ChargePressed = frame.ChargeHeld = true; stage = 1; }
                    else if (stage == 1 && Time.time - started < Weapon.Settings.fullCharge + .05f) frame.ChargeHeld = true;
                    else if (stage == 1) { frame.ChargeReleased = true; stage = 2; }
                    else if (!Weapon.IsAttacking) Finish();
                    break;
                case Action.ThrowRecall:
                    if (stage == 0) { frame.ThrowPressed = frame.ThrowHeld = true; stage = 1; }
                    else if (stage == 1) { frame.ThrowReleased = true; stage = 2; }
                    else if (stage == 2 && Weapon.IsAway && Time.time - started >= .85f)
                    { frame.ThrowPressed = frame.ThrowHeld = true; stage = 3; }
                    // A nearby target/wall can return the axe through ordinary on-foot pickup.
                    else if (stage == 2 && !Weapon.IsAway && !Weapon.IsThrowing && Time.time - started >= .85f) Finish();
                    else if (stage == 3) { frame.ThrowReleased = true; stage = 4; }
                    else if (stage == 4 && !Weapon.IsAway && !Weapon.IsThrowing) Finish();
                    break;
            }
        }

        private void Finish() { Running = false; repeatAt = Time.time + .7f; }

        private void LateUpdate()
        {
            if (PauseAtActive && pauseArmed && Weapon.LightPhase == MeleePhase.Active)
            {
                pauseArmed = false;
                EditorApplication.isPaused = true;
            }
        }
    }
}
#endif
