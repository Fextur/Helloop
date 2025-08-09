using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class MeleeReadyState : IState<MeleeWeapon>, IMeleeInputHandler
    {
        public void OnEnter(MeleeWeapon weapon)
        {
            weapon.transform.localPosition = weapon.originalPosition;
            weapon.transform.localRotation = weapon.originalRotation;

            weapon.SetWeaponVisibility(true);
        }

        public void Update(MeleeWeapon weapon)
        {
        }

        public void OnExit(MeleeWeapon weapon)
        {
        }

        public void HandleInput(MeleeWeapon weapon)
        {
            if (!weapon.CanAttack())
                return;

            if (!weapon.GetStateMachine().CanSwingNow())
                return;

            weapon.GetStateMachine().MarkSwingTime();
            weapon.GetStateMachine().ChangeState(new MeleeSwingingState());
        }
    }
}