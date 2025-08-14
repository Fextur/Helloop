using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Helloop.Generation.Data;
using Helloop.Data;

namespace Helloop.Generation.Services
{
    public class GridBasedRoomPlacementService : IRoomPlacementService
    {
        // --- Added: per-run tracking for miniboss uniqueness + chosen boss ---
        private readonly HashSet<GameObject> _usedMinibossPrefabs = new HashSet<GameObject>();
        private RoomData _selectedBoss; // chosen exactly once per generation run
        // ---------------------------------------------------------------------

        public RoomLayout PlaceRooms(MazeGenerationContext context)
        {
            // --- Added: reset uniqueness + select one boss once and expose via CircleData shim ---
            _usedMinibossPrefabs.Clear();
            var cd = context?.circleData;
            if (cd != null && cd.bossRoomCollection != null && cd.bossRoomCollection.Count > 0)
            {
                if (_selectedBoss == null)
                    _selectedBoss = cd.bossRoomCollection[Random.Range(0, cd.bossRoomCollection.Count)];
                cd.__RuntimeSelectBoss(_selectedBoss);
            }
            // --------------------------------------------------------------------------------------

            var layout = new RoomLayout();

            var gridData = InitializeGrid(context);
            var roomInstances = GenerateRoomLayout(context, gridData);

            layout.allRooms = ConvertToRoomNodes(roomInstances, context, gridData);
            layout.entryRoom = layout.allRooms.FirstOrDefault(r => r.isEntry);
            layout.bossRoom = layout.allRooms.FirstOrDefault(r => r.isBoss);

            return layout;
        }

        private GridData InitializeGrid(MazeGenerationContext context)
        {
            float complexityMultiplier = context.complexityMultiplier;
            int baseGridSize = 12;

            int scaledGridSize = Mathf.RoundToInt(baseGridSize * Mathf.Lerp(0.7f, 1.5f, (complexityMultiplier - 0.5f) / 1.5f));
            scaledGridSize = Mathf.Clamp(scaledGridSize, 8, 20);

            var grid = new GridCell[scaledGridSize, scaledGridSize];
            var gridCenter = new Vector2Int(scaledGridSize / 2, scaledGridSize / 2);

            for (int x = 0; x < scaledGridSize; x++)
            {
                for (int y = 0; y < scaledGridSize; y++)
                {
                    grid[x, y] = new GridCell { gridPosition = new Vector2Int(x, y) };
                }
            }

            return new GridData
            {
                grid = grid,
                gridSize = scaledGridSize,
                gridCenter = gridCenter,
                cellSize = 20f
            };
        }

        private List<RoomInstance> GenerateRoomLayout(MazeGenerationContext context, GridData gridData)
        {
            var allRooms = new List<RoomInstance>();
            var mainPath = new List<RoomInstance>();
            var branchPaths = new List<List<RoomInstance>>();

            GenerateMainPath(context, gridData, allRooms, mainPath);
            GenerateBranches(context, gridData, allRooms, mainPath, branchPaths);
            FillWithAdditionalRooms(context, gridData, allRooms);

            return allRooms;
        }

        private void GenerateMainPath(MazeGenerationContext context, GridData gridData, List<RoomInstance> allRooms, List<RoomInstance> mainPath)
        {
            int pathLength = Random.Range(4, 8);

            Vector2Int entryPos = GetRandomEdgePosition(gridData);
            var entryRoom = CreateRoom(entryPos, RoomType.Regular, gridData, isEntry: true);
            allRooms.Add(entryRoom);
            mainPath.Add(entryRoom);

            RoomInstance currentRoom = entryRoom;
            for (int i = 1; i < pathLength - 1; i++)
            {
                Vector2Int nextPos = FindNextPathPosition(currentRoom.gridPosition, gridData);
                if (nextPos == Vector2Int.one * -1) break;

                RoomType roomType = GetRandomRoomType(context.circleData);
                var nextRoom = CreateRoom(nextPos, roomType, gridData);
                if (nextRoom != null)
                {
                    allRooms.Add(nextRoom);
                    mainPath.Add(nextRoom);
                    currentRoom = nextRoom;
                }
            }

            // --- Added: ensure a boss is selected before reading its type (prevents NRE across scenes) ---
            var cd = context.circleData;
            var bossData = cd.bossRoom;
            if (bossData == null && cd.bossRoomCollection != null && cd.bossRoomCollection.Count > 0)
            {
                // choose one now if not already chosen (e.g., when coming from another scene)
                var chosen = cd.bossRoomCollection[Random.Range(0, cd.bossRoomCollection.Count)];
                cd.__RuntimeSelectBoss(chosen);
                bossData = chosen;
            }
            // ---------------------------------------------------------------------------------------------

            if (bossData != null)
            {
                Vector2Int bossPos = FindBossRoomPosition(currentRoom.gridPosition, bossData.roomType, gridData);
                if (bossPos != Vector2Int.one * -1)
                {
                    var bossRoom = CreateRoom(bossPos, bossData.roomType, gridData, isBoss: true);
                    if (bossRoom != null)
                    {
                        allRooms.Add(bossRoom);
                        mainPath.Add(bossRoom);
                    }
                }
            }
        }

        private void GenerateBranches(MazeGenerationContext context, GridData gridData, List<RoomInstance> allRooms, List<RoomInstance> mainPath, List<List<RoomInstance>> branchPaths)
        {
            // ORIGINAL range: Random.Range(2, 5) -> ints [2,3,4]
            int baseBranchCount = Random.Range(2, 5);

            // Scale by complexity (gentle). Caps keep close to original behavior.
            int branchCount = Mathf.Clamp(
                Mathf.RoundToInt(baseBranchCount * BranchScaleFromComplexity(context.complexityMultiplier)),
                1,
                6 // original max ~4; allow a tiny headroom while staying safe
            );

            for (int i = 0; i < branchCount; i++)
            {
                var validBranchPoints = mainPath.Where(r => !r.isEntryRoom && !r.isBossRoom).ToList();
                if (validBranchPoints.Count == 0) continue;

                RoomInstance branchStart = validBranchPoints[Random.Range(0, validBranchPoints.Count)];
                GenerateBranchFromRoom(branchStart, context, gridData, allRooms, branchPaths);
            }
        }

        private void GenerateBranchFromRoom(RoomInstance branchStart, MazeGenerationContext context, GridData gridData, List<RoomInstance> allRooms, List<List<RoomInstance>> branchPaths)
        {
            // ORIGINAL range: Random.Range(2, 5) -> ints [2,3,4]
            int baseBranchLength = Random.Range(2, 5);

            // Scale by complexity (gentle). Keep within a safe cap.
            int branchLength = Mathf.Clamp(
                Mathf.RoundToInt(baseBranchLength * LengthScaleFromComplexity(context.complexityMultiplier)),
                1,
                6 // small headroom beyond original
            );

            List<RoomInstance> branchPath = new List<RoomInstance> { branchStart };

            RoomInstance currentRoom = branchStart;
            for (int i = 0; i < branchLength; i++)
            {
                Vector2Int nextPos = FindNextBranchPosition(currentRoom.gridPosition, branchStart.gridPosition, gridData);
                if (nextPos == Vector2Int.one * -1) break;

                RoomType roomType = GetRandomRoomType(context.circleData);
                var nextRoom = CreateRoom(nextPos, roomType, gridData);
                if (nextRoom != null)
                {
                    allRooms.Add(nextRoom);
                    branchPath.Add(nextRoom); // C# Add (fixed)
                    currentRoom = nextRoom;
                }
            }

            if (branchPath.Count > 1)
            {
                branchPaths.Add(branchPath);
            }
        }

        private void FillWithAdditionalRooms(MazeGenerationContext context, GridData gridData, List<RoomInstance> allRooms)
        {
            int currentRoomCount = allRooms.Count;
            int targetRoomCount = Random.Range(8, 15);
            int additionalRooms = targetRoomCount - currentRoomCount;

            for (int i = 0; i < additionalRooms; i++)
            {
                RoomType roomType = Random.value < 0.7f ? RoomType.Regular : GetRandomRoomType(context.circleData);
                Vector2Int position = FindConnectablePositionForRoomType(roomType, gridData);

                if (position == Vector2Int.one * -1)
                {
                    if (roomType != RoomType.Regular)
                    {
                        roomType = RoomType.Regular;
                        position = FindConnectablePositionForRoomType(roomType, gridData);
                    }
                    if (position == Vector2Int.one * -1) continue;
                }

                var room = CreateRoom(position, roomType, gridData);
                if (room != null)
                {
                    allRooms.Add(room);
                }
            }
        }

        private RoomInstance CreateRoom(Vector2Int gridPos, RoomType roomType, GridData gridData, bool isEntry = false, bool isBoss = false)
        {
            Vector2Int roomSize = GetRoomGridSize(roomType);

            if (!CanPlaceRoom(gridPos, roomSize, gridData))
            {
                return null;
            }

            var roomInstance = new RoomInstance
            {
                roomObject = null,
                roomType = roomType,
                gridPosition = gridPos,
                gridSize = roomSize,
                isEntryRoom = isEntry,
                isBossRoom = isBoss
            };

            MarkGridCellsOccupied(roomInstance, gridData);

            return roomInstance;
        }

        private void MarkGridCellsOccupied(RoomInstance room, GridData gridData)
        {
            for (int x = 0; x < room.gridSize.x; x++)
            {
                for (int y = 0; y < room.gridSize.y; y++)
                {
                    Vector2Int cellPos = room.gridPosition + new Vector2Int(x, y);
                    if (IsValidGridPosition(cellPos, gridData))
                    {
                        gridData.grid[cellPos.x, cellPos.y].isOccupied = true;
                        gridData.grid[cellPos.x, cellPos.y].roomInstance = room;
                    }
                }
            }
        }

        private List<RoomNode> ConvertToRoomNodes(List<RoomInstance> roomInstances, MazeGenerationContext context, GridData gridData)
        {
            var roomNodes = new List<RoomNode>();

            foreach (var instance in roomInstances)
            {
                var roomPrefab = GetRoomPrefab(context.circleData, instance.roomType, instance.isEntryRoom, instance.isBossRoom);

                Vector3 worldPosition = GridToWorldPosition(instance.gridPosition, instance.gridSize, gridData);
                GameObject worldObject = Object.Instantiate(roomPrefab, worldPosition, Quaternion.identity, context.roomParent);

                worldObject.name = $"{(instance.isEntryRoom ? "Entry" : instance.isBossRoom ? "Boss" : "Regular")}Room_{instance.gridPosition.x}_{instance.gridPosition.y}_{instance.roomType}";

                var node = new RoomNode
                {
                    abstractPosition = new Vector2(instance.gridPosition.x, instance.gridPosition.y),
                    gridPosition = instance.gridPosition,
                    gridSize = instance.gridSize,
                    roomType = instance.roomType,
                    prefab = roomPrefab,
                    worldObject = worldObject,
                    isEntry = instance.isEntryRoom,
                    isBoss = instance.isBossRoom
                };

                roomNodes.Add(node);
            }

            return roomNodes;
        }

        private Vector3 GridToWorldPosition(Vector2Int gridPos, Vector2Int roomSize, GridData gridData)
        {
            Vector2Int offsetFromCenter = gridPos - gridData.gridCenter;
            float worldX = offsetFromCenter.x * gridData.cellSize + (roomSize.x - 1) * gridData.cellSize * 0.5f;
            float worldZ = offsetFromCenter.y * gridData.cellSize + (roomSize.y - 1) * gridData.cellSize * 0.5f;

            switch (GetRoomTypeFromSize(roomSize))
            {
                case RoomType.Wide:
                    worldX -= 10f;
                    break;
                case RoomType.Tall:
                    worldZ -= 10f;
                    break;
                case RoomType.Large:
                    worldX -= 10f;
                    worldZ -= 10f;
                    break;
                case RoomType.Regular:
                default:
                    break;
            }

            return new Vector3(worldX, 0, worldZ);
        }

        private RoomType GetRoomTypeFromSize(Vector2Int size)
        {
            if (size.x == 1 && size.y == 1) return RoomType.Regular;
            if (size.x == 2 && size.y == 1) return RoomType.Wide;
            if (size.x == 1 && size.y == 2) return RoomType.Tall;
            if (size.x == 2 && size.y == 2) return RoomType.Large;
            return RoomType.Regular;
        }

        private GameObject GetRoomPrefab(CircleData circleData, RoomType roomType, bool isEntry, bool isBoss)
        {
            if (isEntry) return circleData.entryRoom;
            if (isBoss) return circleData.bossRoom.roomPrefab;

            // --- Added: miniboss behave like regular but must be unique per prefab in this maze run ---
            var regular = (circleData.roomCollection ?? new List<RoomData>())
                .Where(r => r != null && r.roomPrefab != null && r.roomType == roomType)
                .ToList();

            var mini = (circleData.miniBossRoomCollection ?? new List<RoomData>())
                .Where(r => r != null && r.roomPrefab != null && r.roomType == roomType && !_usedMinibossPrefabs.Contains(r.roomPrefab))
                .ToList();

            List<RoomData> pool;
            if (regular.Count == 0) pool = mini;
            else if (mini.Count == 0) pool = regular;
            else
            {
                pool = new List<RoomData>(regular.Count + mini.Count);
                pool.AddRange(regular);
                pool.AddRange(mini);
            }

            if (pool.Count == 0)
                return null;

            var chosen = pool[Random.Range(0, pool.Count)];

            if (circleData.miniBossRoomCollection != null &&
                circleData.miniBossRoomCollection.Contains(chosen) &&
                chosen.roomPrefab != null)
            {
                _usedMinibossPrefabs.Add(chosen.roomPrefab);
            }
            // -----------------------------------------------------------------------------------------

            return chosen.roomPrefab;
        }

        private RoomType GetRandomRoomType(CircleData circleData)
        {
            List<RoomType> availableTypes = circleData.roomCollection.Select(r => r.roomType).ToList();
            if (availableTypes.Count == 0) return RoomType.Regular;
            return availableTypes[Random.Range(0, availableTypes.Count)];
        }

        private Vector2Int GetRoomGridSize(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Regular: return new Vector2Int(1, 1);
                case RoomType.Wide: return new Vector2Int(2, 1);
                case RoomType.Tall: return new Vector2Int(1, 2);
                case RoomType.Large: return new Vector2Int(2, 2);
                default: return new Vector2Int(1, 1);
            }
        }

        private bool CanPlaceRoom(Vector2Int gridPos, Vector2Int roomSize, GridData gridData)
        {
            for (int x = 0; x < roomSize.x; x++)
            {
                for (int y = 0; y < roomSize.y; y++)
                {
                    Vector2Int checkPos = gridPos + new Vector2Int(x, y);
                    if (!IsValidGridPosition(checkPos, gridData) || gridData.grid[checkPos.x, checkPos.y].isOccupied)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool IsValidGridPosition(Vector2Int pos, GridData gridData)
        {
            return pos.x >= 0 && pos.x < gridData.gridSize && pos.y >= 0 && pos.y < gridData.gridSize;
        }

        private Vector2Int GetRandomEdgePosition(GridData gridData)
        {
            int edge = Random.Range(0, 4);
            int margin = Mathf.Max(2, gridData.gridSize / 8);

            switch (edge)
            {
                case 0: return new Vector2Int(Random.Range(margin, gridData.gridSize - margin), 1);
                case 1: return new Vector2Int(gridData.gridSize - 2, Random.Range(margin, gridData.gridSize - margin));
                case 2: return new Vector2Int(Random.Range(margin, gridData.gridSize - margin), gridData.gridSize - 2);
                case 3: return new Vector2Int(1, Random.Range(margin, gridData.gridSize - margin));
                default: return new Vector2Int(1, 1);
            }
        }

        private Vector2Int FindNextPathPosition(Vector2Int currentPos, GridData gridData)
        {
            List<Vector2Int> possiblePositions = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in directions)
            {
                Vector2Int testPos = currentPos + dir;
                if (CanPlaceRoom(testPos, new Vector2Int(1, 1), gridData))
                {
                    possiblePositions.Add(testPos);
                }
            }

            if (possiblePositions.Count == 0) return Vector2Int.one * -1;

            Vector2Int bestPos = possiblePositions[0];
            float bestScore = CalculatePathScore(bestPos, gridData);

            foreach (var pos in possiblePositions)
            {
                float score = CalculatePathScore(pos, gridData);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPos = pos;
                }
            }

            return bestPos;
        }

        private float CalculatePathScore(Vector2Int pos, GridData gridData)
        {
            float distanceToCenter = Vector2Int.Distance(pos, gridData.gridCenter);
            float maxDistance = Vector2Int.Distance(Vector2Int.zero, gridData.gridCenter);
            float normalizedDistance = 1f - (distanceToCenter / maxDistance);
            float randomFactor = Random.Range(0.7f, 1.3f);
            return normalizedDistance * randomFactor;
        }

        private Vector2Int FindNextBranchPosition(Vector2Int currentPos, Vector2Int branchStartPos, GridData gridData)
        {
            List<Vector2Int> possiblePositions = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in directions)
            {
                Vector2Int testPos = currentPos + dir;
                if (CanPlaceRoom(testPos, new Vector2Int(1, 1), gridData))
                {
                    possiblePositions.Add(testPos);
                }
            }

            if (possiblePositions.Count == 0) return Vector2Int.one * -1;

            Vector2Int bestPos = possiblePositions[0];
            float bestDistance = Vector2Int.Distance(bestPos, branchStartPos);

            foreach (var pos in possiblePositions)
            {
                float distance = Vector2Int.Distance(pos, branchStartPos);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestPos = pos;
                }
            }

            return bestPos;
        }

        private Vector2Int FindBossRoomPosition(Vector2Int lastPathPos, RoomType bossRoomType, GridData gridData)
        {
            Vector2Int bossRoomSize = GetRoomGridSize(bossRoomType);
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in directions)
            {
                Vector2Int testPos = lastPathPos + dir;
                if (CanPlaceRoom(testPos, bossRoomSize, gridData))
                {
                    return testPos;
                }
            }

            for (int distance = 2; distance <= 4; distance++)
            {
                for (int x = -distance; x <= distance; x++)
                {
                    for (int y = -distance; y <= distance; y++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) == distance)
                        {
                            Vector2Int testPos = lastPathPos + new Vector2Int(x, y);
                            if (CanPlaceRoom(testPos, bossRoomSize, gridData))
                            {
                                return testPos;
                            }
                        }
                    }
                }
            }

            return Vector2Int.one * -1;
        }

        private Vector2Int FindConnectablePositionForRoomType(RoomType roomType, GridData gridData)
        {
            Vector2Int roomSize = GetRoomGridSize(roomType);
            List<Vector2Int> connectablePositions = new List<Vector2Int>();

            for (int x = 1; x < gridData.gridSize - roomSize.x; x++)
            {
                for (int y = 1; y < gridData.gridSize - roomSize.y; y++)
                {
                    Vector2Int testPos = new Vector2Int(x, y);
                    if (CanPlaceRoom(testPos, roomSize, gridData) && WouldBeConnectableToExistingRooms(testPos, roomSize, gridData))
                    {
                        connectablePositions.Add(testPos);
                    }
                }
            }

            if (connectablePositions.Count == 0) return Vector2Int.one * -1;
            return connectablePositions[Random.Range(0, connectablePositions.Count)];
        }

        private bool WouldBeConnectableToExistingRooms(Vector2Int gridPos, Vector2Int roomSize, GridData gridData)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in directions)
            {
                for (int x = 0; x < roomSize.x; x++)
                {
                    for (int y = 0; y < roomSize.y; y++)
                    {
                        Vector2Int roomCellPos = gridPos + new Vector2Int(x, y);
                        Vector2Int adjacentPos = roomCellPos + dir;

                        if (IsValidGridPosition(adjacentPos, gridData) && gridData.grid[adjacentPos.x, adjacentPos.y].isOccupied)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        // ===== helpers for subtle complexity scaling (kept conservative to preserve stability) =====
        private static float BranchScaleFromComplexity(float cm)
        {
            // Map cm in [1.00 .. 1.65] → scale in [1.00 .. 1.35]
            float cm01 = Mathf.InverseLerp(1.00f, 1.65f, Mathf.Clamp(cm, 1.00f, 1.65f));
            return Mathf.Lerp(1.00f, 1.35f, cm01);
        }

        private static float LengthScaleFromComplexity(float cm)
        {
            // Map cm in [1.00 .. 1.65] → scale in [1.00 .. 1.25]
            float cm01 = Mathf.InverseLerp(1.00f, 1.65f, Mathf.Clamp(cm, 1.00f, 1.65f));
            return Mathf.Lerp(1.00f, 1.25f, cm01);
        }
    }
}
