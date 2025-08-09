using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class RangedReadyState : IState<RangedWeapon>, IRangedInputHandler
    {
        public void OnEnter(RangedWeapon weapon)
        {
            weapon.transform.localPosition = weapon.originalPosition;
            weapon.transform.localRotation = weapon.originalRotation;
        }

        public void Update(RangedWeapon weapon)
        {
        }

        public void OnExit(RangedWeapon weapon)
        {
        }

        public void HandleFireInput(RangedWeapon weapon)
        {
            if (!weapon.CanFire())
            {
                if (weapon.CanReload())
                {
                    weapon.GetStateMachine().ChangeState(new RangedReloadingState());
                }
                else
                {
                    if (weapon.Data.emptyClipSound != null && weapon.audioSource != null)
                    {
                        weapon.audioSource.PlayOneShot(weapon.Data.emptyClipSound);
                    }
                    weapon.GetStateMachine().ChangeState(new RangedEmptyState());
                }
                return;
            }

            weapon.GetStateMachine().ChangeState(new RangedFiringState());
        }

        public void HandleStopFireInput(RangedWeapon weapon)
        {
        }
    }
}