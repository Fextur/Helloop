using UnityEngine;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Interactions;

namespace Helloop.Rewards
{
    [RequireComponent(typeof(RewardVisualEffects))]

    public class PowerUpPickup : MonoBehaviour, IInteractable
    {
        [Header("Reward Data")]
        public PowerUpData powerUpData;

        [Header("System References")]
        public WeaponSystem weaponSystem;
        public PlayerSystem playerSystem;

        private bool hasBeenCollected = false;
        private bool hasPlayerExited = false;

        void Start()
        {
            StartCoroutine(CheckInitialPlayerPosition());
        }

        System.Collections.IEnumerator CheckInitialPlayerPosition()
        {
            yield return null;

            Collider playerCollider = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider>();
            Collider myCollider = GetComponent<Collider>();

            if (playerCollider != null && myCollider != null)
            {
                if (myCollider.bounds.Intersects(playerCollider.bounds))
                {
                    hasPlayerExited = false;
                }
                else
                {
                    hasPlayerExited = true;
                }
            }
            else
            {
                hasPlayerExited = true;
            }
        }

        public void SetPowerUpData(PowerUpData data)
        {
            powerUpData = data;
        }

        public void Interact()
        {
            if (hasBeenCollected || powerUpData == null) return;

            hasBeenCollected = true;
            ApplyPowerUp();
            Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && hasPlayerExited)
            {
                Interact();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                hasPlayerExited = true;
            }
        }

        void ApplyPowerUp()
        {
            switch (powerUpData.powerUpType)
            {
                case PowerUpType.Health:
                    if (playerSystem != null)
                    {
                        playerSystem.SetFullHealth();
                    }
                    break;

                case PowerUpType.LevelUpMelee:
                    if (weaponSystem != null && weaponSystem.HasMeleeWeapon())
                    {
                        weaponSystem.LevelUpRightWeapon();
                    }
                    break;

                case PowerUpType.LevelUpRange:
                    if (weaponSystem != null && weaponSystem.HasRangedWeapon())
                    {
                        weaponSystem.LevelUpLeftWeapon();
                    }
                    break;

                case PowerUpType.Ammo:
                    if (weaponSystem != null && weaponSystem.HasRangedWeapon())
                    {
                        weaponSystem.RefillAmmo();
                    }
                    break;

                case PowerUpType.Durability:
                    if (weaponSystem != null && weaponSystem.HasMeleeWeapon())
                    {
                        weaponSystem.RestoreDurability();
                    }
                    break;
            }
        }

        public string GetInteractionText()
        {
            if (hasBeenCollected)
                return "Already collected";

            if (!hasPlayerExited)
                return "Step away and return to collect";

            string description = GetPowerUpDescription();
            return $"Press E to collect {powerUpData.rewardName}{description}";
        }

        string GetPowerUpDescription()
        {
            switch (powerUpData.powerUpType)
            {
                case PowerUpType.Health:
                    return " (Restore Health)";
                case PowerUpType.Ammo:
                    return " (Restore Ammo)";
                case PowerUpType.Durability:
                    return " (Repair Weapon)";
                case PowerUpType.LevelUpMelee:
                    return " (Level Up Melee)";
                case PowerUpType.LevelUpRange:
                    return " (Level Up Ranged)";
                default:
                    return "";
            }
        }

        public bool CanInteract()
        {
            return !hasBeenCollected && powerUpData != null && hasPlayerExited;
        }

        public void OnInteractionEnter()
        {
        }

        public void OnInteractionExit()
        {
        }
    }
}