using System;

namespace Helloop.StateMachines
{
    [System.Serializable]
    public class StateTransition<T> where T : class
    {
        public Type fromState;
        public Type toState;
        public Func<T, bool> condition;
        public string transitionName;

        public StateTransition(Type from, Type to, Func<T, bool> transitionCondition, string name = "")
        {
            fromState = from;
            toState = to;
            condition = transitionCondition;
            transitionName = string.IsNullOrEmpty(name) ? $"{from.Name} -> {to.Name}" : name;
        }

        public bool CanTransition(T owner, IState<T> currentState)
        {
            if (currentState?.GetType() != fromState) return false;
            return condition?.Invoke(owner) ?? false;
        }
    }

    public class StateTransitionValidator<T> where T : class
    {
        private readonly System.Collections.Generic.List<StateTransition<T>> transitions;

        public StateTransitionValidator()
        {
            transitions = new System.Collections.Generic.List<StateTransition<T>>();
        }

        public void AddTransition(StateTransition<T> transition)
        {
            transitions.Add(transition);
        }

        public IState<T> GetValidTransition(T owner, IState<T> currentState)
        {
            foreach (var transition in transitions)
            {
                if (transition.CanTransition(owner, currentState))
                {
                    return (IState<T>)Activator.CreateInstance(transition.toState);
                }
            }
            return null;
        }

        public bool IsValidTransition(T owner, IState<T> currentState, Type targetStateType)
        {
            foreach (var transition in transitions)
            {
                if (transition.fromState == currentState?.GetType() &&
                    transition.toState == targetStateType &&
                    transition.CanTransition(owner, currentState))
                {
                    return true;
                }
            }
            return false;
        }
    }
}