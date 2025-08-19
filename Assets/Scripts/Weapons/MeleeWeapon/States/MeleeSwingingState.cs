namespace Helloop.Weapons.States
{
    /// <summary>
    /// Legacy marker interface for states that consumed input directly.
    /// Kept for compatibility with existing code (e.g., MeleeBrokenState).
    /// New routing is centralized in MeleeWeaponStateMachine.
    /// </summary>
    public interface IMeleeInputHandler
    {
        void HandleInput(MeleeWeapon weapon);
    }
}
