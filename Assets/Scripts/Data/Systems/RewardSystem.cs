using UnityEngine;
using System.Collections.Generic;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Events;

namespace Helloop.Systems
{
    [CreateAssetMenu(fileName = "RewardSystem", menuName = "Helloop/Systems/RewardSystem")]
    public class RewardSystem : ScriptableObject
    {
        [Header("Available Rewards")]
        public List<PowerUpData> availablePowerUps = new List<PowerUpData>();
        public List<WeaponData> availableWeapons = new List<WeaponData>();

        [Header("Weapon Reward Prefabs")]
        public GameObject weaponRewardPrefab;

        [Header("Reward Probabilities")]
        [Range(0f, 1f)]
        public float weaponSpawnChance = 0.6f;

        [Header("Weapon Level Variance")]
        [Range(0f, 1f)]
        public float levelVarianceStrength = 0.3f;
        public int maxLevelVariance = 2;

        [Header("Y Position Override")]
        public float constantRewardYPosition = 0.5f;
        public bool useConstantYPosition = true;

        [Header("System References")]
        public ProgressionSystem progressionSystem;

        [Header("Events")]
        public GameEvent OnRewardSpawned;

        public RewardSpawnData CalculateRandomReward(Vector3 position)
        {
            bool spawnWeapon = Random.Range(0f, 1f) < weaponSpawnChance;

            if (spawnWeapon && availableWeapons.Count > 0)
            {
                WeaponData randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
                return CreateWeaponReward(randomWeapon, position);
            }
            else if (availablePowerUps.Count > 0)
            {
                PowerUpData randomPowerUp = availablePowerUps[Random.Range(0, availablePowerUps.Count)];
                return CreatePowerUpReward(randomPowerUp, position);
            }

            return null;
        }

        public RewardSpawnData CalculateSpecificWeaponReward(WeaponData weaponData, Vector3 position)
        {
            return weaponData != null ? CreateWeaponReward(weaponData, position) : null;
        }

        public RewardSpawnData CalculateSpecificPowerUpReward(PowerUpData powerUpData, Vector3 position)
        {
            return powerUpData != null ? CreatePowerUpReward(powerUpData, position) : null;
        }

        RewardSpawnData CreateWeaponReward(WeaponData weaponData, Vector3 position)
        {
            if (weaponRewardPrefab == null) return null;

            return new RewardSpawnData
            {
                rewardType = RewardType.Weapon,
                weaponData = weaponData,
                weaponLevel = CalculateWeaponLevel(),
                spawnPosition = GetAdjustedPosition(position),
                rewardPrefab = weaponRewardPrefab
            };
        }

        RewardSpawnData CreatePowerUpReward(PowerUpData powerUpData, Vector3 position)
        {
            if (powerUpData.rewardPrefab == null) return null;

            return new RewardSpawnData
            {
                rewardType = RewardType.PowerUp,
                powerUpData = powerUpData,
                spawnPosition = GetAdjustedPosition(position),
                rewardPrefab = powerUpData.rewardPrefab
            };
        }

        int CalculateWeaponLevel()
        {
            int baseLevel = progressionSystem != null ? progressionSystem.GetCurrentCircleNumber() : 1;
            float variance = GenerateParabolaRandom() * levelVarianceStrength;
            int levelChange = Mathf.RoundToInt(variance * maxLevelVariance);
            return Mathf.Max(1, baseLevel + levelChange);
        }

        float GenerateParabolaRandom()
        {
            float rand1 = Random.Range(-1f, 1f);
            float rand2 = Random.Range(-1f, 1f);
            return (rand1 + rand2) * 0.5f;
        }

        Vector3 GetAdjustedPosition(Vector3 position)
        {
            return useConstantYPosition ?
                new Vector3(position.x, constantRewardYPosition, position.z) :
                position;
        }
    }

    [System.Serializable]
    public class RewardSpawnData
    {
        public RewardType rewardType;
        public PowerUpData powerUpData;
        public WeaponData weaponData;
        public int weaponLevel;
        public Vector3 spawnPosition;
        public GameObject rewardPrefab;
    }

    public enum RewardType
    {
        PowerUp,
        Weapon
    }
}