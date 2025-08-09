using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class MeleeWeaponStateMachine
    {
        private StateMachine<MeleeWeapon> stateMachine;
        private MeleeWeapon owner;

        private float lastSwingTime = -1f;

        public MeleeWeaponStateMachine(MeleeWeapon weapon)
        {
            owner = weapon;
            stateMachine = new StateMachine<MeleeWeapon>(weapon);
        }

        public void Initialize()
        {
            if (owner.CanAttack())
                stateMachine.ChangeState(new MeleeReadyState());
            else
                stateMachine.ChangeState(new MeleeBrokenState());
        }

        public void Update()
        {
            CheckStateTransitions();
            stateMachine.Update();
        }

        public void HandleInput()
        {
            if (Time.time - lastSwingTime < owner.ScaledSwingTime)
                return;

            if (stateMachine.CurrentState is IMeleeInputHandler inputHandler)
            {
                inputHandler.HandleInput(owner);
            }
        }

        private void CheckStateTransitions()
        {
            if (owner.GetCurrentDurability() <= 0 && !IsInState<MeleeBrokenState>())
            {
                stateMachine.ChangeState(new MeleeBrokenState());
                return;
            }

            if (owner.GetCurrentDurability() > 0 && IsInState<MeleeBrokenState>())
            {
                stateMachine.ChangeState(new MeleeReadyState());
                return;
            }
        }

        public void ChangeState(IState<MeleeWeapon> newState)
        {
            stateMachine.ChangeState(newState);
        }

        public bool IsInState<T>() where T : class, IState<MeleeWeapon>
        {
            return stateMachine.IsInState<T>();
        }

        public T GetCurrentState<T>() where T : class, IState<MeleeWeapon>
        {
            return stateMachine.GetCurrentState<T>();
        }

        public void MarkSwingTime()
        {
            lastSwingTime = Time.time;
        }

        public bool CanSwingNow()
        {
            return Time.time - lastSwingTime >= owner.ScaledSwingTime;
        }
    }

    public interface IMeleeInputHandler
    {
        void HandleInput(MeleeWeapon weapon);
    }
}