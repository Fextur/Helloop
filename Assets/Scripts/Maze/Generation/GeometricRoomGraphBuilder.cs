using Helloop.Generation.Data;

namespace Helloop.Generation.Services
{
    public class GeometricRoomGraphBuilder : IRoomGraphBuilder
    {
        public RoomGraph BuildGraph(RoomLayout roomLayout)
        {
            var graph = new RoomGraph();

            graph.nodes.AddRange(roomLayout.allRooms);
            graph.entryNode = roomLayout.entryRoom;
            graph.bossNode = roomLayout.bossRoom;

            return graph;
        }
    }
}