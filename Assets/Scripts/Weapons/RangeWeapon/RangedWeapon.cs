using UnityEngine;
using System.Collections;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Weapons.States;

namespace Helloop.Weapons
{
    public class RangedWeapon : WeaponBase, IRangeWeapon
    {
        [SerializeField] private RangedWeaponData data;

        [Header("System References")]
        public WeaponSystem weaponSystem;
        public ScalingSystem scalingSystem;
        public Transform firePoint;

        [Header("Level System")]
        public int weaponLevel = 1;

        private RangedWeaponStateMachine stateMachine;

        public new Vector3 originalPosition { get; private set; }
        public new Quaternion originalRotation { get; private set; }
        public new AudioSource audioSource { get; private set; }

        public RangedWeaponData Data
        {
            get => data;
            set => data = value;
        }

        public float ScaledDamage => scalingSystem.GetScaledDamage(Data.damage, weaponLevel);
        public float ScaledReloadTime => scalingSystem.GetScaledReloadTime(Data.reloadTime, weaponLevel);
        public int ScaledMaxAmmo => scalingSystem.GetScaledMaxAmmo(Data.maxAmmoSize, Data.clipSize, weaponLevel);

        public WeaponSystem WeaponSystem => weaponSystem;
        public Transform FirePoint => firePoint;
        public int WeaponLevel => weaponLevel;

        public int CurrentClip => weaponSystem?.currentClipAmmo ?? 0;
        public int CurrentAmmo => weaponSystem?.currentAmmo ?? 0;

        public bool needsRecoilReturn = false;
        public float recoilReturnTime = 0f;

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

            stateMachine = new RangedWeaponStateMachine(this);
            stateMachine.Initialize();
        }

        void Update()
        {
            stateMachine?.Update();

            if (needsRecoilReturn && Time.time >= recoilReturnTime)
            {
                transform.localPosition = originalPosition;
                transform.localRotation = originalRotation;
                needsRecoilReturn = false;
            }
        }

        public void Use()
        {
            stateMachine?.HandleFireInput();
        }

        public void StopUse()
        {
            stateMachine?.HandleStopFireInput();
        }

        public IEnumerator Reload()
        {
            return stateMachine?.TriggerReload() ?? null;
        }

        public bool CanFire() => CurrentClip > 0;
        public bool CanReload() => CurrentClip < Data.clipSize && CurrentAmmo > 0;
        public bool IsReloading() => stateMachine?.IsInState<RangedReloadingState>() ?? false;
        public bool IsFiring() => stateMachine?.IsInState<RangedFiringState>() ?? false;

        public void SetWeaponLevel(int level)
        {
            weaponLevel = Mathf.Max(1, level);
        }

        public void LevelUp()
        {
            SetWeaponLevel(weaponLevel + 1);
        }
        public void RefillAmmo()
        {
            weaponSystem.RefillAmmo();
        }

        public RangedWeaponStateMachine GetStateMachine() => stateMachine;
    }
}