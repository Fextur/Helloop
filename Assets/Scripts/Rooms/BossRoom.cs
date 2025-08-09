using UnityEngine;
using System.Collections.Generic;
using Helloop.Systems;
using Helloop.Rewards;

namespace Helloop.Rooms
{
    public class BossRoom : RoomController
    {
        [Header("Boss Room - Rewards")]
        public List<Transform> bossRewardSpawnPoints = new List<Transform>();

        [Header("Boss Room - Portal")]
        public GameObject portalPrefab;
        public Transform portalSpawnPoint;

        [Header("Y Position Override")]
        public float constantPortalYPosition = 0.1f;
        public bool useConstantPortalYPosition = true;

        [Header("System Reference")]
        public RewardSystem rewardSystem;

        protected override void InitializeRoom()
        {
            base.InitializeRoom();
        }

        protected override void OnRoomActivated()
        {
            base.OnRoomActivated();
            LockDoors();
        }

        protected override void OnRoomCleared()
        {
            base.OnRoomCleared();
            SpawnBossRewards();
            SpawnPortal();
        }

        void SpawnBossRewards()
        {
            if (rewardSystem == null || bossRewardSpawnPoints.Count == 0) return;

            foreach (Transform spawnPoint in bossRewardSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    SpawnReward(rewardSystem.CalculateRandomReward(spawnPoint.position));
                }
            }
        }

        void SpawnReward(RewardSpawnData spawnData)
        {
            if (spawnData == null) return;

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

        void SpawnPortal()
        {
            if (portalPrefab == null || portalSpawnPoint == null) return;

            Vector3 spawnPosition = useConstantPortalYPosition ?
                new Vector3(portalSpawnPoint.position.x, constantPortalYPosition, portalSpawnPoint.position.z) :
                portalSpawnPoint.position;

            Instantiate(portalPrefab, spawnPosition, portalSpawnPoint.rotation);
        }

        public override string GetRoomType() => "Boss Room";
    }
}