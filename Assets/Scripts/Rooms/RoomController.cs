using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Enemies;
using Helloop.Interactions;

namespace Helloop.Rooms
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public Transform spawnPoint;
        public EnemyData enemyData;
        public List<Transform> patrolPoints = new List<Transform>();
    }

    public class RoomController : MonoBehaviour
    {
        [Header("System Reference")]
        public RoomSystem roomSystem;

        [Header("Enemy Spawning")]
        public List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

        [Header("Door References")]
        public List<DoorController> connectedDoors = new List<DoorController>();

        protected List<GameObject> spawnedEnemies = new List<GameObject>();
        protected bool hasBeenActivated = false;
        protected bool isCleared = false;

        private float lastActivationCheck = 0f;
        private float activationCheckInterval = 0.2f;

        protected virtual void Start()
        {
            RegisterWithSystem();
            InitializeRoom();
        }

        void RegisterWithSystem()
        {
            if (roomSystem != null)
            {
                roomSystem.RegisterRoom(this);
            }
        }

        protected virtual void Update()
        {
            if (!hasBeenActivated && Time.time - lastActivationCheck >= activationCheckInterval)
            {
                lastActivationCheck = Time.time;
                CheckForActivation();
            }
        }

        protected virtual void InitializeRoom()
        {
        }

        protected virtual void CheckForActivation()
        {
            foreach (var door in connectedDoors)
            {
                if (door != null && door.isOpen)
                {
                    hasBeenActivated = true;
                    OnRoomActivated();
                    break;
                }
            }
        }

        protected virtual void OnRoomActivated()
        {
            SpawnEnemies();
        }

        protected virtual void SpawnEnemies()
        {
            StartCoroutine(SpawnEnemiesStaggered());
        }

        IEnumerator SpawnEnemiesStaggered()
        {
            int enemiesSpawned = 0;

            foreach (var enemySpawn in enemySpawns)
            {
                if (enemySpawn.spawnPoint != null && enemySpawn.enemyData != null && enemySpawn.enemyData.enemyPrefab != null)
                {
                    GameObject enemy = Instantiate(enemySpawn.enemyData.enemyPrefab, enemySpawn.spawnPoint.position, enemySpawn.spawnPoint.rotation);
                    spawnedEnemies.Add(enemy);

                    if (enemy.TryGetComponent<Enemy>(out var enemyScript))
                    {
                        enemyScript.enemyData = enemySpawn.enemyData;
                        enemyScript.patrolPoints = enemySpawn.patrolPoints;
                        enemyScript.SetRoomController(this);
                    }

                    enemiesSpawned++;

                    if (enemiesSpawned % 2 == 0)
                    {
                        yield return null;
                    }
                }
            }
        }

        public virtual void NotifyEnemyDeath(GameObject enemy)
        {
            if (spawnedEnemies.Contains(enemy))
            {
                spawnedEnemies.Remove(enemy);

                if (spawnedEnemies.Count == 0 && !isCleared)
                {
                    OnRoomCleared();
                }
            }
        }

        protected virtual void OnRoomCleared()
        {
            isCleared = true;
            UnlockDoors();
        }

        protected virtual void UnlockDoors()
        {
            foreach (var door in connectedDoors)
            {
                if (door != null)
                {
                    door.UnlockDoor();
                }
            }
        }

        protected virtual void LockDoors()
        {
            foreach (var door in connectedDoors)
            {
                if (door != null && !door.isOpen)
                {
                    door.LockDoor();
                }
            }
        }

        void OnDestroy()
        {
            if (roomSystem != null)
            {
                roomSystem.UnregisterRoom(this);
            }
        }



        public bool IsCleared() => isCleared;
        public bool HasBeenActivated() => hasBeenActivated;
        public int GetEnemyCount() => spawnedEnemies.Count;
        public virtual string GetRoomType() => "Base Room";
    }
}