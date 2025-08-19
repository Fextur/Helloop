using UnityEngine;
using Helloop.StateMachines;
using Helloop.Player;

namespace Helloop.Weapons.States
{
    /// <summary>
    /// Central melee controller: Skyrim-style routing (tap/hold) + dash overrides.
    /// - External calls (e.g., WeaponManager -> rightWeapon.Use()) arm a press via HandleInput().
    /// - Actual routing (Light/Heavy/Dash) occurs here during Update() while in MeleeReadyState.
    ///
    /// Behavior:
    ///   Tap  (release < 0.45s)                -> Light
    ///   Hold (>= 0.45s, auto-fire; no release)-> Heavy
    ///   Dash overrides (highest priority):
    ///      * Press while dashing                 -> Dash
    ///      * Dash starts during hold (pre-heavy) -> Dash
    ///      * Press within 0.25s after dash start -> Dash
    ///
    /// Notes:
    /// - Only a fresh press arms a decision; re-entering Ready while holding does not resume an old press.
    /// - Attack states ignore input; all reads happen here while Ready.
    /// </summary>
    public class MeleeWeaponStateMachine
    {
        private readonly StateMachine<MeleeWeapon> stateMachine;
        private readonly MeleeWeapon owner;

        // Cooldown tracking (uses owner's scaled timing)
        private float lastSwingTime = -1f;

        // Tap/Hold routing
        private bool pressArmed;
        private float pressStartTime;
        private const float HoldToHeavySeconds = 0.45f; // fixed threshold

        // Dash tracking (movement state is source of truth)
        private PlayerMovement playerMovement;
        private bool wasDashing;
        private bool dashStartedThisFrame;
        private float lastDashStartTime = -999f;
        private const float DashGraceSeconds = 0.25f; // press within grace after dash start -> dash

        private enum MeleeAttackMode { Light, Heavy, Dash }

        public MeleeWeaponStateMachine(MeleeWeapon weapon)
        {
            owner = weapon;
            stateMachine = new StateMachine<MeleeWeapon>(weapon);
            playerMovement = owner.GetComponentInParent<PlayerMovement>();

            wasDashing = IsPlayerDashing();
            if (wasDashing) lastDashStartTime = Time.time;

            pressArmed = false;
        }

        public void Initialize()
        {
            ChangeState(new MeleeReadyState());
        }

        public void Update()
        {
            TrackDashEdge();
            stateMachine.Update();
            RouteInputWhileReady();
            EnforceOwnerTransitions();
            dashStartedThisFrame = false; // reset dash edge each frame
        }

        /// <summary>
        /// Back-compat for existing input path (e.g., WeaponManager -> rightWeapon.Use() on press).
        /// Treat as "press armed" instead of firing Light immediately.
        /// Also applies dash override if within grace or already dashing.
        /// </summary>
        public void HandleInput()
        {
            if (!(stateMachine.CurrentState is MeleeReadyState)) return;
            if (!CanSwingNow()) return;

            // Immediate dash override if currently dashing or inside grace
            if (IsPlayerDashing() || (Time.time - lastDashStartTime) <= DashGraceSeconds)
            {
                FireAttack(MeleeAttackMode.Dash);
                return;
            }

            if (!pressArmed)
            {
                pressArmed = true;
                pressStartTime = Time.time;
            }
        }

        // ------------------------ Internal helpers ------------------------

        private void RouteInputWhileReady()
        {
            if (!(stateMachine.CurrentState is MeleeReadyState)) return;
            if (!CanSwingNow()) return;

            // Legacy Input reads (Project: Active Input Handling = Both)
            bool keyDown = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.V);
            bool keyHeld = Input.GetMouseButton(1) || Input.GetKey(KeyCode.V);
            bool keyUp = Input.GetMouseButtonUp(1) || Input.GetKeyUp(KeyCode.V);

            // Dash started while holding (and heavy hasn't fired yet) -> Dash
            if (dashStartedThisFrame && pressArmed && !HasReachedHeavyThreshold())
            {
                pressArmed = false;
                FireAttack(MeleeAttackMode.Dash);
                return;
            }

            // Fresh keyDown arms the press (no pre-fire Light)
            if (!pressArmed && keyDown)
            {
                if (IsPlayerDashing() || (Time.time - lastDashStartTime) <= DashGraceSeconds)
                {
                    FireAttack(MeleeAttackMode.Dash);
                    return;
                }

                pressArmed = true;
                pressStartTime = Time.time;
                return;
            }

            // Hold path -> Heavy auto-fire at threshold (no release needed)
            if (pressArmed && keyHeld)
            {
                if (HasReachedHeavyThreshold())
                {
                    pressArmed = false;
                    FireAttack(MeleeAttackMode.Heavy);
                }
                return;
            }

            // Release before threshold -> Light
            if (pressArmed && keyUp)
            {
                pressArmed = false;
                FireAttack(MeleeAttackMode.Light);
            }
        }

        private void FireAttack(MeleeAttackMode mode)
        {
            MarkSwingTime();

            switch (mode)
            {
                case MeleeAttackMode.Dash:
                    ChangeState(new MeleeDashAttackState());
                    break;
                case MeleeAttackMode.Heavy:
                    ChangeState(new MeleeSwipeHeavyState());
                    break;
                default:
                    ChangeState(new MeleeSwingLightState());
                    break;
            }
        }

        private void TrackDashEdge()
        {
            dashStartedThisFrame = false;
            bool dashingNow = IsPlayerDashing();
            if (dashingNow && !wasDashing)
            {
                lastDashStartTime = Time.time;
                dashStartedThisFrame = true;
            }
            wasDashing = dashingNow;
        }

        private bool IsPlayerDashing()
        {
            return playerMovement != null && playerMovement.IsInState<Player.States.PlayerDashingState>();
        }

        private bool HasReachedHeavyThreshold()
        {
            return (Time.time - pressStartTime) >= HoldToHeavySeconds;
        }

        // Public so attack states/external code can transition safely (unchanged signature elsewhere)
        public void ChangeState(IState<MeleeWeapon> next)
        {
            stateMachine.ChangeState(next);
        }

        private void EnforceOwnerTransitions()
        {
            if (owner.GetCurrentDurability() <= 0 && !(stateMachine.CurrentState is MeleeBrokenState))
            {
                ChangeState(new MeleeBrokenState());
            }
        }

        public StateMachine<MeleeWeapon> GetInternalStateMachine() => stateMachine;

        public bool IsInState<T>() where T : class, IState<MeleeWeapon>
        {
            return stateMachine.IsInState<T>();
        }

        public void MarkSwingTime()
        {
            lastSwingTime = Time.time;
        }

        public bool CanSwingNow()
        {
            float gate = owner.ScaledSwingTime > 0f ? owner.ScaledSwingTime : Mathf.Max(0.001f, owner.Data.swingTime);
            return Time.time - lastSwingTime >= gate;
        }
    }
}
