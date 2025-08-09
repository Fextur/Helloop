using UnityEngine;

namespace Helloop.Weapons
{
    public interface IMeleeWeapon : IWeapon
    {
        MeleeWeaponData Data { get; }
        Camera PlayerCamera { get; }
    }
}