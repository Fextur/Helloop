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
        private bool hitWindowProcessed;

        private const float WindowStart = 0.65f;
        private const float WindowEnd = 0.80f;
        private const int MaxHitColliders = 96;
        private static readonly Collider[] s_Overlap = new Collider[MaxHitColliders];

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;

            // Get dash duration from player movement + a bit extra
            var playerMovement = owner.GetComponentInParent<PlayerMovement>();
            float dashDuration = playerMovement != null ? playerMovement.dashDuration : 0.5f;
            durationSeconds = Mathf.Max(0.001f, dashDuration + 0.2f);

            angleDegrees = owner.Data.dashAngleDegrees;

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
                case MeleeAnimationType.Thrust: ApplyThrustDash(t01); break;
                case MeleeAnimationType.Overhead: ApplyOverheadDash(t01); break;
                default: ApplySlashDash(t01); break;
            }

            if (!hitWindowProcessed && t01 >= WindowStart && t01 <= WindowEnd)
            {
                hitWindowProcessed = true;
                ResolveExtendedReachHit();
            }

            if (t01 >= 1f)
            {
                owner.GetStateMachine().ChangeState(new MeleeReadyState());
            }
        }

        public void OnExit(MeleeWeapon weapon) { }

        private void ResolveExtendedReachHit()
        {
            Transform camT = owner.PlayerCamera != null ?
                owner.PlayerCamera.transform : owner.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;
            float extendedRange = owner.Data.range * 1.4f;

            int found = Physics.OverlapSphereNonAlloc(origin, extendedRange, s_Overlap, ~owner.Data.ignoreLayers);
            HashSet<GameObject> hitTargets = new HashSet<GameObject>();

            for (int i = 0; i < found; i++)
            {
                Collider col = s_Overlap[i];
                Vector3 dir = (col.bounds.center - origin).normalized;

                if (!WithinAngle(forward, dir, angleDegrees)) continue;

                GameObject rootObj = col.transform.root.gameObject;
                if (hitTargets.Contains(rootObj)) continue;

                float distance = Vector3.Distance(origin, col.bounds.center);
                if (distance > extendedRange) continue;

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

        private void ApplyThrustDash(float t01)
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