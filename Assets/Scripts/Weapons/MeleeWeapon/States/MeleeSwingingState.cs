using UnityEngine;
using System.Collections;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;

namespace Helloop.Weapons.States
{
    public class MeleeSwingingState : IState<MeleeWeapon>
    {
        private MeleeWeapon owner;
        private Coroutine swingCoroutine;
        private bool hasDealtDamage;

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;
            hasDealtDamage = false;

            if (weapon.Data.swingSound != null && weapon.audioSource != null)
            {
                weapon.audioSource.PlayOneShot(weapon.Data.swingSound);
            }

            swingCoroutine = weapon.StartCoroutine(SwingAnimationSequence());
        }

        public void Update(MeleeWeapon weapon)
        {
        }

        public void OnExit(MeleeWeapon weapon)
        {
            if (swingCoroutine != null)
            {
                weapon.StopCoroutine(swingCoroutine);
                swingCoroutine = null;
            }
        }

        private IEnumerator SwingAnimationSequence()
        {
            float animationTime = owner.ScaledSwingTime * 0.7f;
            float elapsedTime = 0f;

            while (elapsedTime < animationTime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / animationTime;

                if (!hasDealtDamage && normalizedTime >= GetDamagePointForAnimation())
                {
                    hasDealtDamage = true;
                    DealDamage();
                }

                ApplyAnimationForType(normalizedTime);

                yield return null;
            }

            owner.transform.localPosition = owner.originalPosition;
            owner.transform.localRotation = owner.originalRotation;

            owner.GetStateMachine().ChangeState(new MeleeReadyState());
        }

        private float GetDamagePointForAnimation()
        {
            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Slash:
                    return 0.4f;
                case MeleeAnimationType.Overhead:
                    return 0.6f;
                case MeleeAnimationType.Thrust:
                    return 0.3f;
                case MeleeAnimationType.Punch:
                    return 0.2f;
                case MeleeAnimationType.Swing:
                    return 0.4f;
                default:
                    return 0.4f;
            }
        }

        private void DealDamage()
        {
            if (owner.PlayerCamera == null)
                return;

            Vector3 rayOrigin = owner.PlayerCamera.transform.position;
            Vector3 rayDirection = owner.PlayerCamera.transform.forward;
            float rayDistance = owner.Data.range;

            LayerMask effectiveHitMask = ~owner.Data.ignoreLayers;

            RaycastHit hit;
            bool didHit = false;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, effectiveHitMask))
            {
                didHit = true;
            }

            if (!didHit || !IsValidTarget(hit.collider))
            {
                RaycastHit sphereHit;
                if (Physics.SphereCast(rayOrigin, 0.3f, rayDirection, out sphereHit, rayDistance, effectiveHitMask))
                {
                    if (IsValidTarget(sphereHit.collider))
                    {
                        hit = sphereHit;
                        didHit = true;
                    }
                }
            }

            if (!didHit || !IsValidTarget(hit.collider))
            {
                Collider[] nearbyColliders = Physics.OverlapSphere(rayOrigin + rayDirection * (rayDistance * 0.7f), rayDistance * 0.5f, effectiveHitMask);

                float closestDistance = float.MaxValue;
                Collider bestTarget = null;

                foreach (Collider col in nearbyColliders)
                {
                    if (IsValidTarget(col))
                    {
                        float distance = Vector3.Distance(rayOrigin, col.bounds.center);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            bestTarget = col;
                        }
                    }
                }

                if (bestTarget != null)
                {
                    Vector3 targetPoint = bestTarget.bounds.center;
                    ProcessHit(bestTarget, targetPoint, rayDirection);
                    return;
                }
            }

            if (didHit)
            {
                ProcessHit(hit.collider, hit.point, rayDirection);
            }
        }

        private void ProcessHit(Collider hitCollider, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (hitCollider.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.TakeDamage(owner.ScaledDamage);

                owner.WeaponSystem.UseDurability(1);

                if (owner.OnEnemyHit != null)
                {
                    owner.OnEnemyHit.Raise(enemyHealth);
                }

                if (owner.GetCurrentDurability() <= 0)
                {
                    owner.SetWeaponVisibility(false);

                    if (owner.Data.breakSound != null && owner.audioSource != null)
                    {
                        owner.audioSource.PlayOneShot(owner.Data.breakSound);
                    }
                }
            }

            else if (hitCollider.TryGetComponent<DestructibleObject>(out var destructible))
            {
                destructible.TakeDamage(owner.ScaledDamage, hitPoint, hitDirection);

                owner.WeaponSystem.UseDurability(1);

                if (owner.GetCurrentDurability() <= 0)
                {
                    owner.SetWeaponVisibility(false);

                    if (owner.Data.breakSound != null && owner.audioSource != null)
                    {
                        owner.audioSource.PlayOneShot(owner.Data.breakSound);
                    }
                }
            }
        }

        private bool IsValidTarget(Collider collider)
        {
            return collider.GetComponent<EnemyHealth>() != null ||
                   collider.GetComponent<DestructibleObject>() != null;
        }

        private void ApplyAnimationForType(float normalizedTime)
        {
            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Slash:
                    ApplySlashAnimation(normalizedTime);
                    break;
                case MeleeAnimationType.Overhead:
                    ApplyOverheadAnimation(normalizedTime);
                    break;
                case MeleeAnimationType.Thrust:
                    ApplyThrustAnimation(normalizedTime);
                    break;
                case MeleeAnimationType.Punch:
                    ApplyPunchAnimation(normalizedTime);
                    break;
                case MeleeAnimationType.Swing:
                    ApplySwingAnimation(normalizedTime);
                    break;
            }
        }

        private void ApplySlashAnimation(float normalizedTime)
        {
            float motionScale = owner.Data.useWideSlash ? 1.6f : 1.0f;

            if (normalizedTime < 0.2f)
            {
                float attackCurve = normalizedTime / 0.2f;
                if (owner.Data.useWideSlash)
                {
                    attackCurve = Mathf.Sin(attackCurve * Mathf.PI * 0.5f);
                }

                Vector3 windupRotation = new Vector3(-15f, 10f, -25f) * attackCurve * motionScale;
                Vector3 windupPosition = owner.originalPosition + new Vector3(0.25f, 0.15f, -0.1f) * attackCurve * motionScale;

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(windupRotation);
                owner.transform.localPosition = windupPosition;
            }
            else if (normalizedTime < 0.6f)
            {
                float strikeProgress = (normalizedTime - 0.2f) / 0.4f;

                Vector3 slashRotation = Vector3.Lerp(
                    new Vector3(-15f, 10f, -25f) * motionScale,
                    new Vector3(20f, -15f, 35f) * motionScale,
                    strikeProgress
                );

                Vector3 slashPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0.25f, 0.15f, -0.1f) * motionScale,
                    owner.originalPosition + new Vector3(-0.35f, -0.25f, 0.25f) * motionScale,
                    strikeProgress
                );

                float strikeCurve = Mathf.Sin(strikeProgress * Mathf.PI);
                slashPosition += Vector3.forward * (strikeCurve * 0.3f * motionScale);

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(slashRotation);
                owner.transform.localPosition = slashPosition;
            }
            else
            {
                float returnProgress = (normalizedTime - 0.6f) / 0.4f;
                float smoothReturn = owner.Data.useWideSlash ?
                    Mathf.Sin(returnProgress * Mathf.PI * 0.5f) :
                    1f - Mathf.Pow(1f - returnProgress, 3f);

                Vector3 currentRotation = Vector3.Lerp(
                    new Vector3(20f, -15f, 35f) * motionScale,
                    Vector3.zero,
                    smoothReturn
                );

                Vector3 currentPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(-0.35f, -0.25f, 0.25f) * motionScale,
                    owner.originalPosition,
                    smoothReturn
                );

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(currentRotation);
                owner.transform.localPosition = currentPosition;
            }
        }

        private void ApplyOverheadAnimation(float normalizedTime)
        {
            if (normalizedTime < 0.3f)
            {
                float windupProgress = normalizedTime / 0.3f;
                float windupCurve = Mathf.Sin(windupProgress * Mathf.PI * 0.5f);

                Vector3 overheadRotation = new Vector3(-60f, 0f, 0f) * windupCurve;
                Vector3 overheadPosition = owner.originalPosition + new Vector3(0f, 0.4f, -0.2f) * windupCurve;

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(overheadRotation);
                owner.transform.localPosition = overheadPosition;
            }
            else if (normalizedTime < 0.7f)
            {
                float strikeProgress = (normalizedTime - 0.3f) / 0.4f;
                float strikeCurve = Mathf.Pow(strikeProgress, 2f);

                Vector3 slamRotation = Vector3.Lerp(
                    new Vector3(-60f, 0f, 0f),
                    new Vector3(45f, 0f, 0f),
                    strikeCurve
                );

                Vector3 slamPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0f, 0.4f, -0.2f),
                    owner.originalPosition + new Vector3(0f, -0.4f, 0.5f),
                    strikeCurve
                );

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(slamRotation);
                owner.transform.localPosition = slamPosition;
            }
            else
            {
                float recoveryProgress = (normalizedTime - 0.7f) / 0.3f;
                float smoothRecovery = Mathf.Sin(recoveryProgress * Mathf.PI * 0.5f);

                Vector3 currentRotation = Vector3.Lerp(
                    new Vector3(45f, 0f, 0f),
                    Vector3.zero,
                    smoothRecovery
                );

                Vector3 currentPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0f, -0.4f, 0.5f),
                    owner.originalPosition,
                    smoothRecovery
                );

                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(currentRotation);
                owner.transform.localPosition = currentPosition;
            }
        }

        private void ApplyThrustAnimation(float normalizedTime)
        {
            if (normalizedTime < 0.15f)
            {
                float windupProgress = normalizedTime / 0.15f;
                Vector3 windupPosition = owner.originalPosition + new Vector3(0f, 0f, -0.3f) * windupProgress;

                owner.transform.localPosition = windupPosition;
                owner.transform.localRotation = owner.originalRotation;
            }
            else if (normalizedTime < 0.5f)
            {
                float thrustProgress = (normalizedTime - 0.15f) / 0.35f;
                float thrustCurve = Mathf.Sin(thrustProgress * Mathf.PI);

                Vector3 thrustPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0f, 0f, -0.3f),
                    owner.originalPosition + new Vector3(0f, -0.1f, 0.8f),
                    thrustProgress
                );

                thrustPosition += Vector3.forward * (thrustCurve * 0.3f);
                Vector3 thrustRotation = new Vector3(10f, 0f, 0f) * thrustCurve;

                owner.transform.localPosition = thrustPosition;
                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(thrustRotation);
            }
            else
            {
                float returnProgress = (normalizedTime - 0.5f) / 0.5f;
                float snapBack = Mathf.Pow(returnProgress, 2f);

                Vector3 currentPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0f, -0.1f, 0.8f),
                    owner.originalPosition,
                    snapBack
                );

                Vector3 currentRotation = Vector3.Lerp(
                    new Vector3(10f, 0f, 0f),
                    Vector3.zero,
                    snapBack
                );

                owner.transform.localPosition = currentPosition;
                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(currentRotation);
            }
        }

        private void ApplyPunchAnimation(float normalizedTime)
        {
            if (normalizedTime < 0.1f)
            {
                float punchProgress = normalizedTime / 0.1f;
                float punchCurve = Mathf.Sin(punchProgress * Mathf.PI * 0.5f);

                Vector3 punchPosition = owner.originalPosition + new Vector3(0f, -0.05f, 0.4f) * punchCurve;
                Vector3 punchRotation = new Vector3(-15f, 0f, 0f) * punchCurve;

                owner.transform.localPosition = punchPosition;
                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(punchRotation);
            }
            else if (normalizedTime < 0.25f)
            {
                Vector3 holdPosition = owner.originalPosition + new Vector3(0f, -0.05f, 0.4f);
                Vector3 holdRotation = new Vector3(-15f, 0f, 0f);

                owner.transform.localPosition = holdPosition;
                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(holdRotation);
            }
            else
            {
                float returnProgress = (normalizedTime - 0.25f) / 0.75f;
                float controlledReturn = Mathf.Sin(returnProgress * Mathf.PI * 0.5f);

                Vector3 currentPosition = Vector3.Lerp(
                    owner.originalPosition + new Vector3(0f, -0.05f, 0.4f),
                    owner.originalPosition,
                    controlledReturn
                );

                Vector3 currentRotation = Vector3.Lerp(
                    new Vector3(-15f, 0f, 0f),
                    Vector3.zero,
                    controlledReturn
                );

                owner.transform.localPosition = currentPosition;
                owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(currentRotation);
            }
        }

        private void ApplySwingAnimation(float normalizedTime)
        {
            float swingCurve = Mathf.Sin(normalizedTime * Mathf.PI);
            float powerCurve = normalizedTime < 0.7f ? normalizedTime / 0.7f : 1f - ((normalizedTime - 0.7f) / 0.3f);

            Vector3 swingRotation = new Vector3(0, Mathf.Lerp(45f, -30f, swingCurve), 0);
            Vector3 swingPosition = owner.originalPosition + new Vector3(
                Mathf.Lerp(-0.2f, 0.2f, swingCurve),
                -0.05f * powerCurve,
                0.15f * powerCurve
            );

            owner.transform.localRotation = owner.originalRotation * Quaternion.Euler(swingRotation);
            owner.transform.localPosition = swingPosition;
        }
    }
}