using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Helloop.Data;
using Helloop.Generation.Algorithms;

namespace Helloop.Generation.Data
{
    public class RoomNode
    {
        public Vector2 abstractPosition;
        public Vector2Int gridPosition;
        public Vector2Int gridSize;
        public RoomType roomType;
        public GameObject prefab;
        public GameObject worldObject;
        public bool isEntry;
        public bool isBoss;

        public Dictionary<Direction, List<DoorState>> doorStates = new Dictionary<Direction, List<DoorState>>();
        public List<RoomConnection> connections = new List<RoomConnection>();

        public RoomNode()
        {
            doorStates[Direction.North] = new List<DoorState>();
            doorStates[Direction.South] = new List<DoorState>();
            doorStates[Direction.East] = new List<DoorState>();
            doorStates[Direction.West] = new List<DoorState>();
        }


        public void SetDoorState(Direction direction, int doorIndex, bool shouldBeOpen, bool isMainPath = false, bool isLoop = false, RoomNode connectedRoom = null)
        {
            var exactDuplicate = doorStates[direction].Find(d =>
                d.doorIndex == doorIndex &&
                d.connectedRoom == connectedRoom &&
                d.shouldBeOpen == shouldBeOpen);

            if (exactDuplicate != null)
            {
                Debug.LogWarning($"⚠️ Exact duplicate door state ignored: {GetRoomIdentifier()} {direction}[{doorIndex}] → {GetRoomIdentifier(connectedRoom)}");
                return;
            }

            var conflictingDoor = doorStates[direction].Find(d =>
                d.doorIndex == doorIndex &&
                d.shouldBeOpen &&
                shouldBeOpen &&
                d.connectedRoom != connectedRoom);

            if (conflictingDoor != null)
            {
                Debug.LogError($"❌ DOOR CONFLICT: {GetRoomIdentifier()} {direction}[{doorIndex}] already connects to {GetRoomIdentifier(conflictingDoor.connectedRoom)}, cannot connect to {GetRoomIdentifier(connectedRoom)}");
                return;
            }

            var existingDoor = doorStates[direction].Find(d => d.doorIndex == doorIndex);
            if (existingDoor != null)
            {
                doorStates[direction].Remove(existingDoor);
                Debug.LogWarning($"⚠️ Replacing door state: {GetRoomIdentifier()} {direction}[{doorIndex}] (was: {GetRoomIdentifier(existingDoor.connectedRoom)} → now: {GetRoomIdentifier(connectedRoom)})");
            }

            var doorState = new DoorState
            {
                direction = direction,
                doorIndex = doorIndex,
                shouldBeOpen = shouldBeOpen,
                isMainPath = isMainPath,
                isLoop = isLoop,
                connectedRoom = connectedRoom
            };

            doorStates[direction].Add(doorState);

            string pathType = isMainPath ? "MAIN" : isLoop ? "LOOP" : "BRANCH";
        }

        private string GetRoomIdentifier()
        {
            string type = isEntry ? "ENTRY" : isBoss ? "BOSS" : "REG";
            return $"{type}-{roomType}@({gridPosition.x},{gridPosition.y})";
        }

        private static string GetRoomIdentifier(RoomNode room)
        {
            if (room == null) return "NULL";
            return room.GetRoomIdentifier();
        }


        public List<DoorState> GetAllDoorStates()
        {
            var allDoors = new List<DoorState>();
            foreach (var directionDoors in doorStates.Values)
            {
                allDoors.AddRange(directionDoors);
            }
            return allDoors;
        }


        public static void CreateBidirectionalConnection(RoomNode roomA, RoomNode roomB, bool isMainPath = false, bool isLoop = false)
        {

            Direction directionAtoB = CalculateDirection(roomA, roomB);
            Direction directionBtoA = GetOppositeDirection(directionAtoB);

            var doorIndexA = UnifiedDoorIndexCalculator.CalculateForGeneration(roomA, roomB, directionAtoB);
            var doorIndexB = UnifiedDoorIndexCalculator.CalculateForGeneration(roomB, roomA, directionBtoA);

            if (!doorIndexA.isValid || !doorIndexB.isValid)
            {

                return;
            }

            var existingConnectionA = roomA.GetAllDoorStates().FirstOrDefault(d =>
                d.shouldBeOpen &&
                d.connectedRoom == roomB &&
                d.direction == directionAtoB &&
                d.doorIndex == doorIndexA.doorIndex);

            var existingConnectionB = roomB.GetAllDoorStates().FirstOrDefault(d =>
                d.shouldBeOpen &&
                d.connectedRoom == roomA &&
                d.direction == directionBtoA &&
                d.doorIndex == doorIndexB.doorIndex);

            if (existingConnectionA != null || existingConnectionB != null)
            {

                return;
            }

            var conflictingDoorA = roomA.GetAllDoorStates().FirstOrDefault(d =>
                d.direction == directionAtoB &&
                d.doorIndex == doorIndexA.doorIndex &&
                d.shouldBeOpen &&
                d.connectedRoom != roomB);

            var conflictingDoorB = roomB.GetAllDoorStates().FirstOrDefault(d =>
                d.direction == directionBtoA &&
                d.doorIndex == doorIndexB.doorIndex &&
                d.shouldBeOpen &&
                d.connectedRoom != roomA);

            if (conflictingDoorA != null)
            {

                return;
            }

            if (conflictingDoorB != null)
            {

                return;
            }

            bool isValidConnection = UnifiedDoorIndexCalculator.ValidateDoorConnection(
                roomA, doorIndexA.doorIndex, directionAtoB,
                roomB, doorIndexB.doorIndex, directionBtoA);

            if (!isValidConnection)
            {

                return;
            }

            roomA.SetDoorState(directionAtoB, doorIndexA.doorIndex, shouldBeOpen: true, isMainPath, isLoop, roomB);
            roomB.SetDoorState(directionBtoA, doorIndexB.doorIndex, shouldBeOpen: true, isMainPath, isLoop, roomA);


        }
        private static Direction CalculateDirection(RoomNode from, RoomNode to)
        {
            Vector2Int fromMin = from.gridPosition;
            Vector2Int fromMax = from.gridPosition + from.gridSize - Vector2Int.one;
            Vector2Int toMin = to.gridPosition;
            Vector2Int toMax = to.gridPosition + to.gridSize - Vector2Int.one;

            if (fromMax.x + 1 == toMin.x && HasVerticalOverlap(fromMin, fromMax, toMin, toMax))
            {
                return Direction.East;
            }
            if (toMax.x + 1 == fromMin.x && HasVerticalOverlap(fromMin, fromMax, toMin, toMax))
            {
                return Direction.West;
            }
            if (fromMax.y + 1 == toMin.y && HasHorizontalOverlap(fromMin, fromMax, toMin, toMax))
            {
                return Direction.North;
            }
            if (toMax.y + 1 == fromMin.y && HasHorizontalOverlap(fromMin, fromMax, toMin, toMax))
            {
                return Direction.South;
            }

            Vector2 fromCenter = new Vector2(from.gridPosition.x + from.gridSize.x * 0.5f, from.gridPosition.y + from.gridSize.y * 0.5f);
            Vector2 toCenter = new Vector2(to.gridPosition.x + to.gridSize.x * 0.5f, to.gridPosition.y + to.gridSize.y * 0.5f);
            Vector2 diff = toCenter - fromCenter;

            Direction fallbackDirection;
            if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
                fallbackDirection = diff.x > 0 ? Direction.East : Direction.West;
            else
                fallbackDirection = diff.y > 0 ? Direction.North : Direction.South;

            return fallbackDirection;
        }


        private static bool HasVerticalOverlap(Vector2Int minA, Vector2Int maxA, Vector2Int minB, Vector2Int maxB)
        {
            return !(maxA.y < minB.y || maxB.y < minA.y);
        }

        private static bool HasHorizontalOverlap(Vector2Int minA, Vector2Int maxA, Vector2Int minB, Vector2Int maxB)
        {
            return !(maxA.x < minB.x || maxB.x < minA.x);
        }

        private static Direction GetOppositeDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                case Direction.West: return Direction.East;
                default: return Direction.North;
            }
        }


    }
}