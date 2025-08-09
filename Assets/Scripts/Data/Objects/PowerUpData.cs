using UnityEngine;

namespace Helloop.Data
{
    public enum PowerUpType
    {
        Health,
        Ammo,
        Durability,
        LevelUpMelee,
        LevelUpRange
    }

    [CreateAssetMenu(fileName = "NewPowerUp", menuName = "Helloop/Rewards/PowerUp")]
    public class PowerUpData : RewardData
    {
        [Header("PowerUp Settings")]
        public PowerUpType powerUpType;
    }
}