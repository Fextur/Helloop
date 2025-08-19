using UnityEngine;
using System.Collections.Generic;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;
using Helloop.Weapons;

namespace Helloop.Weapons.States
{
    public class MeleeSwipeHeavyState : IState<MeleeWeapon>
    {
        private MeleeWeapon owner;
        private float durationSeconds;
        private float elapsed;
        private float angleDegrees;
        private bool hitWindowProcessed;
        private float windowStart;
        private float windowEnd;

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;
            durationSeconds = Mathf.Max(0.001f, owner.Data.swipeTime);
            angleDegrees = owner.Data.swipeAngleDegrees;

            float damagePoint = GetDamagePointForAnimation();
            windowStart = Mathf.Max(0f, damagePoint - 0.05f);
            windowEnd = Mathf.Min(1f, damagePoint + 0.3f);

            if (owner.audioSource != null && owner.Data.swingSound != null)
                owner.audioSource.PlayOneShot(owner.Data.swingSound);

            hitWindowProcessed = false;
            elapsed = 0f;
        }

        public void Update(MeleeWeapon weapon)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / durationSeconds);

            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Thrust: ApplyThrustHeavy(t01); break;
                case MeleeAnimationType.Overhead: ApplyOverheadHeavy(t01); break;
                default: ApplySlashHeavy(t01); break;
            }

            if (!hitWindowProcessed && t01 >= windowStart && t01 <= windowEnd)
            {
                hitWindowProcessed = true;
                ResolveMultiHit();
            }

            if (t01 >= 1f)
            {
                owner.GetStateMachine().ChangeState(new MeleeReadyState());
            }
        }

        public void OnExit(MeleeWeapon weapon) { }

        private void ResolveMultiHit()
        {
            if (owner.PlayerCamera == null) return;

            Vector3 rayOrigin = owner.PlayerCamera.transform.position;
            Vector3 rayDirection = owner.PlayerCamera.transform.forward;
            float rayDistance = owner.Data.range;

            LayerMask effectiveHitMask = ~owner.Data.ignoreLayers;
            HashSet<GameObject> hitTargets = new HashSet<GameObject>();

            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, effectiveHitMask))
            {
                Vector3 hitDir = (hit.point - rayOrigin).normalized;
                if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(hit.collider))
                {
                    GameObject rootObj = hit.collider.transform.root.gameObject;
                    if (!hitTargets.Contains(rootObj))
                    {
                        hitTargets.Add(rootObj);
                        ProcessHit(hit.collider, hit.point, rayDirection);
                    }
                }
            }

            RaycastHit sphereHit;
            if (Physics.SphereCast(rayOrigin, 0.5f, rayDirection, out sphereHit, rayDistance, effectiveHitMask))
            {
                Vector3 hitDir = (sphereHit.point - rayOrigin).normalized;
                if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(sphereHit.collider))
                {
                    GameObject rootObj = sphereHit.collider.transform.root.gameObject;
                    if (!hitTargets.Contains(rootObj))
                    {
                        hitTargets.Add(rootObj);
                        ProcessHit(sphereHit.collider, sphereHit.point, rayDirection);
                    }
                }
            }

            Collider[] nearbyColliders = Physics.OverlapSphere(rayOrigin + rayDirection * (rayDistance * 0.7f), rayDistance * 0.8f, effectiveHitMask);

            foreach (Collider col in nearbyColliders)
            {
                if (IsValidTarget(col))
                {
                    Vector3 dir = (col.bounds.center - rayOrigin).normalized;
                    if (WithinAngle(rayDirection, dir, angleDegrees))
                    {
                        GameObject rootObj = col.transform.root.gameObject;
                        if (!hitTargets.Contains(rootObj))
                        {
                            hitTargets.Add(rootObj);
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

        private float GetDamagePointForAnimation()
        {
            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Slash:
                    return 0.5f;
                case MeleeAnimationType.Overhead:
                    return 0.65f;
                case MeleeAnimationType.Thrust:
                    return 0.45f;
                case MeleeAnimationType.Swing:
                    return 0.5f;
                default:
                    return 0.5f;
            }
        }

        private static bool WithinAngle(Vector3 forward, Vector3 dir, float angleDegrees)
            => Vector3.Angle(forward, dir) <= angleDegrees * 0.5f;



        private void ApplyThrustHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) },
                new(){ timePercent=0.20f, pos=new Vector3( 0.060f,  0.020f, -0.020f), rotEuler=new Vector3( -3f,  35f, -15f) },
                new(){ timePercent=0.40f, pos=new Vector3( 0.100f,  0.030f, -0.040f), rotEuler=new Vector3( -6f,  60f, -25f) },
                new(){ timePercent=0.60f, pos=new Vector3(-0.040f, -0.020f,  0.200f), rotEuler=new Vector3( -2f, -60f,  10f) },
                new(){ timePercent=0.80f, pos=new Vector3(-0.080f, -0.015f,  0.180f), rotEuler=new Vector3(  5f, -80f,  15f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) },
                new(){ timePercent=0.20f, pos=new Vector3( 0.080f,  0.020f, -0.030f), rotEuler=new Vector3( -5f,  45f, -20f) },
                new(){ timePercent=0.40f, pos=new Vector3( 0.120f,  0.040f, -0.050f), rotEuler=new Vector3( -8f,  75f, -30f) },
                new(){ timePercent=0.60f, pos=new Vector3(-0.050f, -0.030f,  0.240f), rotEuler=new Vector3( -3f, -70f,  12f) },
                new(){ timePercent=0.80f, pos=new Vector3(-0.090f, -0.020f,  0.200f), rotEuler=new Vector3(  6f, -100f, 20f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) },
                new(){ timePercent=0.20f, pos=new Vector3( 0.070f, -0.045f,  0.066f), rotEuler=new Vector3( -5f,  28f, -38f) },
                new(){ timePercent=0.45f, pos=new Vector3(-0.015f, -0.112f,  0.164f), rotEuler=new Vector3(  -5f, -37f, -85f) },
                new(){ timePercent=0.60f, pos=new Vector3(-0.030f, -0.095f,  0.220f), rotEuler=new Vector3( -7f, -57f, -90f) },
                new(){ timePercent=0.80f, pos=new Vector3(-0.060f, -0.040f,  0.180f), rotEuler=new Vector3(  8f, -85f, -25f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.000f,  0.000f,  0.000f), rotEuler=new Vector3(  0f,   0f,   0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}