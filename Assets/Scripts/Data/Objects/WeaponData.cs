using UnityEngine;
namespace Helloop.Data
{
    public enum WeaponType { Melee, Ranged }

    public abstract class WeaponData : ScriptableObject
    {
        public string weaponName;
        public GameObject weaponPrefab;

        public float damage;
    }
}