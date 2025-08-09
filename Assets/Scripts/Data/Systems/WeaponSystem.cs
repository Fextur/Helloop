using UnityEngine;
using Helloop.Systems;
using Helloop.Events;
using Helloop.Data;
using Helloop.Weapons;

[CreateAssetMenu(fileName = "WeaponSystem", menuName = "Helloop/Systems/WeaponSystem")]
public class WeaponSystem : ScriptableObject
{
    [Header("Current Equipment")]
    public WeaponData currentLeftWeapon;
    public WeaponData currentRightWeapon;
    public int leftWeaponLevel = 1;
    public int rightWeaponLevel = 1;

    [Header("Current State")]
    public int currentAmmo;
    public int currentClipAmmo;
    public int currentDurability;
    public int maxDurability;

    [Header("Events")]
    public GameEvent OnWeaponEquipped;
    public GameEvent OnAmmoChanged;
    public GameEvent OnDurabilityChanged;
    public GameEvent OnWeaponLevelChanged;

    [Header("System References")]
    public ScalingSystem scalingSystem;

    public void EquipLeftWeapon(WeaponData weaponData, int level)
    {
        currentLeftWeapon = weaponData;
        leftWeaponLevel = level;

        if (weaponData is RangedWeaponData rangedData)
        {
            int scaledMaxAmmo = scalingSystem.GetScaledMaxAmmo(rangedData.maxAmmoSize, rangedData.clipSize, level);
            currentAmmo = scaledMaxAmmo;
            currentClipAmmo = rangedData.clipSize;
        }

        OnWeaponEquipped?.Raise();
    }

    public void EquipRightWeapon(WeaponData weaponData, int level)
    {
        currentRightWeapon = weaponData;
        rightWeaponLevel = level;

        if (weaponData is MeleeWeaponData meleeData)
        {
            int scaledDurability = scalingSystem.GetScaledDurability(meleeData.durability, level);
            currentDurability = scaledDurability;
            maxDurability = scaledDurability;
        }

        OnWeaponEquipped?.Raise();
    }

    public void EquipWeapon(WeaponData weaponData, int level = 1)
    {
        if (weaponData is RangedWeaponData)
        {
            EquipLeftWeapon(weaponData, level);
        }
        else if (weaponData is MeleeWeaponData)
        {
            EquipRightWeapon(weaponData, level);
        }
    }

    public void UseAmmo(int amount)
    {
        currentClipAmmo = Mathf.Max(0, currentClipAmmo - amount);
        OnAmmoChanged?.Raise();
    }

    public void UseDurability(int amount)
    {
        currentDurability = Mathf.Max(0, currentDurability - amount);
        OnDurabilityChanged?.Raise();
    }

    public void RefillAmmo()
    {
        if (currentLeftWeapon is RangedWeaponData rangedData)
        {
            int scaledMaxAmmo = scalingSystem.GetScaledMaxAmmo(rangedData.maxAmmoSize, rangedData.clipSize, leftWeaponLevel);
            currentAmmo = scaledMaxAmmo;
            currentClipAmmo = rangedData.clipSize;
            OnAmmoChanged?.Raise();
        }
    }

    public void RestoreDurability()
    {
        if (currentRightWeapon is MeleeWeaponData meleeData)
        {
            int scaledDurability = scalingSystem.GetScaledDurability(meleeData.durability, rightWeaponLevel);
            currentDurability = scaledDurability;
            maxDurability = scaledDurability;
            OnDurabilityChanged?.Raise();
        }
    }

    public void LevelUpLeftWeapon()
    {
        if (currentLeftWeapon != null)
        {
            leftWeaponLevel++;
            OnWeaponLevelChanged?.Raise();
        }
    }

    public void LevelUpRightWeapon()
    {
        if (currentRightWeapon != null)
        {
            rightWeaponLevel++;
            OnWeaponLevelChanged?.Raise();
        }
    }

    public bool HasRangedWeapon() => currentLeftWeapon is RangedWeaponData;
    public bool HasMeleeWeapon() => currentRightWeapon is MeleeWeaponData;

    public bool CanFireRanged() => HasRangedWeapon() && currentClipAmmo > 0;
    public bool CanUseMelee() => HasMeleeWeapon() && currentDurability > 0;

    public float GetDurabilityPercentage() => maxDurability > 0 ? (float)currentDurability / maxDurability : 0f;

    public void ResetToDefaults()
    {
        currentLeftWeapon = null;
        currentRightWeapon = null;
        leftWeaponLevel = 1;
        rightWeaponLevel = 1;
        currentAmmo = 0;
        currentClipAmmo = 0;
        currentDurability = 0;
        maxDurability = 0;
    }
}