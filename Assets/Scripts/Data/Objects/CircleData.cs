using UnityEngine;
using System.Collections.Generic;

namespace Helloop.Data
{
    public enum RoomType
    {
        Regular,
        Wide,
        Tall,
        Large
    }

    [System.Serializable]
    public class RoomData
    {
        public GameObject roomPrefab;
        public RoomType roomType;
    }

    [CreateAssetMenu(fileName = "NewCircleData", menuName = "Helloop/Circles/CircleData")]
    public class CircleData : ScriptableObject
    {
        [Header("Circle Info")]
        public int circleLevel = 2;
        public string circleName = "Lust";

        [Header("Progression")]
        public CircleData nextCircle;

        [Header("Entry Room")]
        public GameObject entryRoom;

        // === Exact data changes you requested ===
        [Header("Boss Rooms")]
        public List<RoomData> bossRoomCollection = new List<RoomData>();

        [Header("MiniBoss Rooms")]
        public List<RoomData> miniBossRoomCollection = new List<RoomData>();
        // ========================================

        [Header("Regular Rooms")]
        public List<RoomData> roomCollection = new List<RoomData>();

        // Runtime-only selection exposed under the legacy name so downstream reads still work
        [System.NonSerialized] private RoomData _runtimeSelectedBoss;
        public RoomData bossRoom => _runtimeSelectedBoss;
        public void __RuntimeSelectBoss(RoomData chosen) { _runtimeSelectedBoss = chosen; }

        public string GetFullCircleName()
        {
            return $"Circle {circleLevel} - {circleName}";
        }

        public float GetComplexityMultiplier()
        {
            // Smooth, saturating growth: fast at early levels, then taper.
            float baseMul = 1.00f;
            float maxMul = 1.65f;
            float tau = 5f;

            int level = Mathf.Max(0, circleLevel);
            float t = 1f - Mathf.Exp(-level / Mathf.Max(0.01f, tau));
            return Mathf.Lerp(baseMul, maxMul, t);
        }
    }
}
