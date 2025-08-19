using UnityEngine;
using System.Collections.Generic;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;
using Helloop.Weapons; // for MeleeAnimPhasesSimple

namespace Helloop.Weapons.States
{
    /// <summary>
    /// Light attack (tap). Handles animation + single-target hit resolution only.
    /// Routing is performed by MeleeWeaponStateMachine.
    /// </summary>
    public class MeleeSwingLightState : IState<MeleeWeapon>
    {
        private MeleeWeapon owner;

        private float durationSeconds;
        private float elapsed;
        private float angleDegrees;

        private bool hitWindowProcessed;

        // Normalized hit window
        private const float HitWindowStart = 0.28f;
        private const float HitWindowEnd = 0.52f;

        private const int MaxHitColliders = 64;
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

            // Animate by weapon family
            switch (owner.Data.animationType)
            {
                case MeleeAnimationType.Thrust: ApplyThrustLight(t01); break;
                case MeleeAnimationType.Overhead: ApplyOverheadLight(t01); break;
                default: ApplySlashLight(t01); break;
            }

            // Resolve hits once inside the window
            if (!hitWindowProcessed && t01 >= HitWindowStart && t01 <= HitWindowEnd)
            {
                hitWindowProcessed = true;
                ResolveSingleTargetHit();
            }

            if (t01 >= 1f)
            {
                owner.GetStateMachine().ChangeState(new MeleeReadyState());
            }
        }

        public void OnExit(MeleeWeapon weapon) { }

        private void ResolveSingleTargetHit()
        {
            Transform camT = owner.PlayerCamera != null ? owner.PlayerCamera.transform : owner.transform;
            Vector3 origin = camT.position;
            Vector3 forward = camT.forward;

            float maxDist = owner.Data.range;
            int mask = ~owner.Data.ignoreLayers;

            // 1) Raycast priority within cone
            if (Physics.Raycast(origin, forward, out RaycastHit hit, maxDist, mask))
            {
                Vector3 dir = (hit.point - origin).normalized;
                if (WithinAngle(forward, dir, angleDegrees))
                {
                    Collider c = hit.collider;
                    if (TryResolveEnemy(c, out var enemy))
                    {
                        enemy.TakeDamage(owner.ScaledDamage);
                        owner.WeaponSystem.UseDurability(1);
                        owner.OnEnemyHit?.Raise(enemy);
                        if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                        return;
                    }
                    if (TryResolveDestructible(c, out var destructible))
                    {
                        destructible.TakeDamage(owner.ScaledDamage, hit.point, dir);
                        owner.WeaponSystem.UseDurability(1);
                        if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                        return;
                    }
                }
            }

            // 2) Else: closest eligible in cone
            Vector3 center = origin + forward * (maxDist * 0.6f);
            int count = Physics.OverlapSphereNonAlloc(center, maxDist, s_Overlap, mask);

            Collider best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var col = s_Overlap[i];
                if (col == null) continue;
                if (!IsValidTarget(col)) continue;

                Vector3 to = col.bounds.center - origin;
                float dist = to.magnitude;
                if (dist > maxDist) continue;

                if (!WithinAngle(forward, to.normalized, angleDegrees)) continue;

                if (dist < bestDist) { bestDist = dist; best = col; }
            }

            if (best != null)
            {
                if (TryResolveEnemy(best, out var enemy))
                {
                    enemy.TakeDamage(owner.ScaledDamage);
                    owner.WeaponSystem.UseDurability(1);
                    owner.OnEnemyHit?.Raise(enemy);
                    if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
                }
                else if (TryResolveDestructible(best, out var d))
                {
                    Vector3 hp = best.ClosestPoint(origin);
                    Vector3 dir = (hp - origin).normalized;
                    d.TakeDamage(owner.ScaledDamage, hp, dir);
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

        private static bool IsValidTarget(Collider col)
            => TryResolveEnemy(col, out _) || TryResolveDestructible(col, out _);

        // ---------------- Animation (phase helper) ----------------

        private void ApplyThrustLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f, 0f, 0f) },
                new(){ timePercent=0.12f, pos=new Vector3(0f, -0.01f, 0.12f), rotEuler=new Vector3(-2f, 4f, 0f) },
                new(){ timePercent=0.32f, pos=new Vector3(0f, -0.02f, 0.42f), rotEuler=new Vector3(-4f, 6f, 2f) },
                new(){ timePercent=0.52f, pos=new Vector3(0f, -0.01f, 0.48f), rotEuler=new Vector3(-3f, 2f, 1f) },
                new(){ timePercent=0.72f, pos=new Vector3(0f, -0.01f, 0.32f), rotEuler=new Vector3(-2f, 1f, 0f) },
                new(){ timePercent=1.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f, 0f, 0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,  0f,  0f) },
                new(){ timePercent=0.18f, pos=new Vector3(-0.02f, 0.04f, 0.05f), rotEuler=new Vector3(-22f, 10f, 12f) },
                new(){ timePercent=0.40f, pos=new Vector3(-0.02f, 0.10f, 0.10f), rotEuler=new Vector3(-60f, 12f, 20f) },
                new(){ timePercent=0.55f, pos=new Vector3( 0.01f,-0.01f, 0.30f), rotEuler=new Vector3(-10f,  6f, 10f) },
                new(){ timePercent=0.75f, pos=new Vector3( 0.01f,-0.01f, 0.18f), rotEuler=new Vector3( -6f,  1f,  4f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,  0f,  0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashLight(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
                new(){ timePercent=0.18f, pos=new Vector3(0.02f, 0.01f, 0.05f), rotEuler=new Vector3( -4f,  60f, -8f) },
                new(){ timePercent=0.42f, pos=new Vector3(0.04f, 0.00f, 0.26f), rotEuler=new Vector3( -6f, 140f,-12f) },
                new(){ timePercent=0.62f, pos=new Vector3(0.02f,-0.01f, 0.28f), rotEuler=new Vector3( -4f, 190f, -6f) },
                new(){ timePercent=0.82f, pos=new Vector3(0.01f,-0.01f, 0.16f), rotEuler=new Vector3( -2f,  30f, -2f) },
                new(){ timePercent=1.00f, pos=new Vector3(0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}
