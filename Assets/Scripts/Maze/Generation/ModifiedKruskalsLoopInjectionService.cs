using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Helloop.Generation.Data;
using Helloop.Generation.Algorithms;

namespace Helloop.Generation.Services
{
    public class ModifiedKruskalsLoopInjectionService : ILoopInjectionService
    {
        private const float BASE_LOOP_DENSITY = 0.12f;
        private const float MAX_LOOP_DENSITY = 0.20f;
        private const float MIN_LOOP_DENSITY = 0.08f;

        public void AddLoops(RoomGraph roomGraph, float complexityMultiplier)
        {
            var allPossibleEdges = GenerateAllAdjacentEdges(roomGraph);
            var existingEdges = new HashSet<string>(roomGraph.edges.Select(e => GetEdgeKey(e.fromRoom, e.toRoom)));

            var candidateEdges = allPossibleEdges
                .Where(edge => !existingEdges.Contains(GetEdgeKey(edge.roomA, edge.roomB)))
                .OrderBy(edge => edge.weight)
                .ToList();

            float targetLoopDensity = CalculateTargetLoopDensity(complexityMultiplier);
            int targetLoopCount = Mathf.RoundToInt(roomGraph.edges.Count * targetLoopDensity);

            AddStrategicLoops(roomGraph, candidateEdges, targetLoopCount);
        }

        private List<PotentialEdge> GenerateAllAdjacentEdges(RoomGraph roomGraph)
        {
            var edges = new List<PotentialEdge>();

            for (int i = 0; i < roomGraph.nodes.Count; i++)
            {
                for (int j = i + 1; j < roomGraph.nodes.Count; j++)
                {
                    var roomA = roomGraph.nodes[i];
                    var roomB = roomGraph.nodes[j];

                    if (AdjacencyValidator.AreRoomsAdjacent(roomA, roomB))
                    {
                        float weight = CalculateEdgeWeight(roomA, roomB, roomGraph);
                        edges.Add(new PotentialEdge
                        {
                            roomA = roomA,
                            roomB = roomB,
                            weight = weight,
                            priority = CalculateLoopPriority(roomA, roomB, roomGraph)
                        });
                    }
                }
            }

            return edges;
        }

        private float CalculateTargetLoopDensity(float complexityMultiplier)
        {
            float density = BASE_LOOP_DENSITY + (complexityMultiplier - 1f) * 0.05f;
            return Mathf.Clamp(density, MIN_LOOP_DENSITY, MAX_LOOP_DENSITY);
        }

        private void AddStrategicLoops(RoomGraph roomGraph, List<PotentialEdge> candidateEdges, int targetLoopCount)
        {
            var addedLoops = 0;

            foreach (var edge in candidateEdges)
            {
                if (addedLoops >= targetLoopCount)
                    break;

                if (ShouldAddLoop(edge, roomGraph))
                {
                    bool connectionAdded = roomGraph.AddValidatedConnection(
                        edge.roomA, edge.roomB,
                        isMainPath: false,
                        isLoop: true,
                        callerName: "LoopInjection"
                    );

                    if (connectionAdded)
                    {
                        addedLoops++;
                    }
                }
            }
        }

        private bool ShouldAddLoop(PotentialEdge edge, RoomGraph roomGraph)
        {
            return Random.Range(0f, 1f) < 0.5f;
        }

        private float CalculateEdgeWeight(RoomNode roomA, RoomNode roomB, RoomGraph roomGraph)
        {
            float distance = Vector2.Distance(roomA.abstractPosition, roomB.abstractPosition);
            return distance;
        }

        private float CalculateLoopPriority(RoomNode roomA, RoomNode roomB, RoomGraph roomGraph)
        {
            float priority = 1f;

            if (roomA.isEntry || roomB.isEntry || roomA.isBoss || roomB.isBoss)
                priority *= 0.3f;

            float distance = Vector2.Distance(roomA.abstractPosition, roomB.abstractPosition);
            priority *= 1f / (distance + 0.1f);

            return priority;
        }

        private string GetEdgeKey(RoomNode roomA, RoomNode roomB)
        {
            int hashA = roomA.GetHashCode();
            int hashB = roomB.GetHashCode();

            if (hashA < hashB)
                return $"{hashA}-{hashB}";
            else
                return $"{hashB}-{hashA}";
        }

        private class PotentialEdge
        {
            public RoomNode roomA;
            public RoomNode roomB;
            public float weight;
            public float priority;
        }
    }
}