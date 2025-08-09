using UnityEngine;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Interactions;


namespace Helloop.Rewards
{
    [RequireComponent(typeof(RewardVisualEffects))]

    public class WeaponPickup : MonoBehaviour, IInteractable
    {
        [Header("Weapon Data")]
        public WeaponData weaponData;
        public int weaponLevel = 1;

        [Header("Visual Display")]
        public TextMesh levelText;
        public GameObject levelDisplay;

        [Header("System Reference")]
        public WeaponSystem weaponSystem;
        public ScalingSystem scalingSystem;


        private bool hasBeenCollected = false;
        private bool hasPlayerExited = false;

        void Start()
        {
            UpdateLevelDisplay();
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

        public void SetWeaponData(WeaponData data, int level = 1)
        {
            weaponData = data;
            weaponLevel = level;
            UpdateLevelDisplay();
        }

        void UpdateLevelDisplay()
        {
            if (levelText != null)
            {
                levelText.text = scalingSystem.GetLevelDisplayText(weaponLevel);
                levelText.color = scalingSystem.GetLevelColor(weaponLevel);
            }

            if (TryGetComponent<Renderer>(out var renderer))
            {
                Color levelColor = scalingSystem.GetLevelColor(weaponLevel);
                levelColor.a = 0.7f;

                Material levelMaterial = new Material(renderer.material);
                levelMaterial.color = Color.Lerp(renderer.material.color, levelColor, 0.3f);
                renderer.material = levelMaterial;
            }
        }

        public void Interact()
        {
            if (hasBeenCollected || weaponData == null) return;

            hasBeenCollected = true;
            GiveWeaponToPlayer();
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
        void GiveWeaponToPlayer()
        {
            if (weaponSystem != null)
            {
                weaponSystem.EquipWeapon(weaponData, weaponLevel);
            }
        }

        public string GetInteractionText()
        {
            if (hasBeenCollected)
                return "Already collected";

            if (!hasPlayerExited)
                return "Step away and return to collect";

            string levelText = $" (Level {weaponLevel})";
            return $"Press E to equip {weaponData.weaponName}{levelText}";
        }

        public bool CanInteract()
        {
            return !hasBeenCollected && weaponData != null && hasPlayerExited;
        }

        public void OnInteractionEnter()
        {
        }

        public void OnInteractionExit()
        {
        }

        void OnDestroy()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Destroy(renderer.material);
            }
        }
    }
}