using UnityEngine;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;
using Helloop.Weapons;

namespace Helloop.Weapons.States
{
    public class MeleeSwingLightState : IState<MeleeWeapon>
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
            durationSeconds = Mathf.Max(0.001f, owner.Data.swingTime);
            angleDegrees = owner.Data.swingAngleDegrees;

            float damagePoint = GetDamagePointForAnimation();
            windowStart = Mathf.Max(0f, damagePoint - 0.1f);
            windowEnd = Mathf.Min(1f, damagePoint + 0.2f);

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
                case MeleeAnimationType.Thrust: ApplyThrustLight(t01); break;
                case MeleeAnimationType.Overhead: ApplyOverheadLight(t01); break;
                default: ApplySlashLight(t01); break;
            }

            if (!hitWindowProcessed && t01 >= windowStart && t01 <= windowEnd)
            {
                hitWindowProcessed = true;
                ResolveSingleHit();
            }

            if (t01 >= 1f)
            {
                owner.GetStateMachine().ChangeState(new MeleeReadyState());
            }
        }

        public void OnExit(MeleeWeapon weapon) { }

        private void ResolveSingleHit()
        {
            if (owner.PlayerCamera == null) return;

            Vector3 rayOrigin = owner.PlayerCamera.transform.position;
            Vector3 rayDirection = owner.PlayerCamera.transform.forward;
            float rayDistance = owner.Data.range;

            LayerMask effectiveHitMask = ~owner.Data.ignoreLayers;

            RaycastHit hit;
            bool didHit = false;

            if (Physics.Raycast(rayOrigin, rayDirection, out hit, rayDistance, effectiveHitMask))
            {
                Vector3 hitDir = (hit.point - rayOrigin).normalized;
                if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(hit.collider))
                {
                    didHit = true;
                }
            }

            if (!didHit)
            {
                RaycastHit sphereHit;
                if (Physics.SphereCast(rayOrigin, 0.3f, rayDirection, out sphereHit, rayDistance, effectiveHitMask))
                {
                    Vector3 hitDir = (sphereHit.point - rayOrigin).normalized;
                    if (WithinAngle(rayDirection, hitDir, angleDegrees) && IsValidTarget(sphereHit.collider))
                    {
                        hit = sphereHit;
                        didHit = true;
                    }
                }
            }

            if (!didHit)
            {
                Collider[] nearbyColliders = Physics.OverlapSphere(rayOrigin + rayDirection * (rayDistance * 0.7f), rayDistance * 0.5f, effectiveHitMask);

                float closestDistance = float.MaxValue;
                Collider bestTarget = null;

                foreach (Collider col in nearbyColliders)
                {
                    if (IsValidTarget(col))
                    {
                        Vector3 dir = (col.bounds.center - rayOrigin).normalized;
                        if (WithinAngle(rayDirection, dir, angleDegrees))
                        {
                            float distance = Vector3.Distance(rayOrigin, col.bounds.center);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                bestTarget = col;
                            }
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
                        owner.audioSource.PlayOneShot(owner.Data.breakSound);
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
                    return 0.4f;
                case MeleeAnimationType.Overhead:
                    return 0.6f;
                case MeleeAnimationType.Thrust:
                    return 0.3f;
                case MeleeAnimationType.Swing:
                    return 0.4f;
                default:
                    return 0.4f;
            }
        }

        private static bool WithinAngle(Vector3 forward, Vector3 dir, float angleDegrees)
            => Vector3.Angle(forward, dir) <= angleDegrees * 0.5f;



        private void ApplyThrustLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f, 0f, 0f) },
                new(){ timePercent=0.25f, pos=new Vector3(0f,  0.06f, -0.04f), rotEuler=new Vector3(-35f, -10f, 8f) },
                new(){ timePercent=0.35f, pos=new Vector3(0f,  0.10f, -0.06f), rotEuler=new Vector3(-50f, -8f, 12f) },
                new(){ timePercent=0.55f, pos=new Vector3(0f, -0.01f, 0.30f), rotEuler=new Vector3(10f, 3f, -3f) },
                new(){ timePercent=0.75f, pos=new Vector3(0f, -0.02f, 0.24f), rotEuler=new Vector3(18f, 5f, -2f) },
                new(){ timePercent=1.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f, 0f, 0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,  0f,  0f) },
                new(){ timePercent=0.25f, pos=new Vector3(-0.02f, 0.08f, -0.05f), rotEuler=new Vector3(-45f, -15f, 10f) },
                new(){ timePercent=0.35f, pos=new Vector3(-0.01f, 0.12f, -0.08f), rotEuler=new Vector3(-65f, -10f, 15f) },
                new(){ timePercent=0.55f, pos=new Vector3( 0.02f,-0.02f,  0.35f), rotEuler=new Vector3( 15f,   5f, -5f) },
                new(){ timePercent=0.75f, pos=new Vector3( 0.01f,-0.04f,  0.28f), rotEuler=new Vector3( 25f,   8f, -3f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,   0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
                new(){ timePercent=0.25f, pos=new Vector3(-0.02f, 0.08f, -0.05f), rotEuler=new Vector3(-45f, -15f, 10f) },
                new(){ timePercent=0.35f, pos=new Vector3(-0.01f, 0.12f, -0.08f), rotEuler=new Vector3(-65f, -10f, 15f) },
                new(){ timePercent=0.55f, pos=new Vector3( 0.02f,-0.02f,  0.35f), rotEuler=new Vector3( 15f,   5f, -5f) },
                new(){ timePercent=0.75f, pos=new Vector3( 0.01f,-0.04f,  0.28f), rotEuler=new Vector3( 25f,   8f, -3f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,   0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}