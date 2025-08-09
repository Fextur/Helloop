using UnityEngine;
using System.Collections;
using Helloop.StateMachines;

namespace Helloop.Weapons.States
{
    public class RangedReloadingState : IState<RangedWeapon>, IRangedInputHandler
    {
        private RangedWeapon owner;
        private Coroutine reloadCoroutine;

        public void OnEnter(RangedWeapon weapon)
        {
            owner = weapon;

            reloadCoroutine = weapon.StartCoroutine(ReloadSequence());
        }

        public void Update(RangedWeapon weapon)
        {
        }

        public void OnExit(RangedWeapon weapon)
        {
            if (reloadCoroutine != null)
            {
                weapon.StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }
        }

        public void HandleFireInput(RangedWeapon weapon)
        {
        }

        public void HandleStopFireInput(RangedWeapon weapon)
        {
        }

        private IEnumerator ReloadSequence()
        {
            if (owner.CurrentClip >= owner.Data.clipSize || owner.CurrentAmmo <= 0)
            {
                TransitionAfterReload();
                yield break;
            }

            if (owner.Data.reloadSound != null && owner.audioSource != null)
            {
                owner.audioSource.PlayOneShot(owner.Data.reloadSound);
            }

            Coroutine animationCoroutine = owner.StartCoroutine(ReloadAnimation());

            yield return new WaitForSeconds(owner.ScaledReloadTime);

            int needed = owner.Data.clipSize - owner.CurrentClip;
            int toReload = Mathf.Min(needed, owner.CurrentAmmo);

            if (toReload > 0)
            {
                owner.WeaponSystem.currentClipAmmo += toReload;
                owner.WeaponSystem.currentAmmo -= toReload;
                owner.WeaponSystem.OnAmmoChanged?.Raise();
            }

            yield return animationCoroutine;

            TransitionAfterReload();
        }

        private void TransitionAfterReload()
        {
            if (owner.CurrentClip > 0)
            {
                owner.GetStateMachine().ChangeState(new RangedReadyState());
            }
            else
            {
                owner.GetStateMachine().ChangeState(new RangedEmptyState());
            }
        }

        private IEnumerator ReloadAnimation()
        {
            float animationTime = owner.ScaledReloadTime * 0.9f;
            float elapsedTime = 0f;

            Vector3 reloadStartPosition = owner.originalPosition;
            Vector3 reloadMidPosition = owner.originalPosition + new Vector3(0, -0.3f, 0.1f);
            Vector3 reloadEndPosition = owner.originalPosition;

            Quaternion reloadStartRotation = owner.originalRotation;
            Quaternion reloadMidRotation = owner.originalRotation * Quaternion.Euler(30f, 0, 0);
            Quaternion reloadEndRotation = owner.originalRotation;

            while (elapsedTime < animationTime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / animationTime;

                Vector3 currentPosition;
                Quaternion currentRotation;

                if (normalizedTime < 0.5f)
                {
                    float t = normalizedTime * 2f;
                    currentPosition = Vector3.Lerp(reloadStartPosition, reloadMidPosition, t);
                    currentRotation = Quaternion.Lerp(reloadStartRotation, reloadMidRotation, t);
                }
                else
                {
                    float t = (normalizedTime - 0.5f) * 2f;
                    currentPosition = Vector3.Lerp(reloadMidPosition, reloadEndPosition, t);
                    currentRotation = Quaternion.Lerp(reloadMidRotation, reloadEndRotation, t);
                }

                owner.transform.localPosition = currentPosition;
                owner.transform.localRotation = currentRotation;

                yield return null;
            }

            owner.transform.localPosition = owner.originalPosition;
            owner.transform.localRotation = owner.originalRotation;
        }
    }
}