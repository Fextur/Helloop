using UnityEngine;
using System.Collections;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class RangedWeaponStateMachine
    {
        private StateMachine<RangedWeapon> stateMachine;
        private RangedWeapon owner;

        public RangedWeaponStateMachine(RangedWeapon weapon)
        {
            owner = weapon;
            stateMachine = new StateMachine<RangedWeapon>(weapon);
        }

        public void Initialize()
        {
            if (owner.CurrentClip > 0)
                stateMachine.ChangeState(new RangedReadyState());
            else if (owner.CurrentAmmo > 0)
                stateMachine.ChangeState(new RangedEmptyState());
            else
                stateMachine.ChangeState(new RangedEmptyState());
        }

        public void Update()
        {
            CheckStateTransitions();
            stateMachine.Update();
        }

        private void CheckStateTransitions()
        {
            if (owner.CurrentClip <= 0)
            {
                if (!IsInState<RangedReloadingState>() && !IsInState<RangedEmptyState>())
                {
                    if (owner.CanReload())
                    {
                        stateMachine.ChangeState(new RangedReloadingState());
                    }
                    else
                    {
                        stateMachine.ChangeState(new RangedEmptyState());
                    }
                    return;
                }
            }

            if (owner.CurrentClip > 0 && IsInState<RangedEmptyState>())
            {
                stateMachine.ChangeState(new RangedReadyState());
                return;
            }
        }

        public void HandleFireInput()
        {
            if (stateMachine.CurrentState is IRangedInputHandler inputHandler)
            {
                inputHandler.HandleFireInput(owner);
            }
        }

        public void HandleStopFireInput()
        {
            if (stateMachine.CurrentState is IRangedInputHandler inputHandler)
            {
                inputHandler.HandleStopFireInput(owner);
            }
        }

        public IEnumerator TriggerReload()
        {
            if (!owner.CanReload() || IsInState<RangedReloadingState>())
                yield break;

            stateMachine.ChangeState(new RangedReloadingState());

            while (IsInState<RangedReloadingState>())
            {
                yield return null;
            }
        }

        public void ChangeState(IState<RangedWeapon> newState)
        {
            stateMachine.ChangeState(newState);
        }

        public bool IsInState<T>() where T : class, IState<RangedWeapon>
        {
            return stateMachine.IsInState<T>();
        }

        public T GetCurrentState<T>() where T : class, IState<RangedWeapon>
        {
            return stateMachine.GetCurrentState<T>();
        }
    }

    public interface IRangedInputHandler
    {
        void HandleFireInput(RangedWeapon weapon);
        void HandleStopFireInput(RangedWeapon weapon);
    }
}