using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helloop.Data;
using Helloop.Generation.Data;
using Helloop.Generation.Services;
using Helloop.Generation.Algorithms;

namespace Helloop.Generation
{
    public class MazeGenerator : MonoBehaviour
    {
        [Header("Services")]
        private IRoomPlacementService roomPlacer;
        private IRoomGraphBuilder graphBuilder;
        private IConnectivityGenerator connectivityGen;
        private ILoopInjectionService loopInjector;

        [Header("Door Management")]
        private AlgorithmDoorManager algorithmDoorManager;

        public RoomGraph CurrentRoomGraph { get; private set; }
        public RoomLayout CurrentRoomLayout { get; private set; }

        private void Awake()
        {
            InitializeServices();
            InitializeDoorManager();
        }

        private void InitializeServices()
        {
            roomPlacer = new GridBasedRoomPlacementService();
            graphBuilder = new GeometricRoomGraphBuilder();
            connectivityGen = new GrowingTreeConnectivityGenerator();
            loopInjector = new ModifiedKruskalsLoopInjectionService();
        }

        private void InitializeDoorManager()
        {
            algorithmDoorManager = GetComponent<AlgorithmDoorManager>();
            if (algorithmDoorManager == null)
            {
                algorithmDoorManager = gameObject.AddComponent<AlgorithmDoorManager>();
            }
        }

        public void GenerateMaze(CircleData circleData, Transform roomParent)
        {
            try
            {
                var context = CreateGenerationContext(circleData, roomParent);

                ExecuteGenerationPipeline(context);

                SetupDoors();

            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Maze generation failed: {e.Message}");
                Debug.LogException(e);
            }
        }

        private MazeGenerationContext CreateGenerationContext(CircleData circleData, Transform roomParent)
        {
            return new MazeGenerationContext
            {
                circleData = circleData,
                roomParent = roomParent,
                complexityMultiplier = circleData.GetComplexityMultiplier(),
                circleLevel = circleData.circleLevel
            };
        }

        private void ExecuteGenerationPipeline(MazeGenerationContext context)
        {
            CurrentRoomLayout = roomPlacer.PlaceRooms(context);
            CurrentRoomGraph = graphBuilder.BuildGraph(CurrentRoomLayout);
            connectivityGen.GenerateConnectivity(CurrentRoomGraph, context);
            loopInjector.AddLoops(CurrentRoomGraph, context.complexityMultiplier);
        }

        private void SetupDoors()
        {
            if (algorithmDoorManager != null && CurrentRoomGraph != null)
            {
                algorithmDoorManager.ApplyAlgorithmDoorStates(CurrentRoomGraph);
            }
            else
            {
                Debug.LogError("❌ Cannot setup doors - missing DoorManager or RoomGraph");
            }
        }
    }
}