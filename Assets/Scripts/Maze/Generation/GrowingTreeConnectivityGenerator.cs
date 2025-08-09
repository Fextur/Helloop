using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Helloop.Generation.Data;

namespace Helloop.Generation.Services
{
    public class GrowingTreeConnectivityGenerator : IConnectivityGenerator
    {
        private const float NEWEST_CELL_PROBABILITY = 0.7f;

        public void GenerateConnectivity(RoomGraph roomGraph, MazeGenerationContext context)
        {
            ClearExistingConnections(roomGraph);

            var visited = new HashSet<RoomNode>();
            var activeList = new List<RoomNode>();
            var mainPathRooms = new HashSet<RoomNode>();

            var entryRoom = roomGraph.nodes.FirstOrDefault(r => r.isEntry);
            if (entryRoom == null) return;

            visited.Add(entryRoom);
            activeList.Add(entryRoom);
            mainPathRooms.Add(entryRoom);

            while (activeList.Count > 0)
            {
                RoomNode currentRoom = SelectNextRoom(activeList);
                if (currentRoom == null) break;

                var unvisitedNeighbors = GetUnvisitedGridAdjacentNeighbors(currentRoom, roomGraph.nodes, visited);

                if (unvisitedNeighbors.Count > 0)
                {
                    var chosenNeighbor = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

                    bool isMainPathConnection = mainPathRooms.Contains(currentRoom);
                    RoomNode.CreateBidirectionalConnection(currentRoom, chosenNeighbor, isMainPathConnection);

                    AddValidatedGridConnection(roomGraph, currentRoom, chosenNeighbor, isMainPathConnection);

                    visited.Add(chosenNeighbor);
                    activeList.Add(chosenNeighbor);

                    if (isMainPathConnection)
                    {
                        mainPathRooms.Add(chosenNeighbor);
                    }
                }
                else
                {
                    activeList.Remove(currentRoom);
                }
            }

            EnsureBossConnectivity(roomGraph, visited, mainPathRooms);
            EnsureFullConnectivity(roomGraph, visited);
        }

        private void ClearExistingConnections(RoomGraph roomGraph)
        {
            roomGraph.edges.Clear();
            foreach (var node in roomGraph.nodes)
            {
                node.connections.Clear();
            }
        }

        private RoomNode SelectNextRoom(List<RoomNode> activeList)
        {
            if (activeList.Count == 0) return null;

            if (Random.value < NEWEST_CELL_PROBABILITY)
            {
                return activeList[activeList.Count - 1];
            }
            else
            {
                return activeList[Random.Range(0, activeList.Count)];
            }
        }

        private List<RoomNode> GetUnvisitedGridAdjacentNeighbors(RoomNode room, List<RoomNode> allRooms, HashSet<RoomNode> visited)
        {
            var neighbors = new List<RoomNode>();

            foreach (var candidate in allRooms)
            {
                if (visited.Contains(candidate)) continue;
                if (AreRoomsGridAdjacent(room, candidate))
                {
                    neighbors.Add(candidate);
                }
            }

            return neighbors;
        }

        private bool AreRoomsGridAdjacent(RoomNode roomA, RoomNode roomB)
        {
            if (roomA == null || roomB == null) return false;
            if (roomA == roomB) return false;

            Vector2Int minA = roomA.gridPosition;
            Vector2Int maxA = roomA.gridPosition + roomA.gridSize - Vector2Int.one;
            Vector2Int minB = roomB.gridPosition;
            Vector2Int maxB = roomB.gridPosition + roomB.gridSize - Vector2Int.one;

            bool adjacentHorizontally = (maxA.x + 1 == minB.x || maxB.x + 1 == minA.x) &&
                                        !(maxA.y < minB.y || maxB.y < minA.y);

            bool adjacentVertically = (maxA.y + 1 == minB.y || maxB.y + 1 == minA.y) &&
                                      !(maxA.x < minB.x || maxB.x < minA.x);

            return adjacentHorizontally || adjacentVertically;
        }

        private void AddValidatedGridConnection(RoomGraph roomGraph, RoomNode fromRoom, RoomNode toRoom, bool isMainPath)
        {
            var connection = new RoomConnection
            {
                fromRoom = fromRoom,
                toRoom = toRoom,
                isMainPath = isMainPath,
                isLoop = false
            };

            roomGraph.edges.Add(connection);
            fromRoom.connections.Add(connection);
            toRoom.connections.Add(connection);
        }

        private void EnsureBossConnectivity(RoomGraph roomGraph, HashSet<RoomNode> visited, HashSet<RoomNode> mainPathRooms)
        {
            var bossRoom = roomGraph.nodes.FirstOrDefault(r => r.isBoss);
            if (bossRoom == null) return;

            if (!visited.Contains(bossRoom))
            {
                var adjacentVisitedRooms = new List<RoomNode>();
                foreach (var room in visited)
                {
                    if (AreRoomsGridAdjacent(bossRoom, room))
                    {
                        adjacentVisitedRooms.Add(room);
                    }
                }

                if (adjacentVisitedRooms.Count > 0)
                {
                    var nearestRoom = adjacentVisitedRooms.OrderBy(r => mainPathRooms.Contains(r) ? 0 : 1).First();

                    RoomNode.CreateBidirectionalConnection(nearestRoom, bossRoom, isMainPath: true);
                    AddValidatedGridConnection(roomGraph, nearestRoom, bossRoom, isMainPath: true);
                    visited.Add(bossRoom);
                    mainPathRooms.Add(bossRoom);
                }
            }
        }

        private void EnsureFullConnectivity(RoomGraph roomGraph, HashSet<RoomNode> visited)
        {
            var unvisited = roomGraph.nodes.Where(r => !visited.Contains(r)).ToList();

            foreach (var unvisitedRoom in unvisited)
            {
                var adjacentVisitedRooms = new List<RoomNode>();
                foreach (var room in visited)
                {
                    if (AreRoomsGridAdjacent(unvisitedRoom, room))
                    {
                        adjacentVisitedRooms.Add(room);
                    }
                }

                if (adjacentVisitedRooms.Count > 0)
                {
                    var nearestRoom = adjacentVisitedRooms.First();

                    RoomNode.CreateBidirectionalConnection(nearestRoom, unvisitedRoom, isMainPath: false);
                    AddValidatedGridConnection(roomGraph, nearestRoom, unvisitedRoom, isMainPath: false);
                    visited.Add(unvisitedRoom);
                }
            }
        }
    }
}