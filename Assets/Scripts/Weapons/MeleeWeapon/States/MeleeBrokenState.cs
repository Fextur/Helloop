using UnityEngine;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    /// <summary>
    /// Broken melee state. Disables visuals, plays break sound once, then idles.
    /// No input handling — routing is centralized in MeleeWeaponStateMachine.
    /// </summary>
    public class MeleeBrokenState : IState<MeleeWeapon>
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

        public void Update(MeleeWeapon weapon) { }

        public void OnExit(MeleeWeapon weapon)
        {
            weapon.SetWeaponVisibility(true);
            hasPlayedBreakSound = false;
        }
    }
}
