using UnityEngine;
using System.Collections.Generic;
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

        private const float WindowStart = 0.45f;
        private const float WindowEnd = 0.65f;
        private const int MaxHitColliders = 96;
        private static readonly Collider[] s_Overlap = new Collider[MaxHitColliders];

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;
            durationSeconds = Mathf.Max(0.001f, owner.Data.swingTime);
            angleDegrees = owner.Data.swingAngleDegrees;

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

            if (!hitWindowProcessed && t01 >= WindowStart && t01 <= WindowEnd)
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
            Transform camT = owner.PlayerCamera != null ?
                owner.PlayerCamera.transform : owner.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;

            int found = Physics.OverlapSphereNonAlloc(origin, owner.Data.range, s_Overlap, ~owner.Data.ignoreLayers);

            for (int i = 0; i < found; i++)
            {
                Collider col = s_Overlap[i];
                Vector3 dir = (col.bounds.center - origin).normalized;

                if (!WithinAngle(forward, dir, angleDegrees)) continue;

                if (TryResolveEnemy(col, out EnemyHealth enemyHealth))
                {
                    enemyHealth.TakeDamage(owner.ScaledDamage);
                    owner.WeaponSystem.UseDurability(1);
                    if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                    return;
                }

                if (TryResolveDestructible(col, out DestructibleObject destructible))
                {
                    Vector3 hitPoint = col.ClosestPoint(origin);
                    destructible.TakeDamage(owner.ScaledDamage, hitPoint, dir);
                    owner.WeaponSystem.UseDurability(1);
                    if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                    return;
                }
            }
        }

        private static bool WithinAngle(Vector3 forward, Vector3 dir, float angleDegrees)
            => Vector3.Angle(forward, dir) <= angleDegrees * 0.5f;

        private static bool TryResolveEnemy(Collider col, out EnemyHealth enemyHealth)
        {
            if (col.TryGetComponent<EnemyHealth>(out enemyHealth)) return true;
            enemyHealth = col.GetComponentInParent<EnemyHealth>();
            return enemyHealth != null;
        }

        private static bool TryResolveDestructible(Collider col, out DestructibleObject destructible)
        {
            if (col.TryGetComponent<DestructibleObject>(out destructible)) return true;
            destructible = col.GetComponentInParent<DestructibleObject>();
            return destructible != null;
        }

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