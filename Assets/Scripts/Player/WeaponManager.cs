using UnityEngine;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Weapons;

namespace Helloop.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(InteractionController))]
    public class WeaponManager : MonoBehaviour
    {
        [Header("System References")]
        public WeaponSystem weaponSystem;
        public GameStateSystem gameStateSystem;
        public ProgressionSystem progressionSystem;

        [Header("Weapon Anchors")]
        public Transform leftHandAnchor;
        public Transform rightHandAnchor;
        public Camera playerCamera;

        [Header("Anti-Clipping Settings")]
        public float detectionDistance = 2.0f;
        public float pullBackDistance = 1.0f;
        public float smoothSpeed = 15f;
        public LayerMask wallLayers = -1;

        [Header("Starting Weapons")]
        public RangedWeaponData startingLeftWeapon;
        public MeleeWeaponData startingRightWeapon;

        public IRangeWeapon leftWeapon;
        public IMeleeWeapon rightWeapon;

        private GameObject LeftWeaponObject => (leftWeapon as MonoBehaviour)?.gameObject;
        private GameObject RightWeaponObject => (rightWeapon as MonoBehaviour)?.gameObject;

        public bool CanUseWeapons { get; set; } = true;

        private Vector3 originalLeftPosition;
        private Vector3 originalRightPosition;
        private Vector3 targetLeftPosition;
        private Vector3 targetRightPosition;

        private float lastClippingCheck = 0f;
        private float clippingCheckInterval = 0.15f;

        void Start()
        {
            InitializePositions();
            SetupEventSubscriptions();
            InitializeStartingWeapons();
        }

        void OnDestroy()
        {
            weaponSystem.OnWeaponEquipped.Unsubscribe(OnWeaponEquipped);
        }

        void Update()
        {
            if (!CanUseWeapons) return;

            UpdateAntiClippingSystem();
            HandleInput();
        }

        private void InitializePositions()
        {
            originalLeftPosition = leftHandAnchor.localPosition;
            originalRightPosition = rightHandAnchor.localPosition;
            targetLeftPosition = originalLeftPosition;
            targetRightPosition = originalRightPosition;
        }

        private void SetupEventSubscriptions()
        {
            weaponSystem.OnWeaponEquipped.Subscribe(OnWeaponEquipped);
        }

        private void InitializeStartingWeapons()
        {
            if (progressionSystem.IsInLimbo() ||
                (weaponSystem.currentLeftWeapon == null && weaponSystem.currentRightWeapon == null))
            {
                weaponSystem.ResetToDefaults();

                if (startingLeftWeapon != null)
                    weaponSystem.EquipLeftWeapon(startingLeftWeapon, 1);
                if (startingRightWeapon != null)
                    weaponSystem.EquipRightWeapon(startingRightWeapon, 1);
            }
            else
            {
                CreateWeaponInstances();
            }
        }

        private void OnWeaponEquipped()
        {
            CreateWeaponInstances();
        }

        private void CreateWeaponInstances()
        {
            CreateLeftWeaponInstance();
            CreateRightWeaponInstance();
        }

        private void CreateLeftWeaponInstance()
        {
            if (LeftWeaponObject != null) Destroy(LeftWeaponObject);

            if (weaponSystem.currentLeftWeapon is RangedWeaponData rangedData)
            {
                GameObject instance = Instantiate(rangedData.weaponPrefab, leftHandAnchor);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                RangedWeapon ranged = instance.GetComponent<RangedWeapon>();
                ranged.Data = rangedData;
                ranged.SetWeaponLevel(weaponSystem.leftWeaponLevel);

                leftWeapon = ranged;
            }
        }

        private void CreateRightWeaponInstance()
        {
            if (RightWeaponObject != null) Destroy(RightWeaponObject);

            if (weaponSystem.currentRightWeapon is MeleeWeaponData meleeData)
            {
                GameObject instance = Instantiate(meleeData.weaponPrefab, rightHandAnchor);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                MeleeWeapon melee = instance.GetComponent<MeleeWeapon>();
                melee.data = meleeData;
                melee.playerCamera = playerCamera;
                melee.SetWeaponLevel(weaponSystem.rightWeaponLevel);
                melee.SetDurability(weaponSystem.currentDurability);

                rightWeapon = melee;
            }
        }

        private void HandleInput()
        {
            if (gameStateSystem != null && gameStateSystem.ShouldBlockInput()) return;

            HandleRangedInput();
            HandleMeleeInput();
            HandleReloadInput();
        }

        private void HandleRangedInput()
        {
            if (leftWeapon == null || !weaponSystem.CanFireRanged()) return;

            if (Input.GetMouseButtonDown(0))
                leftWeapon.Use();

            if (Input.GetMouseButton(0) && leftWeapon.Data.fireMode == FireMode.Automatic)
                leftWeapon.Use();

            if (Input.GetMouseButtonDown(0) && leftWeapon.Data.fireMode == FireMode.Burst)
                leftWeapon.Use();

            if (Input.GetMouseButtonUp(0))
                leftWeapon.StopUse();
        }

        private void HandleMeleeInput()
        {
            if (rightWeapon == null || !weaponSystem.CanUseMelee()) return;

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.V))
                rightWeapon.Use();
        }

        private void HandleReloadInput()
        {
            if (Input.GetKeyDown(KeyCode.R) && leftWeapon != null)
            {
                RangedWeapon rangedWeapon = leftWeapon as RangedWeapon;
                if (rangedWeapon != null && rangedWeapon.CanReload())
                {
                    StartCoroutine(leftWeapon.Reload());
                }
            }
        }

        private void UpdateAntiClippingSystem()
        {
            CheckForClipping();
            UpdateWeaponPositions();
        }

        private void CheckForClipping()
        {
            if (playerCamera == null || Time.time - lastClippingCheck < clippingCheckInterval)
                return;

            lastClippingCheck = Time.time;

            Vector3 cameraPos = playerCamera.transform.position;
            Vector3 cameraForward = playerCamera.transform.forward;

            if (Physics.Raycast(cameraPos, cameraForward, out RaycastHit hit, detectionDistance, wallLayers))
            {
                float pullBackAmount = 1f - (hit.distance / detectionDistance);
                Vector3 positionOffset = new Vector3(0, -0.3f, -pullBackDistance * 2f) * pullBackAmount;

                targetLeftPosition = originalLeftPosition + positionOffset;
                targetRightPosition = originalRightPosition + positionOffset;
            }
            else
            {
                targetLeftPosition = originalLeftPosition;
                targetRightPosition = originalRightPosition;
            }
        }

        private void UpdateWeaponPositions()
        {
            leftHandAnchor.localPosition = Vector3.Lerp(
                leftHandAnchor.localPosition,
                targetLeftPosition,
                smoothSpeed * Time.deltaTime
            );

            rightHandAnchor.localPosition = Vector3.Lerp(
                rightHandAnchor.localPosition,
                targetRightPosition,
                smoothSpeed * Time.deltaTime
            );
        }

        public void SetWeaponsActive(bool isActive)
        {
            if (LeftWeaponObject != null)
                LeftWeaponObject.SetActive(isActive);

            if (RightWeaponObject != null)
                RightWeaponObject.SetActive(isActive);
        }
    }
}