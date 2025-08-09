using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Helloop.Data;
using Helloop.Generation.Data;
using Helloop.Interactions;
using Helloop.Generation.Algorithms;
using Helloop.Rooms;

namespace Helloop.Generation
{
    public class AlgorithmDoorManager : MonoBehaviour
    {
        [Header("Door Hole Detection")]
        public string doorHoleTag = "DoorHole";
        public string doorChildName = "Door";
        public string blockChildName = "Block";

        private const float CellHalf = 10f;
        private const float SnapMargin = 2f;

        private Dictionary<RoomNode, List<DoorHole>> roomDoorHoles = new Dictionary<RoomNode, List<DoorHole>>();
        private List<GameObject> activeDoors = new List<GameObject>();
        private class DoorHole
        {
            public Transform transform;
            public Vector3 worldPosition;
            public GameObject doorObject;
            public GameObject blockObject;

            public Direction direction;
            public int doorIndex;
            public bool isActive;
        }
        public void ApplyAlgorithmDoorStates(RoomGraph roomGraph)
        {
            foreach (var roomNode in roomGraph.nodes)
            {
                SetupRoomFromAlgorithm(roomNode);
            }

            PairAndResolveDoors(roomGraph);

        }

        private void SetupRoomFromAlgorithm(RoomNode roomNode)
        {
            if (roomNode.worldObject == null)
            {
                Debug.LogError($"❌ Room has no world object: {GetRoomIdentifier(roomNode)}");
                return;
            }

            List<DoorHole> doorHoles = FindDoorHolesInRoom(roomNode);
            roomDoorHoles[roomNode] = doorHoles;

            var algorithmDoorStates = roomNode.GetAllDoorStates();

            foreach (var doorHole in doorHoles)
            {
                var algorithmState = FindMatchingAlgorithmState(doorHole, algorithmDoorStates, roomNode);

                if (algorithmState != null && algorithmState.shouldBeOpen)
                {
                    SetDoorHoleActive(doorHole, algorithmState, roomNode);
                }
                else
                {
                    SetDoorHoleBlocked(doorHole, roomNode);
                }
            }
        }

        private DoorState FindMatchingAlgorithmState(DoorHole doorHole, List<DoorState> algorithmStates, RoomNode roomNode)
        {
            var expectedIndexResult = UnifiedDoorIndexCalculator.CalculateForDoorHole(
                roomNode, doorHole.worldPosition, doorHole.direction);

            if (!expectedIndexResult.isValid)
            {
                Debug.LogError($"❌ Invalid door index calculation for door hole: {expectedIndexResult.debugInfo}");
                return null;
            }

            var matchingStates = algorithmStates.Where(ads =>
                ads.direction == doorHole.direction &&
                ads.doorIndex == expectedIndexResult.doorIndex).ToList();

            if (matchingStates.Count == 0)
            {
                return null;
            }


            return matchingStates.First();
        }

        private List<DoorHole> FindDoorHolesInRoom(RoomNode roomNode)
        {
            var doorHoles = new List<DoorHole>();

            if (roomNode.worldObject == null)
            {
                Debug.LogError($"❌ Room world object is null: {GetRoomIdentifier(roomNode)}");
                return doorHoles;
            }

            Transform[] allChildren = roomNode.worldObject.GetComponentsInChildren<Transform>();

            foreach (Transform child in allChildren)
            {
                if (child.CompareTag(doorHoleTag))
                {
                    DoorHole doorHole = CreateDoorHoleFromTransform(child, roomNode);
                    if (doorHole != null)
                    {
                        doorHoles.Add(doorHole);
                    }
                }
            }

            doorHoles.Sort((a, b) =>
            {
                int dirComparison = a.direction.CompareTo(b.direction);
                if (dirComparison != 0) return dirComparison;
                return a.doorIndex.CompareTo(b.doorIndex);
            });

            return doorHoles;
        }

        private DoorHole CreateDoorHoleFromTransform(Transform doorHoleTransform, RoomNode roomNode)
        {
            Transform doorChild = doorHoleTransform.Find(doorChildName);
            Transform blockChild = doorHoleTransform.Find(blockChildName);

            if (doorChild == null || blockChild == null)
            {
                Debug.LogWarning($"⚠️ DoorHole missing children: {doorHoleTransform.name} (Door: {doorChild != null}, Block: {blockChild != null})");
                return null;
            }

            DoorHole doorHole = new DoorHole
            {
                transform = doorHoleTransform,
                worldPosition = doorHoleTransform.position,
                doorObject = doorChild.gameObject,
                blockObject = blockChild.gameObject
            };

            CalculateDoorHoleProperties(doorHole, roomNode);

            return doorHole;
        }

        private void CalculateDoorHoleProperties(DoorHole doorHole, RoomNode roomNode)
        {
            float halfCell = 10f;
            Vector3 roomOrigin = roomNode.worldObject.transform.position;
            Vector3 roomCenter = roomOrigin + new Vector3(
                (roomNode.gridSize.x - 1) * halfCell,
                0f,
                (roomNode.gridSize.y - 1) * halfCell
            );

            Vector3 local = doorHole.worldPosition - roomCenter;

            float halfX = roomNode.gridSize.x * halfCell;
            float halfZ = roomNode.gridSize.y * halfCell;

            float dEast = Mathf.Abs(local.x - halfX);
            float dWest = Mathf.Abs(local.x + halfX);
            float dNorth = Mathf.Abs(local.z - halfZ);
            float dSouth = Mathf.Abs(local.z + halfZ);

            Direction dir = Direction.East; float minD = dEast;
            if (dWest < minD) { minD = dWest; dir = Direction.West; }
            if (dNorth < minD) { minD = dNorth; dir = Direction.North; }
            if (dSouth < minD) { minD = dSouth; dir = Direction.South; }

            if (minD > SnapMargin)
            {
                dir = Mathf.Abs(local.x) >= Mathf.Abs(local.z)
                    ? (local.x >= 0 ? Direction.East : Direction.West)
                    : (local.z >= 0 ? Direction.North : Direction.South);
            }

            doorHole.direction = dir;

            var indexResult = UnifiedDoorIndexCalculator.CalculateForDoorHole_WithCenter(roomNode, roomCenter, doorHole.worldPosition, dir);
            doorHole.doorIndex = indexResult.isValid ? indexResult.doorIndex : 0;
        }

        private void SetDoorHoleActive(DoorHole doorHole, DoorState algorithmState, RoomNode roomNode)
        {
            if (doorHole.doorObject != null)
            {
                doorHole.doorObject.SetActive(true);
            }

            if (doorHole.blockObject != null)
                doorHole.blockObject.SetActive(false);

            doorHole.isActive = true;

            if (doorHole.doorObject != null)
                activeDoors.Add(doorHole.doorObject);
        }

        private void SetDoorHoleBlocked(DoorHole doorHole, RoomNode roomNode)
        {
            if (doorHole.doorObject != null)
                doorHole.doorObject.SetActive(false);

            if (doorHole.blockObject != null)
                doorHole.blockObject.SetActive(true);

            doorHole.isActive = false;
        }

        private DoorHole FindHole(RoomNode room, Direction dir, int index)
        {
            if (room == null) return null;
            if (!roomDoorHoles.TryGetValue(room, out var holes)) return null;
            return holes.FirstOrDefault(h => h.direction == dir && h.doorIndex == index);
        }

        private RoomController GetRoomController(RoomNode node)
        {
            if (node?.worldObject == null) return null;
            var rc = node.worldObject.GetComponent<RoomController>();
            if (rc == null) rc = node.worldObject.GetComponentInChildren<RoomController>(true);
            return rc;
        }

        private void AddDoorTo(RoomNode node, DoorController ctrl)
        {
            if (node == null || ctrl == null) return;
            var rc = GetRoomController(node);
            if (rc == null) return;
            if (!rc.connectedDoors.Contains(ctrl))
                rc.connectedDoors.Add(ctrl);
        }

        private void MakePassThrough(DoorHole hole)
        {
            if (hole == null) return;

            if (hole.doorObject != null) hole.doorObject.SetActive(false);
            if (hole.blockObject != null) hole.blockObject.SetActive(false);

            if (hole.isActive && hole.doorObject != null)
                activeDoors.Remove(hole.doorObject);

            hole.isActive = false;
        }

        private void MakeOwnerDoor(DoorHole hole)
        {
            if (hole == null) return;

            if (hole.doorObject != null) hole.doorObject.SetActive(true);
            if (hole.blockObject != null) hole.blockObject.SetActive(false);

            if (!hole.isActive && hole.doorObject != null)
                activeDoors.Add(hole.doorObject);

            hole.isActive = true;
        }

        private void PairAndResolveDoors(RoomGraph graph)
        {
            if (graph?.edges == null) return;

            foreach (var edge in graph.edges)
            {
                var a = edge?.fromRoom;
                var b = edge?.toRoom;
                if (a == null || b == null) continue;

                var aState = a.GetAllDoorStates().FirstOrDefault(d => d.shouldBeOpen && d.connectedRoom == b);
                var bState = b.GetAllDoorStates().FirstOrDefault(d => d.shouldBeOpen && d.connectedRoom == a);
                if (aState == null || bState == null) continue;

                var aHole = FindHole(a, aState.direction, aState.doorIndex);
                var bHole = FindHole(b, bState.direction, bState.doorIndex);
                if (aHole == null || bHole == null) continue;

                bool aOwns =
                    a.gridPosition.x > b.gridPosition.x ||
                    (a.gridPosition.x == b.gridPosition.x && a.gridPosition.y > b.gridPosition.y);

                var owner = aOwns ? aHole : bHole;
                var pass = aOwns ? bHole : aHole;

                MakeOwnerDoor(owner);
                MakePassThrough(pass);

                var ctrl = owner.doorObject ? owner.doorObject.GetComponent<DoorController>() : null;
                if (ctrl == null && owner.doorObject) ctrl = owner.doorObject.GetComponentInChildren<DoorController>(true);
                if (ctrl != null)
                {
                    AddDoorTo(a, ctrl);
                    AddDoorTo(b, ctrl);
                }
            }
        }


        private string GetRoomIdentifier(RoomNode room)
        {
            if (room == null) return "NULL";

            string type = room.isEntry ? "ENTRY" : room.isBoss ? "BOSS" : "REG";
            string size = GetRoomTypeString(room.roomType);
            return $"{type}-{size}@({room.gridPosition.x},{room.gridPosition.y})";
        }

        private string GetRoomTypeString(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Regular: return "1x1";
                case RoomType.Wide: return "2x1";
                case RoomType.Tall: return "1x2";
                case RoomType.Large: return "2x2";
                default: return "???";
            }
        }
    }
}
