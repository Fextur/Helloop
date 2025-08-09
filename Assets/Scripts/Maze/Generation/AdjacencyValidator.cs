using UnityEngine;
using Helloop.Generation.Data;

namespace Helloop.Generation.Algorithms
{
    public static class AdjacencyValidator
    {
        public static bool AreRoomsAdjacent(RoomNode roomA, RoomNode roomB)
        {
            if (roomA == null || roomB == null) return false;
            if (roomA == roomB) return false;

            return AreRoomsGridAdjacent(roomA, roomB);
        }

        public static bool ValidateConnection(RoomNode from, RoomNode to, string callerName = "Unknown")
        {
            if (!AreRoomsAdjacent(from, to))
            {
                return false;
            }

            if (!ValidateGridOverlap(from, to))
            {
                return false;
            }

            return true;
        }

        private static bool AreRoomsGridAdjacent(RoomNode roomA, RoomNode roomB)
        {
            Vector2Int minA = roomA.gridPosition;
            Vector2Int maxA = roomA.gridPosition + roomA.gridSize - Vector2Int.one;
            Vector2Int minB = roomB.gridPosition;
            Vector2Int maxB = roomB.gridPosition + roomB.gridSize - Vector2Int.one;

            bool adjacentHorizontally = (maxA.x + 1 == minB.x || maxB.x + 1 == minA.x) &&
                                        HasVerticalOverlap(minA, maxA, minB, maxB);

            bool adjacentVertically = (maxA.y + 1 == minB.y || maxB.y + 1 == minA.y) &&
                                      HasHorizontalOverlap(minA, maxA, minB, maxB);

            return adjacentHorizontally || adjacentVertically;
        }

        private static bool HasVerticalOverlap(Vector2Int minA, Vector2Int maxA, Vector2Int minB, Vector2Int maxB)
        {
            return !(maxA.y < minB.y || maxB.y < minA.y);
        }

        private static bool HasHorizontalOverlap(Vector2Int minA, Vector2Int maxA, Vector2Int minB, Vector2Int maxB)
        {
            return !(maxA.x < minB.x || maxB.x < minA.x);
        }

        private static bool ValidateGridOverlap(RoomNode from, RoomNode to)
        {
            Vector2Int minA = from.gridPosition;
            Vector2Int maxA = from.gridPosition + from.gridSize - Vector2Int.one;
            Vector2Int minB = to.gridPosition;
            Vector2Int maxB = to.gridPosition + to.gridSize - Vector2Int.one;

            if (maxA.x + 1 == minB.x || maxB.x + 1 == minA.x)
            {
                int overlapStart = Mathf.Max(minA.y, minB.y);
                int overlapEnd = Mathf.Min(maxA.y, maxB.y);
                return overlapEnd >= overlapStart;
            }

            if (maxA.y + 1 == minB.y || maxB.y + 1 == minA.y)
            {
                int overlapStart = Mathf.Max(minA.x, minB.x);
                int overlapEnd = Mathf.Min(maxA.x, maxB.x);
                return overlapEnd >= overlapStart;
            }

            return false;
        }
    }

    public static class RoomGraphValidationExtensions
    {
        public static bool AddValidatedConnection(this RoomGraph roomGraph, RoomNode from, RoomNode to, bool isMainPath = false, bool isLoop = false, string callerName = "Unknown")
        {
            if (!AdjacencyValidator.ValidateConnection(from, to, callerName))
            {
                return false;
            }

            RoomNode.CreateBidirectionalConnection(from, to, isMainPath, isLoop);

            var connection = new RoomConnection
            {
                fromRoom = from,
                toRoom = to,
                isMainPath = isMainPath,
                isLoop = isLoop
            };

            roomGraph.edges.Add(connection);
            from.connections.Add(connection);
            to.connections.Add(connection);

            return true;
        }
    }
}