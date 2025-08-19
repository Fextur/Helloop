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

        // State machine (routing happens there; states only animate & resolve hits)
        private MeleeWeaponStateMachine stateMachine;

        // Expose original local pose to states (hide base protected fields)
        public new Vector3 originalPosition { get; private set; }
        public new Quaternion originalRotation { get; private set; }
        public new AudioSource audioSource { get; private set; }

        // Public accessors used across the codebase
        public MeleeWeaponData Data => data;
        public Camera PlayerCamera => playerCamera;
        public int WeaponLevel => weaponLevel;

        public float ScaledDamage => scalingSystem != null
            ? scalingSystem.GetScaledDamage(data.damage, weaponLevel)
            : data.damage;

        public float ScaledSwingTime => scalingSystem != null
            ? scalingSystem.GetScaledSwingTime(data.swingTime, weaponLevel)
            : data.swingTime;

        public int ScaledDurability => scalingSystem != null
            ? scalingSystem.GetScaledDurability(data.durability, weaponLevel)
            : data.durability;

        public WeaponSystem WeaponSystem => weaponSystem;
        public EnemyHitEvent OnEnemyHit => onEnemyHit;

        // ---------------- Unity lifecycle ----------------
        protected override void Start()
        {
            base.Start();

            // Cache current local pose as base for in-hand animations
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;

            // Ensure audio source (read by states and Broken state)
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            // Initialize melee state machine (router)
            stateMachine = new MeleeWeaponStateMachine(this);
            stateMachine.Initialize();

            // Ensure visibility if we have durability
            if (GetCurrentDurability() > 0)
                SetWeaponVisibility(true);
        }

        private void Update()
        {
            stateMachine?.Update();
        }

        // Called by WeaponManager / input layer on attack press
        public void Use()
        {
            // Arms the press; the state machine decides Light/Heavy/Dash
            stateMachine?.HandleInput();
        }

        // ---------------- Durability & Level helpers ----------------
        public int GetCurrentDurability() => weaponSystem != null ? weaponSystem.currentDurability : 0;
        public int GetMaxDurability() => weaponSystem != null ? weaponSystem.maxDurability : 0;

        // ✅ Restored: used by WeaponManager
        public void SetWeaponLevel(int level)
        {
            int oldMax = GetMaxDurability();
            weaponLevel = Mathf.Max(1, level);

            if (weaponSystem == null) return;

            if (oldMax > 0)
            {
                // Preserve ratio when max changes with level
                float ratio = oldMax > 0 ? (float)GetCurrentDurability() / oldMax : 0f;
                weaponSystem.currentDurability = Mathf.RoundToInt(ScaledDurability * ratio);
                weaponSystem.maxDurability = ScaledDurability;
            }
            else
            {
                weaponSystem.currentDurability = ScaledDurability;
                weaponSystem.maxDurability = ScaledDurability;
            }

            if (GetCurrentDurability() > 0)
                SetWeaponVisibility(true);
        }

        public void LevelUp()
        {
            SetWeaponLevel(weaponLevel + 1);
        }

        // ✅ Restored: used by WeaponManager
        public void SetDurability(int durability)
        {
            if (weaponSystem == null) return;

            int previous = GetCurrentDurability();
            weaponSystem.currentDurability = Mathf.Clamp(durability, 0, ScaledDurability);
            weaponSystem.maxDurability = ScaledDurability;

            // If we repaired from 0 -> >0, ensure mesh is visible again
            if (previous <= 0 && GetCurrentDurability() > 0)
                SetWeaponVisibility(true);
        }

        // ---------------- Visibility ----------------
        public void SetWeaponVisibility(bool isVisible)
        {
            // Renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
                r.enabled = isVisible;

            // Colliders
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
                c.enabled = isVisible;
        }

        // ---------------- State machine access ----------------
        public MeleeWeaponStateMachine GetStateMachine() => stateMachine;

        // Utility used elsewhere (unchanged public surface)
        public bool IsSwinging()
        {
            if (stateMachine == null) return false;
            return stateMachine.IsInState<MeleeSwingLightState>()
                || stateMachine.IsInState<MeleeSwipeHeavyState>()
                || stateMachine.IsInState<MeleeDashAttackState>();
        }
    }
}
