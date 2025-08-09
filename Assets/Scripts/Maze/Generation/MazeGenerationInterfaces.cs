using Helloop.Generation.Data;

namespace Helloop.Generation.Services
{
    public interface IRoomPlacementService
    {
        RoomLayout PlaceRooms(MazeGenerationContext context);
    }

    public interface IRoomGraphBuilder
    {
        RoomGraph BuildGraph(RoomLayout roomLayout);
    }

    public interface IConnectivityGenerator
    {
        void GenerateConnectivity(RoomGraph roomGraph, MazeGenerationContext context);
    }

    public interface ILoopInjectionService
    {
        void AddLoops(RoomGraph roomGraph, float complexityMultiplier);
    }

}