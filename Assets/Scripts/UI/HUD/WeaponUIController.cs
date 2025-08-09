using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Helloop.Systems;

namespace Helloop.UI
{
    public class WeaponUIController : MonoBehaviour
    {
        [Header("Ranged Weapon UI")]
        public GameObject rangedWeaponPanel;
        public TextMeshProUGUI rangedWeaponName;
        public TextMeshProUGUI rangedWeaponLevel;
        public TextMeshProUGUI ammoText;

        [Header("Melee Weapon UI")]
        public GameObject meleeWeaponPanel;
        public TextMeshProUGUI meleeWeaponName;
        public TextMeshProUGUI meleeWeaponLevel;
        public Slider durabilityBar;

        [Header("System References")]
        public WeaponSystem weaponSystem;
        public ScalingSystem scalingSystem;

        void Start()
        {
            SubscribeToEvents();
            UpdateWeaponUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped?.Subscribe(UpdateWeaponUI);
                weaponSystem.OnAmmoChanged?.Subscribe(UpdateWeaponUI);
                weaponSystem.OnDurabilityChanged?.Subscribe(UpdateWeaponUI);
                weaponSystem.OnWeaponLevelChanged?.Subscribe(UpdateWeaponUI);
            }
        }

        void UnsubscribeFromEvents()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped?.Unsubscribe(UpdateWeaponUI);
                weaponSystem.OnAmmoChanged?.Unsubscribe(UpdateWeaponUI);
                weaponSystem.OnDurabilityChanged?.Unsubscribe(UpdateWeaponUI);
                weaponSystem.OnWeaponLevelChanged?.Unsubscribe(UpdateWeaponUI);
            }
        }

        void UpdateWeaponUI()
        {
            UpdateRangedWeaponUI();
            UpdateMeleeWeaponUI();
        }

        void UpdateRangedWeaponUI()
        {
            if (weaponSystem?.HasRangedWeapon() == true && weaponSystem.currentLeftWeapon != null)
            {
                if (rangedWeaponPanel != null)
                    rangedWeaponPanel.SetActive(true);

                if (rangedWeaponName != null)
                    rangedWeaponName.text = weaponSystem.currentLeftWeapon.weaponName;

                if (rangedWeaponLevel != null && scalingSystem != null)
                {
                    int weaponLevel = weaponSystem.leftWeaponLevel;
                    rangedWeaponLevel.text = scalingSystem.GetLevelDisplayText(weaponLevel);
                    rangedWeaponLevel.color = scalingSystem.GetLevelColor(weaponLevel);
                }

                if (ammoText != null)
                    ammoText.text = $"{weaponSystem.currentClipAmmo}/{weaponSystem.currentAmmo}";
            }
            else
            {
                if (rangedWeaponPanel != null)
                    rangedWeaponPanel.SetActive(false);

                if (ammoText != null)
                    ammoText.text = "";
            }
        }

        void UpdateMeleeWeaponUI()
        {
            if (weaponSystem?.HasMeleeWeapon() == true && weaponSystem.currentRightWeapon != null)
            {
                if (meleeWeaponPanel != null)
                    meleeWeaponPanel.SetActive(true);

                if (meleeWeaponName != null)
                    meleeWeaponName.text = weaponSystem.currentRightWeapon.weaponName;

                if (meleeWeaponLevel != null && scalingSystem != null)
                {
                    int weaponLevel = weaponSystem.rightWeaponLevel;
                    meleeWeaponLevel.text = scalingSystem.GetLevelDisplayText(weaponLevel);
                    meleeWeaponLevel.color = scalingSystem.GetLevelColor(weaponLevel);
                }

                if (durabilityBar != null)
                    durabilityBar.value = weaponSystem.GetDurabilityPercentage();
            }
            else
            {
                if (meleeWeaponPanel != null)
                    meleeWeaponPanel.SetActive(false);
            }
        }
    }
}