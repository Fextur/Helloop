using UnityEngine;
using System.Collections.Generic;
using Helloop.Data;

namespace Helloop.Generation.Data
{
    public enum Direction
    {
        North, East, South, West
    }

    [System.Serializable]
    public class DoorState
    {
        public Direction direction;
        public int doorIndex;
        public bool shouldBeOpen;
        public bool isMainPath;
        public bool isLoop;
        public RoomNode connectedRoom;
    }


    public class GridData
    {
        public GridCell[,] grid;
        public int gridSize;
        public Vector2Int gridCenter;
        public float cellSize;
    }

    public class GridCell
    {
        public bool isOccupied = false;
        public RoomInstance roomInstance = null;
        public Vector2Int gridPosition;
    }

    public class RoomInstance
    {
        public GameObject roomObject;
        public RoomType roomType;
        public Vector2Int gridPosition;
        public Vector2Int gridSize;
        public bool isEntryRoom = false;
        public bool isBossRoom = false;
    }

    public class RoomConnection
    {
        public RoomNode fromRoom;
        public RoomNode toRoom;
        public bool isMainPath;
        public bool isLoop;
    }

    public class RoomLayout
    {
        public List<RoomNode> allRooms = new List<RoomNode>();
        public RoomNode entryRoom;
        public RoomNode bossRoom;
        public float circularRadius;
    }

    public class RoomGraph
    {
        public List<RoomNode> nodes = new List<RoomNode>();
        public List<RoomConnection> edges = new List<RoomConnection>();
        public RoomNode entryNode;
        public RoomNode bossNode;

    }

    public class MazeGenerationContext
    {
        public CircleData circleData;
        public Transform roomParent;
        public float complexityMultiplier;
        public int circleLevel;

        public HashSet<int> usedMinibossIndices;
        public HashSet<GameObject> usedMinibossPrefabs;

        public MazeGenerationContext()
        {
            usedMinibossIndices = new HashSet<int>();
            usedMinibossPrefabs = new HashSet<GameObject>();
        }
    }
}