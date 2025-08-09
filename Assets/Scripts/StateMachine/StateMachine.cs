using UnityEngine;

namespace Helloop.StateMachines
{
    public class StateMachine<T> where T : class
    {
        private IState<T> currentState;
        private IState<T> previousState;
        private T owner;
        private bool isTransitioning;

        public IState<T> CurrentState => currentState;
        public IState<T> PreviousState => previousState;
        public bool IsTransitioning => isTransitioning;

        public StateMachine(T owner)
        {
            this.owner = owner;
        }

        public void ChangeState(IState<T> newState)
        {
            if (newState == null || isTransitioning) return;
            if (currentState == newState) return;

            isTransitioning = true;

            previousState = currentState;
            currentState?.OnExit(owner);
            currentState = newState;
            currentState?.OnEnter(owner);

            isTransitioning = false;
        }

        public void Update()
        {
            if (!isTransitioning && currentState != null)
            {
                currentState.Update(owner);
            }
        }

        public void RevertToPreviousState()
        {
            if (previousState != null && !isTransitioning)
            {
                ChangeState(previousState);
            }
        }

        public bool IsInState<TState>() where TState : class, IState<T>
        {
            return currentState is TState;
        }

        public TState GetCurrentState<TState>() where TState : class, IState<T>
        {
            return currentState as TState;
        }

        public void ForceState(IState<T> newState)
        {
            previousState = currentState;
            currentState?.OnExit(owner);
            currentState = newState;
            currentState?.OnEnter(owner);
        }
    }
}