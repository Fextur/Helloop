using UnityEngine;
using Helloop.Systems;

namespace Helloop.Rooms
{
    public class EntryRoom : RoomController
    {
        [Header("Entry Room - Player Spawn")]
        public Transform playerSpawnPoint;

        [Header("System Reference")]
        public PlayerSystem playerSystem;


        protected override void InitializeRoom()
        {
            base.InitializeRoom();
            OnRoomCleared();
        }

        public GameObject SpawnPlayer()
        {
            if (playerSpawnPoint == null)
            {
                Debug.LogWarning($"EntryRoom {name} has no playerSpawnPoint assigned!");
                return null;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (player.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false;

                player.transform.position = playerSpawnPoint.position;
                player.transform.rotation = playerSpawnPoint.rotation;

                if (cc != null) cc.enabled = true;

                if (playerSystem != null)
                {
                    playerSystem.SetCheckpoint(playerSpawnPoint.position, playerSpawnPoint.rotation);
                }
            }
            return player;
        }

        public override string GetRoomType() => "Entry Room";
    }
}