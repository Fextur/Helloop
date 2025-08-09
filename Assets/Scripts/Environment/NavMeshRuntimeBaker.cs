using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using Helloop.Interactions;
using Helloop.Systems;

namespace Helloop.Environment
{
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        [Header("System Reference")]
        public RoomSystem roomSystem;

        [Header("Agent Type Surfaces")]
        public NavMeshSurface smallAgentSurface;
        public NavMeshSurface largeAgentSurface;

        [Header("Baking Settings")]
        public bool autoBakeOnStart = false;

        [Header("Exclusion Settings")]
        [Tooltip("Exclude objects with these tags from NavMesh")]
        public string[] excludedTags = { "Decoration" };

        private List<Renderer> disabledRenderers = new List<Renderer>();

        void OnEnable()
        {
            roomSystem.OnGenerationComplete?.Subscribe(BakeAllNavMeshes);
        }

        void OnDisable()
        {
            roomSystem.OnGenerationComplete?.Unsubscribe(BakeAllNavMeshes);
        }

        void Start()
        {
            SetupNavMeshSurfaces();

            if (autoBakeOnStart)
            {
                BakeAllNavMeshes();
            }
        }


        void SetupNavMeshSurfaces()
        {
            if (smallAgentSurface == null)
            {
                GameObject smallSurfaceObj = new GameObject("SmallAgentSurface");
                smallSurfaceObj.transform.SetParent(transform);
                smallAgentSurface = smallSurfaceObj.AddComponent<NavMeshSurface>();

                smallAgentSurface.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
                smallAgentSurface.collectObjects = CollectObjects.All;
                smallAgentSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            }

            if (largeAgentSurface == null)
            {
                GameObject largeSurfaceObj = new GameObject("LargeAgentSurface");
                largeSurfaceObj.transform.SetParent(transform);
                largeAgentSurface = largeSurfaceObj.AddComponent<NavMeshSurface>();

                largeAgentSurface.agentTypeID = NavMesh.GetSettingsByIndex(1).agentTypeID;
                largeAgentSurface.collectObjects = CollectObjects.All;
                largeAgentSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            }
        }

        void DisableExcludedObjects()
        {
            disabledRenderers.Clear();

            foreach (string excludedTag in excludedTags)
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(excludedTag);
                foreach (GameObject obj in taggedObjects)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                        disabledRenderers.Add(renderer);
                    }
                }
            }
        }

        void RestoreExcludedObjects()
        {
            foreach (Renderer renderer in disabledRenderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }
            disabledRenderers.Clear();
        }

        public void BakeAllNavMeshes()
        {
            DisableExcludedObjects();

            List<DoorController> allDoors = new List<DoorController>();
            List<bool> originalDoorStates = new List<bool>();

            DoorController[] doors = FindObjectsByType<DoorController>(FindObjectsSortMode.None);
            foreach (DoorController door in doors)
            {
                allDoors.Add(door);
                originalDoorStates.Add(door.isOpen);

                if (!door.isOpen)
                {
                    door.SetDoorState(true, true);
                }
            }

            if (smallAgentSurface != null)
            {
                try
                {
                    smallAgentSurface.BuildNavMesh();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"NavMesh baking warning (can be ignored): {e.Message}");
                }
            }

            if (largeAgentSurface != null)
            {
                try
                {
                    largeAgentSurface.BuildNavMesh();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"NavMesh baking warning (can be ignored): {e.Message}");
                }
            }
            for (int i = 0; i < allDoors.Count; i++)
            {
                if (allDoors[i] != null)
                {
                    allDoors[i].SetDoorState(originalDoorStates[i], true);
                }
            }

            RestoreExcludedObjects();
        }

        public void BakeNavMesh()
        {
            BakeAllNavMeshes();
        }
    }
}