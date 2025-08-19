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

        private const float WindowStart = 0.50f;
        private const float WindowEnd = 0.70f;
        private const int MaxHitColliders = 96;
        private static readonly Collider[] s_Overlap = new Collider[MaxHitColliders];

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;
            durationSeconds = Mathf.Max(0.001f, owner.Data.swingTime * 1.5f);
            angleDegrees = owner.Data.swipeAngleDegrees;

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

            if (!hitWindowProcessed && t01 >= WindowStart && t01 <= WindowEnd)
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
            Transform camT = owner.PlayerCamera != null ?
                owner.PlayerCamera.transform : owner.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;

            int found = Physics.OverlapSphereNonAlloc(origin, owner.Data.range, s_Overlap, ~owner.Data.ignoreLayers);
            HashSet<GameObject> hitTargets = new HashSet<GameObject>();

            for (int i = 0; i < found; i++)
            {
                Collider col = s_Overlap[i];
                Vector3 dir = (col.bounds.center - origin).normalized;

                if (!WithinAngle(forward, dir, angleDegrees)) continue;

                GameObject rootObj = col.transform.root.gameObject;
                if (hitTargets.Contains(rootObj)) continue;

                if (TryResolveEnemy(col, out EnemyHealth enemyHealth))
                {
                    hitTargets.Add(rootObj);
                    enemyHealth.TakeDamage(owner.ScaledDamage);
                    owner.WeaponSystem.UseDurability(1);
                    if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                    continue;
                }

                if (TryResolveDestructible(col, out DestructibleObject destructible))
                {
                    hitTargets.Add(rootObj);
                    Vector3 hitPoint = col.ClosestPoint(origin);
                    destructible.TakeDamage(owner.ScaledDamage, hitPoint, dir);
                    owner.WeaponSystem.UseDurability(1);
                    if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
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