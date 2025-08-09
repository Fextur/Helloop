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

        [Header("Special Rooms")]
        public GameObject entryRoom;
        public RoomData bossRoom = new();

        [Header("Regular Rooms")]
        public List<RoomData> roomCollection = new List<RoomData>();


        public string GetFullCircleName()
        {
            return $"Circle {circleLevel} - {circleName}";
        }


        public float GetComplexityMultiplier()
        {
            // Smooth, saturating growth: fast at early levels, then taper.
            // Tweak these three numbers to taste.
            float baseMul = 1.00f;  // circle 0/1 feel
            float maxMul = 1.65f;  // cap
            float tau = 5f;     // smaller = faster early growth

            int level = Mathf.Max(0, circleLevel);
            float t = 1f - Mathf.Exp(-level / Mathf.Max(0.01f, tau));
            return Mathf.Lerp(baseMul, maxMul, t);
        }

    }
}