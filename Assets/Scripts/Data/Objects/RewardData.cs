using UnityEngine;

namespace Helloop.Data
{
    public abstract class RewardData : ScriptableObject
    {
        [Header("Basic Info")]
        public string rewardName;
        public GameObject rewardPrefab;
    }
}