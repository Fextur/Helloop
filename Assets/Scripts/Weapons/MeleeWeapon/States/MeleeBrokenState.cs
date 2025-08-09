using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class MeleeBrokenState : IState<MeleeWeapon>, IMeleeInputHandler
    {
        private bool hasPlayedBreakSound;

        public void OnEnter(MeleeWeapon weapon)
        {
            hasPlayedBreakSound = false;

            weapon.SetWeaponVisibility(false);

            if (!hasPlayedBreakSound && weapon.Data.breakSound != null && weapon.audioSource != null)
            {
                weapon.audioSource.PlayOneShot(weapon.Data.breakSound);
                hasPlayedBreakSound = true;
            }
        }

        public void Update(MeleeWeapon weapon)
        {
        }

        public void OnExit(MeleeWeapon weapon)
        {
            weapon.SetWeaponVisibility(true);
            hasPlayedBreakSound = false;
        }

        public void HandleInput(MeleeWeapon weapon)
        {
        }
    }
}