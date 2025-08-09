using UnityEngine;

namespace Helloop.Systems
{
    [System.Serializable]
    public class WeaponLevelConfig
    {
        [Header("Scaling Settings")]
        public float damageMultiplierPerLevel = 1.2f;

        [Header("Ranged Weapon Scaling")]
        public float reloadTimeMultiplierPerLevel = 0.9f;
        public float minimumReloadTime = 0.3f;
        public float ammoIncreasePerLevel = 0.5f;

        [Header("Melee Weapon Scaling")]
        public float swingTimeMultiplierPerLevel = 0.95f;
        public float minimumSwingTime = 0.2f;
        public int durabilityIncreasePerLevel = 15;
    }

    [System.Serializable]
    public class EnemyLevelConfig
    {
        [Header("Enemy Scaling")]
        public float damageMultiplierPerLevel = 1.15f;
        public float speedMultiplierPerLevel = 1.1f;
        public float maximumSpeedLimit = 8f;
    }

    [CreateAssetMenu(fileName = "ScalingSystem", menuName = "Helloop/Systems/ScalingSystem")]
    public class ScalingSystem : ScriptableObject
    {
        [Header("Weapon Scaling Configuration")]
        public WeaponLevelConfig weaponConfig = new WeaponLevelConfig();

        [Header("Enemy Scaling Configuration")]
        public EnemyLevelConfig enemyConfig = new EnemyLevelConfig();

        public float GetScaledDamage(float baseDamage, int level)
        {
            return baseDamage * Mathf.Pow(weaponConfig.damageMultiplierPerLevel, level - 1);
        }

        public float GetScaledReloadTime(float baseReloadTime, int level)
        {
            float scaledTime = baseReloadTime * Mathf.Pow(weaponConfig.reloadTimeMultiplierPerLevel, level - 1);
            return Mathf.Max(scaledTime, weaponConfig.minimumReloadTime);
        }

        public int GetScaledMaxAmmo(int baseMaxAmmo, int clipSize, int level)
        {
            float ammoIncrease = clipSize * weaponConfig.ammoIncreasePerLevel * (level - 1);
            return baseMaxAmmo + Mathf.RoundToInt(ammoIncrease);
        }

        public float GetScaledSwingTime(float baseSwingTime, int level)
        {
            float scaledTime = baseSwingTime * Mathf.Pow(weaponConfig.swingTimeMultiplierPerLevel, level - 1);
            return Mathf.Max(scaledTime, weaponConfig.minimumSwingTime);
        }

        public int GetScaledDurability(int baseDurability, int level)
        {
            return baseDurability + (weaponConfig.durabilityIncreasePerLevel * (level - 1));
        }

        public float GetScaledEnemyDamage(float baseDamage, int level)
        {
            return baseDamage * Mathf.Pow(enemyConfig.damageMultiplierPerLevel, level - 1);
        }

        public float GetScaledEnemySpeed(float baseSpeed, int level)
        {
            float scaledSpeed = baseSpeed * Mathf.Pow(enemyConfig.speedMultiplierPerLevel, level - 1);
            return Mathf.Min(scaledSpeed, enemyConfig.maximumSpeedLimit);
        }

        public string GetLevelDisplayText(int level)
        {
            return $"Level {level}";
        }

        public Color GetLevelColor(int level)
        {
            if (level == 1) return Color.white;
            if (level <= 3) return Color.green;
            if (level <= 6) return Color.blue;
            if (level <= 9) return Color.magenta;
            return Color.red;
        }
    }
}