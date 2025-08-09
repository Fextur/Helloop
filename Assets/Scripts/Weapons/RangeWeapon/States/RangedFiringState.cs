using UnityEngine;
using System.Collections;
using Helloop.StateMachines;
using Helloop.Data;

namespace Helloop.Weapons.States
{
    public class RangedFiringState : IState<RangedWeapon>, IRangedInputHandler
    {
        private RangedWeapon owner;
        private Coroutine firingCoroutine;
        private bool isFiring;
        private bool shouldContinueFiring;

        public void OnEnter(RangedWeapon weapon)
        {
            owner = weapon;
            isFiring = false;
            shouldContinueFiring = true;

            StartFiring();
        }

        public void Update(RangedWeapon weapon)
        {
            if (!isFiring && !shouldContinueFiring)
            {
                weapon.GetStateMachine().ChangeState(new RangedReadyState());
            }
        }

        public void OnExit(RangedWeapon weapon)
        {
            StopFiring();
        }

        public void HandleFireInput(RangedWeapon weapon)
        {
            if (weapon.Data.fireMode == FireMode.SemiAutomatic || weapon.Data.fireMode == FireMode.Burst)
            {
                if (!isFiring)
                {
                    shouldContinueFiring = true;
                    StartFiring();
                }
            }
            else if (weapon.Data.fireMode == FireMode.Automatic)
            {
                shouldContinueFiring = true;
                if (!isFiring)
                {
                    StartFiring();
                }
            }
        }

        public void HandleStopFireInput(RangedWeapon weapon)
        {
            shouldContinueFiring = false;

            if (weapon.Data.fireMode == FireMode.Automatic)
            {
                StopFiring();
            }
        }

        private void StartFiring()
        {
            if (isFiring || !owner.CanFire()) return;

            isFiring = true;

            switch (owner.Data.fireMode)
            {
                case FireMode.SemiAutomatic:
                    firingCoroutine = owner.StartCoroutine(SemiAutomaticFire());
                    break;
                case FireMode.Burst:
                    firingCoroutine = owner.StartCoroutine(BurstFire());
                    break;
                case FireMode.Automatic:
                    firingCoroutine = owner.StartCoroutine(AutomaticFire());
                    break;
            }
        }

        private void StopFiring()
        {
            shouldContinueFiring = false;
            isFiring = false;

            if (firingCoroutine != null)
            {
                owner.StopCoroutine(firingCoroutine);
                firingCoroutine = null;
            }
        }

        private IEnumerator SemiAutomaticFire()
        {
            FireSingleShot();
            yield return null;

            isFiring = false;
            shouldContinueFiring = false;
        }

        private IEnumerator BurstFire()
        {
            int burstCount = owner.Data.burstCount;
            float burstDelay = owner.Data.fireRate;

            for (int i = 0; i < burstCount && shouldContinueFiring; i++)
            {
                if (owner.CurrentClip <= 0) break;

                FireSingleShot();

                if (owner.CurrentClip <= 0) break;

                if (i < burstCount - 1)
                {
                    yield return new WaitForSeconds(burstDelay);
                }
            }

            isFiring = false;
            shouldContinueFiring = false;
        }

        private IEnumerator AutomaticFire()
        {
            float fireRate = owner.Data.fireRate;

            while (shouldContinueFiring && owner.CurrentClip > 0)
            {
                FireSingleShot();

                if (owner.CurrentClip <= 0) break;

                yield return new WaitForSeconds(fireRate);
            }

            isFiring = false;
        }

        private void FireSingleShot()
        {
            if (owner.CurrentClip <= 0) return;

            owner.WeaponSystem.UseAmmo(1);

            if (owner.Data.fireSound != null && owner.audioSource != null)
            {
                owner.audioSource.PlayOneShot(owner.Data.fireSound);
            }

            if (owner.Data.projectilePrefab != null && owner.FirePoint != null)
            {
                GameObject projObj = Object.Instantiate(owner.Data.projectilePrefab, owner.FirePoint.position, owner.FirePoint.rotation);
                if (projObj.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Initialize(owner.Data.projectileSpeed, owner.ScaledDamage, owner.Data.falloffDistance);
                }
            }

            owner.StartCoroutine(MuzzleFlashAnimation());
        }

        private IEnumerator MuzzleFlashAnimation()
        {
            float recoilTime = 0.1f;
            Vector3 recoilPosition = owner.originalPosition + new Vector3(0, 0.02f, -0.05f);
            Quaternion recoilRotation = owner.originalRotation * Quaternion.Euler(-5f, 0, 0);

            float elapsedTime = 0f;
            while (elapsedTime < recoilTime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / recoilTime;
                float curve = 1f - normalizedTime;

                owner.transform.localPosition = Vector3.Lerp(owner.originalPosition, recoilPosition, curve);
                owner.transform.localRotation = Quaternion.Lerp(owner.originalRotation, recoilRotation, curve);

                yield return null;
            }

            owner.transform.localPosition = owner.originalPosition;
            owner.transform.localRotation = owner.originalRotation;
        }
    }
}