using UnityEngine;
using Helloop.StateMachines;
using Helloop.Player.States;

namespace Helloop.Player
{
    public class PlayerMovementStateMachine
    {
        private StateMachine<PlayerMovement> stateMachine;
        private StateTransitionValidator<PlayerMovement> transitionValidator;
        private PlayerMovement owner;

        public IState<PlayerMovement> CurrentState => stateMachine.CurrentState;
        public bool IsTransitioning => stateMachine.IsTransitioning;

        public PlayerMovementStateMachine(PlayerMovement player)
        {
            owner = player;
            stateMachine = new StateMachine<PlayerMovement>(player);
            transitionValidator = new StateTransitionValidator<PlayerMovement>();
            SetupTransitions();
        }

        private void SetupTransitions()
        {
            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerWalkingState), typeof(PlayerDashingState),
                player => player.ShouldStartDashing(),
                "Normal to Dashing"
            ));

            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerWalkingState), typeof(PlayerStealthState),
                player => player.ShouldEnterStealth(),
                "Normal to Stealth"
            ));

            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerDashingState), typeof(PlayerWalkingState),
                player => !player.IsDashing,
                "Dashing to Normal"
            ));

            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerStealthState), typeof(PlayerWalkingState),
                player => player.ShouldExitStealth(),
                "Stealth to Normal"
            ));

            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerStealthState), typeof(PlayerDashingState),
                player => player.ShouldStartDashing(),
                "Stealth to Dashing"
            ));

            transitionValidator.AddTransition(new StateTransition<PlayerMovement>(
                typeof(PlayerDashingState), typeof(PlayerStealthState),
                player => player.ShouldEnterStealth() && player.ShouldEndDashing(),
                "Dashing to Stealth"
            ));
        }

        public void Initialize()
        {
            stateMachine.ChangeState(new PlayerWalkingState());
        }

        public void Update()
        {
            CheckForStateTransitions();
            stateMachine.Update();
        }

        private void CheckForStateTransitions()
        {
            IState<PlayerMovement> validTransition = transitionValidator.GetValidTransition(owner, stateMachine.CurrentState);
            if (validTransition != null)
            {
                stateMachine.ChangeState(validTransition);
            }
        }

        public void ForceChangeState(IState<PlayerMovement> newState)
        {
            stateMachine.ForceState(newState);
        }

        public bool IsInState<T>() where T : class, IState<PlayerMovement>
        {
            return stateMachine.IsInState<T>();
        }

        public T GetCurrentState<T>() where T : class, IState<PlayerMovement>
        {
            return stateMachine.GetCurrentState<T>();
        }
    }
}