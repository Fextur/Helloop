using UnityEngine;
using Helloop.Systems;
using Helloop.Events;
using Helloop.Weapons.States;

namespace Helloop.Weapons
{
    public class MeleeWeapon : WeaponBase, IMeleeWeapon
    {
        [SerializeField] public MeleeWeaponData data;

        [Header("System References")]
        public WeaponSystem weaponSystem;
        public ScalingSystem scalingSystem;
        public Camera playerCamera;

        [Header("Event System")]
        public EnemyHitEvent onEnemyHit;

        [Header("Level System")]
        public int weaponLevel = 1;

        private MeleeWeaponStateMachine stateMachine;

        public new Vector3 originalPosition { get; private set; }
        public new Quaternion originalRotation { get; private set; }
        public new AudioSource audioSource { get; private set; }

        public MeleeWeaponData Data => data;
        public Camera PlayerCamera
        {
            get => playerCamera;
            set => playerCamera = value;
        }
        public int WeaponLevel => weaponLevel;
        public float ScaledDamage => scalingSystem.GetScaledDamage(data.damage, weaponLevel);
        public float ScaledSwingTime => scalingSystem.GetScaledSwingTime(data.swingTime, weaponLevel);
        public int ScaledDurability => scalingSystem.GetScaledDurability(data.durability, weaponLevel);

        public WeaponSystem WeaponSystem => weaponSystem;
        public EnemyHitEvent OnEnemyHit => onEnemyHit;

        protected override void Start()
        {
            base.Start();

            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            stateMachine = new MeleeWeaponStateMachine(this);
            stateMachine.Initialize();

            SetWeaponVisibility(GetCurrentDurability() > 0);
        }

        void Update()
        {
            stateMachine?.Update();
        }

        public void Use()
        {
            stateMachine?.HandleInput();
        }

        public bool CanAttack() => GetCurrentDurability() > 0;
        public bool IsSwinging() => stateMachine?.IsInState<MeleeSwingingState>() ?? false;

        public int GetCurrentDurability() => weaponSystem?.currentDurability ?? 0;
        public int GetMaxDurability() => weaponSystem?.maxDurability ?? 0;

        public void SetWeaponLevel(int level)
        {
            int oldMaxDurability = GetMaxDurability();
            weaponLevel = Mathf.Max(1, level);

            if (oldMaxDurability > 0)
            {
                float durabilityRatio = (float)GetCurrentDurability() / oldMaxDurability;
                weaponSystem.currentDurability = Mathf.RoundToInt(ScaledDurability * durabilityRatio);
                weaponSystem.maxDurability = ScaledDurability;
            }
            else
            {
                weaponSystem.currentDurability = ScaledDurability;
                weaponSystem.maxDurability = ScaledDurability;
            }

            if (GetCurrentDurability() > 0)
            {
                SetWeaponVisibility(true);
            }
        }

        public void LevelUp()
        {
            SetWeaponLevel(weaponLevel + 1);
        }

        public void SetDurability(int durability)
        {
            int previousDurability = GetCurrentDurability();
            weaponSystem.currentDurability = Mathf.Clamp(durability, 0, ScaledDurability);
            weaponSystem.maxDurability = ScaledDurability;

            if (previousDurability <= 0 && GetCurrentDurability() > 0)
            {
                SetWeaponVisibility(true);
            }
            else if (previousDurability > 0 && GetCurrentDurability() <= 0)
            {
                SetWeaponVisibility(false);
            }

            weaponSystem.OnDurabilityChanged?.Raise();
        }

        public void RestoreDurability()
        {
            SetDurability(ScaledDurability);
        }

        public void SetWeaponVisibility(bool isVisible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isVisible;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = isVisible;
            }
        }

        public MeleeWeaponStateMachine GetStateMachine() => stateMachine;
    }
}