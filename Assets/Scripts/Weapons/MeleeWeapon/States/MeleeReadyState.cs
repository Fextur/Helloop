using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    /// <summary>
    /// Idle/ready for melee. Input routing lives in MeleeWeaponStateMachine.Update().
    /// This state intentionally does not read input or decide attacks.
    /// </summary>
    public class MeleeReadyState : IState<MeleeWeapon>
    {
        public void OnEnter(MeleeWeapon weapon) { }
        public void Update(MeleeWeapon weapon) { }
        public void OnExit(MeleeWeapon weapon) { }
    }
}
