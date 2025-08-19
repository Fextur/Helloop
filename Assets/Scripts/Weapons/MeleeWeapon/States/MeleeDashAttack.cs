using UnityEngine;
using System.Collections.Generic;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;
using Helloop.Weapons;
using Helloop.Player;

namespace Helloop.Weapons.States
{
    public class MeleeDashAttackState : IState<MeleeWeapon>
    {
        private MeleeWeapon owner;
        private float durationSeconds;
        private float elapsed;
        private float angleDegrees;
        private HashSet<GameObject> hitTargetsThisDash;
        private float lastHitCheckTime;

        private const float HitCheckInterval = 0.05f;

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;

            var playerMovement = owner.GetComponentInParent<PlayerMovement>();
            float dashDuration = playerMovement != null ? playerMovement.dashDuration : 0.5f;
            durationSeconds = Mathf.Max(0.001f, dashDuration + 0.2f);

            angleDegrees = owner.Data.dashAngleDegrees;

            if (owner.audioSource != null && owner.Data.swingSound != null)
                owner.audioSource.PlayOneShot(owner.Data.swingSound);

            hitTargetsThisDash = new HashSet<GameObject>();
            lastHitCheckTime = 0f;
            elapsed = 0f;
        }

        public void Update(MeleeWeapon weapon)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / durationSeconds);

            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Thrust: ApplyThrustDash(t01); break;
                case MeleeAnimationType.Overhead: ApplyOverheadDash(t01); break;
                default: ApplySlashDash(t01); break;
            }

            if (elapsed - lastHitCheckTime >= HitCheckInterval)
            {
                lastHitCheckTime = elapsed;
                ProcessContinuousHits();
            }

            if (t01 >= 1f)
            {
                owner.GetStateMachine().ChangeState(new MeleeReadyState());
            }
        }

        public void OnExit(MeleeWeapon weapon)
        {
            hitTargetsThisDash?.Clear();
        }

        private void ProcessContinuousHits()
        {
            if (owner.PlayerCamera == null) return;

            Vector3 rayOrigin = owner.PlayerCamera.transform.position;
            Vector3 rayDirection = owner.PlayerCamera.transform.forward;
            float rayDistance = owner.Data.range;

            LayerMask effectiveHitMask = ~owner.Data.ignoreLayers;

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, effectiveHitMask))
            {
                Vector3 hitDir = (hit.point - rayOrigin).normalized;
                if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(hit.collider))
                {
                    GameObject rootObj = hit.collider.transform.root.gameObject;
                    if (!hitTargetsThisDash.Contains(rootObj))
                    {
                        hitTargetsThisDash.Add(rootObj);
                        ProcessHit(hit.collider, hit.point, rayDirection);
                    }
                }
            }

            RaycastHit sphereHit;
            if (Physics.SphereCast(rayOrigin, 0.4f, rayDirection, out sphereHit, rayDistance, effectiveHitMask))
            {
                Vector3 hitDir = (sphereHit.point - rayOrigin).normalized;
                if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(sphereHit.collider))
                {
                    GameObject rootObj = sphereHit.collider.transform.root.gameObject;
                    if (!hitTargetsThisDash.Contains(rootObj))
                    {
                        hitTargetsThisDash.Add(rootObj);
                        ProcessHit(sphereHit.collider, sphereHit.point, rayDirection);
                    }
                }
            }

            Collider[] nearbyColliders = Physics.OverlapSphere(rayOrigin + rayDirection * (rayDistance * 0.6f), rayDistance * 0.4f, effectiveHitMask);

            foreach (Collider col in nearbyColliders)
            {
                if (IsValidTarget(col))
                {
                    Vector3 dir = (col.bounds.center - rayOrigin).normalized;
                    if (WithinAngle(rayDirection, dir, angleDegrees))
                    {
                        GameObject rootObj = col.transform.root.gameObject;
                        if (!hitTargetsThisDash.Contains(rootObj))
                        {
                            hitTargetsThisDash.Add(rootObj);
                            Vector3 targetPoint = col.bounds.center;
                            ProcessHit(col, targetPoint, rayDirection);
                        }
                    }
                }
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
                        owner.audioSource.PlayOneShot(owner.Data.breakSound);
                    return;
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
                        owner.audioSource.PlayOneShot(owner.Data.breakSound);
                    return;
                }
            }
        }

        private bool IsValidTarget(Collider collider)
        {
            return collider.GetComponent<EnemyHealth>() != null ||
                   collider.GetComponent<DestructibleObject>() != null;
        }

        private static bool WithinAngle(Vector3 forward, Vector3 dir, float angleDegrees)
            => Vector3.Angle(forward, dir) <= angleDegrees * 0.5f;



        private void ApplyThrustDash(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3( 0f,  0f,  0f) },
                new(){ timePercent=0.1f, pos=new Vector3(0.120f, -0.015f,  0.012f), rotEuler=new Vector3( 20f,  5f,  0f) },
                new(){ timePercent=0.85f, pos=new Vector3(0.150f, -0.020f,  0.020f), rotEuler=new Vector3( 30f,  8f,  0f) },
                new(){ timePercent=1.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,  0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadDash(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3( 0f,  0f,  0f) },
                new(){ timePercent=0.1f, pos=new Vector3(0.125f, -0.018f,  0.015f), rotEuler=new Vector3( 25f,  7f,  0f) },
                new(){ timePercent=0.85f, pos=new Vector3(0.163f, -0.025f,  0.024f), rotEuler=new Vector3( 35f, 10f,  0f) },
                new(){ timePercent=1.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,  0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashDash(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3( 0f,  0f,  0f) },
                new(){ timePercent=0.1f, pos=new Vector3(0.120f, -0.018f,  0.015f), rotEuler=new Vector3( 25f,  7f,  0f) },
                new(){ timePercent=0.85f, pos=new Vector3(0.163f, -0.025f,  0.024f), rotEuler=new Vector3( 35f, 10f,  0f) },
                new(){ timePercent=1.00f, pos=new Vector3(0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,  0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}