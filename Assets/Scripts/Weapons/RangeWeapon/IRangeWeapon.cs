using Helloop.Data;

namespace Helloop.Weapons
{
    public interface IRangeWeapon : IWeapon
    {
        RangedWeaponData Data { get; }
        public System.Collections.IEnumerator Reload();

        public void StopUse();
    }
}
