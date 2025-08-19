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
            Transform camT = owner.PlayerCamera != null ? owner.PlayerCamera.transform : owner.transform;
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
                new(){ timePercent=0.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f,   0f,  0f) },
                new(){ timePercent=0.15f, pos=new Vector3(0f, -0.01f, 0.10f), rotEuler=new Vector3(-4f,  10f,  2f) },
                new(){ timePercent=0.40f, pos=new Vector3(0f, -0.02f, 0.55f), rotEuler=new Vector3(-8f,  18f,  4f) },
                new(){ timePercent=0.60f, pos=new Vector3(0f, -0.02f, 0.60f), rotEuler=new Vector3(-6f,  12f,  2f) },
                new(){ timePercent=0.80f, pos=new Vector3(0f, -0.01f, 0.34f), rotEuler=new Vector3(-3f,   6f,  1f) },
                new(){ timePercent=1.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f,   0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,   0f,   0f) },
                new(){ timePercent=0.20f, pos=new Vector3( 0.00f, 0.03f, -0.06f), rotEuler=new Vector3(-50f,   0f,   0f) },
                new(){ timePercent=0.45f, pos=new Vector3( 0.00f, 0.06f, -0.10f), rotEuler=new Vector3(-80f,   0f,   0f) },
                new(){ timePercent=0.60f, pos=new Vector3( 0.00f,-0.03f,  0.32f), rotEuler=new Vector3( 35f,   0f,   0f) },
                new(){ timePercent=0.80f, pos=new Vector3( 0.00f,-0.01f,  0.18f), rotEuler=new Vector3( 20f,   0f,   0f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f,  0.00f), rotEuler=new Vector3(  0f,   0f,   0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
                new(){ timePercent=0.18f, pos=new Vector3( 0.04f, 0.01f, 0.04f), rotEuler=new Vector3(-15f, -100f,  0f) },
                new(){ timePercent=0.45f, pos=new Vector3( 0.06f, 0.02f, 0.08f), rotEuler=new Vector3(-25f, -120f,  0f) },
                new(){ timePercent=0.62f, pos=new Vector3(-0.04f,-0.01f, 0.34f), rotEuler=new Vector3(-15f,  80f,  0f) },
                new(){ timePercent=0.82f, pos=new Vector3(-0.02f, 0.00f, 0.18f), rotEuler=new Vector3( -8f,  40f,  0f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) }
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}