using UnityEngine;

namespace Helloop.Data
{
    public enum FireMode
    {
        SemiAutomatic,
        Automatic,
        Burst
    }

    [CreateAssetMenu(fileName = "NewRangedWeapon", menuName = "Helloop/Weapons/RangedWeapon")]
    public class RangedWeaponData : WeaponData
    {
        [Header("Ranged Stats")]
        public GameObject projectilePrefab;
        public float reloadTime;
        public float projectileSpeed;
        public float falloffDistance;

        public int maxAmmoSize;
        public int clipSize;

        [Header("Fire Settings")]
        public FireMode fireMode;
        public float fireRate = 0.1f;
        public int burstCount = 3;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip reloadSound;
        public AudioClip emptyClipSound;
    }
}