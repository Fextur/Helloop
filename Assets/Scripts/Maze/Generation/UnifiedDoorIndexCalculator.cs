using UnityEngine;
using Helloop.Data;
using Helloop.Generation.Data;

namespace Helloop.Generation.Algorithms
{
    public static class UnifiedDoorIndexCalculator
    {
        [System.Serializable]
        public struct DoorIndexResult
        {
            public int doorIndex;
            public bool isValid;
            public string debugInfo;

            public static DoorIndexResult Valid(int index, string debug = "") =>
                new DoorIndexResult { doorIndex = index, isValid = true, debugInfo = debug };
            public static DoorIndexResult Invalid(string debug) =>
                new DoorIndexResult { doorIndex = -1, isValid = false, debugInfo = debug };
        }

        public static DoorIndexResult CalculateForGeneration(RoomNode room, RoomNode connectedRoom, Direction direction)
        {
            if (room.roomType == RoomType.Regular)
                return DoorIndexResult.Valid(0, "Regular room always uses index 0");

            Vector2Int connectionPoint = GetConnectionPoint(room, connectedRoom, direction);
            return CalculateFromGridPosition(room, connectionPoint, direction);
        }

        public static DoorIndexResult CalculateForDoorHole(RoomNode room, Vector3 doorWorldPosition, Direction direction)
        {
            if (room.roomType == RoomType.Regular)
                return DoorIndexResult.Valid(0, "Regular room always uses index 0");

            float halfCell = 10f;
            Vector3 origin = room.worldObject.transform.position;
            Vector3 roomCenter = origin + new Vector3(
                (room.gridSize.x - 1) * halfCell,
                0f,
                (room.gridSize.y - 1) * halfCell
            );

            return CalculateForDoorHole_WithCenter(room, roomCenter, doorWorldPosition, direction);
        }

        public static DoorIndexResult CalculateForDoorHole_WithCenter(RoomNode room, Vector3 roomCenter, Vector3 doorWorldPosition, Direction direction)
        {
            Vector3 localOffset = doorWorldPosition - roomCenter;
            return CalculateFromLocalOffset(room.roomType, localOffset, direction);
        }

        private static Vector2Int GetConnectionPoint(RoomNode fromRoom, RoomNode toRoom, Direction direction)
        {
            Vector2Int fromMin = fromRoom.gridPosition;
            Vector2Int fromMax = fromRoom.gridPosition + fromRoom.gridSize - Vector2Int.one;
            Vector2Int toMin = toRoom.gridPosition;
            Vector2Int toMax = toRoom.gridPosition + toRoom.gridSize - Vector2Int.one;

            int overlapMinY = Mathf.Max(fromMin.y, toMin.y);
            int overlapMaxY = Mathf.Min(fromMax.y, toMax.y);
            int overlapMinX = Mathf.Max(fromMin.x, toMin.x);
            int overlapMaxX = Mathf.Min(fromMax.x, toMax.x);

            switch (direction)
            {
                case Direction.East:
                    {
                        int connectY = (overlapMinY <= overlapMaxY)
                            ? overlapMinY
                            : Mathf.Clamp(toMin.y, fromMin.y, fromMax.y);
                        return new Vector2Int(fromMax.x, connectY);
                    }

                case Direction.West:
                    {
                        int connectY = (overlapMinY <= overlapMaxY)
                            ? overlapMinY
                            : Mathf.Clamp(toMin.y, fromMin.y, fromMax.y);
                        return new Vector2Int(fromMin.x, connectY);
                    }

                case Direction.North:
                    {
                        int connectX = (overlapMinX <= overlapMaxX)
                            ? overlapMinX
                            : Mathf.Clamp(toMin.x, fromMin.x, fromMax.x);
                        return new Vector2Int(connectX, fromMax.y);
                    }

                case Direction.South:
                    {
                        int connectX = (overlapMinX <= overlapMaxX)
                            ? overlapMinX
                            : Mathf.Clamp(toMin.x, fromMin.x, fromMax.x);
                        return new Vector2Int(connectX, fromMin.y);
                    }

                default:
                    return fromMin;
            }
        }

        private static DoorIndexResult CalculateFromGridPosition(RoomNode room, Vector2Int connectionPoint, Direction direction)
        {
            Vector2Int roomMin = room.gridPosition;
            Vector2Int roomMax = room.gridPosition + room.gridSize - Vector2Int.one;

            switch (room.roomType)
            {
                case RoomType.Wide:
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        float roomCenterX = roomMin.x + 0.5f;
                        int index = connectionPoint.x < roomCenterX ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Wide {direction}: connectionX={connectionPoint.x}, roomCenterX={roomCenterX:F1}");
                    }
                    return DoorIndexResult.Valid(0, $"Wide {direction}: single door side");

                case RoomType.Tall:
                    if (direction == Direction.East || direction == Direction.West)
                    {
                        int index = (connectionPoint.y == roomMax.y) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Tall {direction}: connectionY={connectionPoint.y}, roomMaxY={roomMax.y}");
                    }
                    return DoorIndexResult.Valid(0, $"Tall {direction}: single door side");

                case RoomType.Large:
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        float roomCenterX = roomMin.x + 0.5f;
                        int index = connectionPoint.x < roomCenterX ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Large {direction}: connectionX={connectionPoint.x}, roomCenterX={roomCenterX:F1}");
                    }
                    else
                    {
                        int index = (connectionPoint.y == roomMax.y) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Large {direction}: connectionY={connectionPoint.y}, roomMaxY={roomMax.y}");
                    }

                default:
                    return DoorIndexResult.Valid(0, "Default case");
            }
        }

        private static DoorIndexResult CalculateFromLocalOffset(RoomType roomType, Vector3 localOffset, Direction direction)
        {
            switch (roomType)
            {
                case RoomType.Wide:
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        int index = (localOffset.x < 0f) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Wide {direction}: localX={localOffset.x:F2}");
                    }
                    return DoorIndexResult.Valid(0, $"Wide {direction}: single door side");

                case RoomType.Tall:
                    if (direction == Direction.East || direction == Direction.West)
                    {
                        int index = (localOffset.z >= 0f) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Tall {direction}: localZ={localOffset.z:F2}");
                    }
                    return DoorIndexResult.Valid(0, $"Tall {direction}: single door side");

                case RoomType.Large:
                    if (direction == Direction.North || direction == Direction.South)
                    {
                        int index = (localOffset.x < 0f) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Large {direction}: localX={localOffset.x:F2}");
                    }
                    else
                    {
                        int index = (localOffset.z >= 0f) ? 0 : 1;
                        return DoorIndexResult.Valid(index, $"Large {direction}: localZ={localOffset.z:F2}");
                    }

                default:
                    return DoorIndexResult.Valid(0, "Default case");
            }
        }

        public static bool ValidateDoorConnection(RoomNode roomA, int doorIndexA, Direction directionA,
                                                 RoomNode roomB, int doorIndexB, Direction directionB)
        {
            Direction expectedOpposite = GetOppositeDirection(directionA);
            if (directionB != expectedOpposite)
            {
                Debug.LogError($"Direction mismatch: {directionA} should connect to {expectedOpposite}, got {directionB}");
                return false;
            }

            return ValidatePhysicalAlignment(roomA, doorIndexA, directionA, roomB, doorIndexB, directionB);
        }

        private static bool ValidatePhysicalAlignment(RoomNode roomA, int doorIndexA, Direction directionA,
                                                     RoomNode roomB, int doorIndexB, Direction directionB)
        {
            if (roomA.roomType == RoomType.Regular && roomB.roomType == RoomType.Regular)
                return true;

            Vector2Int connectionPointA = GetConnectionPoint(roomA, roomB, directionA);
            Vector2Int connectionPointB = GetConnectionPoint(roomB, roomA, directionB);

            var calculatedIndexA = CalculateFromGridPosition(roomA, connectionPointA, directionA);
            var calculatedIndexB = CalculateFromGridPosition(roomB, connectionPointB, directionB);

            if (!calculatedIndexA.isValid || !calculatedIndexB.isValid)
            {
                Debug.LogError($"❌ Invalid calculated indices during validation: A={calculatedIndexA.debugInfo}, B={calculatedIndexB.debugInfo}");
                return false;
            }

            bool indexesMatch = calculatedIndexA.doorIndex == doorIndexA && calculatedIndexB.doorIndex == doorIndexB;

            if (!indexesMatch)
            {
                Debug.LogError($"❌ Door index mismatch during validation: " +
                               $"{GetRoomIdentifier(roomA)} expected {doorIndexA}, calculated {calculatedIndexA.doorIndex}; " +
                               $"{GetRoomIdentifier(roomB)} expected {doorIndexB}, calculated {calculatedIndexB.doorIndex}");
                Debug.LogError($"   Connection points: A={connectionPointA}, B={connectionPointB}");
            }

            return indexesMatch;
        }

        private static string GetRoomIdentifier(RoomNode room)
        {
            if (room == null) return "NULL";
            string type = room.isEntry ? "ENTRY" : room.isBoss ? "BOSS" : "REG";
            return $"{type}-{room.roomType}@({room.gridPosition.x},{room.gridPosition.y})";
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
