using UnityEngine;
using System.Collections;
using System.Linq;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Environment;
using Helloop.Rewards;
using Helloop.Rooms;

namespace Helloop.Generation
{
    [RequireComponent(typeof(NavMeshRuntimeBaker))]
    public class ProceduralLevelGenerator : MonoBehaviour
    {
        [Header("Scene References")]
        public Transform roomsParent;

        [Header("Player Spawning (Fallback)")]
        public GameObject fallbackPlayerPrefab;

        [Header("Generation Settings")]
        public bool generateOnStart = true;

        [Header("System References")]
        public ProgressionSystem progressionSystem;
        public RoomSystem roomSystem;

        private MazeGenerator mazeGenerator;
        private CircleData currentCircleData;
        private bool generationStarted = false;

        void Awake()
        {
            mazeGenerator = gameObject.GetComponent<MazeGenerator>();
            if (mazeGenerator == null)
            {
                mazeGenerator = gameObject.AddComponent<MazeGenerator>();
            }
        }

        void Start()
        {
            if (generateOnStart)
            {
                StartCoroutine(GenerateLevelCoroutine());
            }
        }

        void OnEnable()
        {
            if (roomSystem != null)
            {
                roomSystem.OnEntryRoomRegistered?.Subscribe(OnEntryRoomReady);
            }
        }

        void OnDisable()
        {
            if (roomSystem != null)
            {
                roomSystem.OnEntryRoomRegistered?.Unsubscribe(OnEntryRoomReady);
            }
        }

        public IEnumerator GenerateLevelCoroutine()
        {
            currentCircleData = progressionSystem.GetCurrentCircle();

            roomSystem.StartGeneration();
            generationStarted = true;

            yield return StartCoroutine(GenerateMazeLayout());
        }

        void OnEntryRoomReady()
        {
            if (!generationStarted) return;

            SpawnPlayer();
            roomSystem.CompleteGeneration();
            generationStarted = false;

        }

        IEnumerator GenerateMazeLayout()
        {
            mazeGenerator.GenerateMaze(currentCircleData, roomsParent);
            yield return null;
        }

        void SpawnPlayer()
        {
            if (roomSystem.EntryRoom != null)
            {
                GameObject spawnedPlayer = roomSystem.EntryRoom.SpawnPlayer();
                if (spawnedPlayer != null)
                {
                    return;
                }
            }

            Debug.LogWarning("EntryRoom spawn failed, using fallback");
            var anyRoom = roomSystem.AllRooms.FirstOrDefault();
            if (anyRoom != null && fallbackPlayerPrefab != null)
            {
                GameObject player = Instantiate(fallbackPlayerPrefab, anyRoom.transform.position + Vector3.up, Quaternion.identity);
            }
        }

    }
}