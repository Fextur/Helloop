using UnityEngine;
using Helloop.Data;
using Helloop.Systems;
using Helloop.Rewards;

namespace Helloop.Rooms
{
    public class RegularRoom : RoomController
    {
        [Header("Regular Room - Rewards")]
        public Transform rewardSpawnPoint;
        public PowerUpData specificPowerUp;
        public WeaponData specificWeapon;

        [Header("System Reference")]
        public RewardSystem rewardSystem;

        protected override void InitializeRoom()
        {
            base.InitializeRoom();
        }

        protected override void OnRoomActivated()
        {
            base.OnRoomActivated();

            if (spawnedEnemies.Count > 0)
            {
                LockDoors();
            }
        }

        protected override void OnRoomCleared()
        {
            base.OnRoomCleared();
            SpawnReward();
        }

        void SpawnReward()
        {
            if (rewardSystem == null || rewardSpawnPoint == null) return;

            RewardSpawnData spawnData = null;

            if (specificWeapon != null)
            {
                spawnData = rewardSystem.CalculateSpecificWeaponReward(specificWeapon, rewardSpawnPoint.position);
            }
            else if (specificPowerUp != null)
            {
                spawnData = rewardSystem.CalculateSpecificPowerUpReward(specificPowerUp, rewardSpawnPoint.position);
            }
            else
            {
                spawnData = rewardSystem.CalculateRandomReward(rewardSpawnPoint.position);
            }

            if (spawnData != null)
            {
                GameObject rewardObj = Instantiate(spawnData.rewardPrefab, spawnData.spawnPosition, Quaternion.identity);

                if (spawnData.rewardType == RewardType.PowerUp && rewardObj.TryGetComponent<PowerUpPickup>(out var powerUp))
                {
                    powerUp.SetPowerUpData(spawnData.powerUpData);
                }
                else if (spawnData.rewardType == RewardType.Weapon && rewardObj.TryGetComponent<WeaponPickup>(out var weapon))
                {
                    weapon.SetWeaponData(spawnData.weaponData, spawnData.weaponLevel);
                }

                rewardSystem.OnRewardSpawned?.Raise();
            }
        }

        public override string GetRoomType() => "Regular Room";
    }
}