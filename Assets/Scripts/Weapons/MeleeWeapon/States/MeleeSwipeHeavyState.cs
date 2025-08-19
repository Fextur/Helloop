using UnityEngine;
using System.Collections.Generic;
using Helloop.StateMachines;
using Helloop.Enemies;
using Helloop.Environment;
using Helloop.Weapons; // for MeleeAnimPhasesSimple

namespace Helloop.Weapons.States
{
    /// <summary>
    /// Heavy swipe. Handles animation + multi-hit cone resolution.
    /// No input or dash override here (routing is handled by the weapon state machine).
    /// </summary>
    public class MeleeSwipeHeavyState : IState<MeleeWeapon>
    {
        private MeleeWeapon owner;

        private float durationSeconds;
        private float elapsed;
        private float angleDegrees;

        private bool hitWindowProcessed;

        // Hit window (normalized)
        private const float WindowStart = 0.40f;
        private const float WindowEnd = 0.60f;

        private const int MaxHitColliders = 96;
        private static readonly Collider[] s_Overlap = new Collider[MaxHitColliders];

        public void OnEnter(MeleeWeapon weapon)
        {
            owner = weapon;

            durationSeconds = Mathf.Max(0.001f, owner.Data.swipeTime);
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

            float maxDist = owner.Data.range;
            int mask = ~owner.Data.ignoreLayers;

            Vector3 center = origin + forward * (maxDist * 0.6f);
            int count = Physics.OverlapSphereNonAlloc(center, maxDist, s_Overlap, mask);

            var uniqueEnemies = new HashSet<EnemyHealth>();
            var uniqueDestructibles = new HashSet<DestructibleObject>();

            for (int i = 0; i < count; i++)
            {
                var col = s_Overlap[i];
                if (col == null) continue;

                Vector3 toCenter = col.bounds.center - origin;
                float dist = toCenter.magnitude;
                if (dist > maxDist) continue;

                Vector3 dir = toCenter.normalized;
                if (!WithinAngle(forward, dir, angleDegrees)) continue;

                if (TryResolveEnemy(col, out var enemy)) { uniqueEnemies.Add(enemy); continue; }
                if (TryResolveDestructible(col, out var d)) { uniqueDestructibles.Add(d); }
            }

            foreach (var enemy in uniqueEnemies)
            {
                enemy.TakeDamage(owner.ScaledDamage);
                owner.WeaponSystem.UseDurability(1);
                owner.OnEnemyHit?.Raise(enemy);
                if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
            }

            foreach (var d in uniqueDestructibles)
            {
                var dCol = d.GetComponent<Collider>();
                Vector3 hp = dCol != null ? dCol.ClosestPoint(origin) : d.transform.position;
                Vector3 dir = (hp - origin).normalized;

                d.TakeDamage(owner.ScaledDamage, hp, dir);
                owner.WeaponSystem.UseDurability(1);
                if (owner.GetCurrentDurability() <= 0) owner.SetWeaponVisibility(false);
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

        // ---------------- Animation (phase helper) ----------------

        private void ApplyThrustHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f,   0f,  0f) },
                new(){ timePercent=0.15f, pos=new Vector3(0f, -0.01f, 0.10f), rotEuler=new Vector3(-4f,  10f,  2f) },
                new(){ timePercent=0.40f, pos=new Vector3(0f, -0.02f, 0.55f), rotEuler=new Vector3(-8f,  18f,  4f) },
                new(){ timePercent=0.60f, pos=new Vector3(0f, -0.02f, 0.60f), rotEuler=new Vector3(-6f,  12f,  2f) },
                new(){ timePercent=0.80f, pos=new Vector3(0f, -0.01f, 0.34f), rotEuler=new Vector3(-3f,   6f,  1f) },
                new(){ timePercent=1.00f, pos=new Vector3(0f,  0.00f, 0.00f), rotEuler=new Vector3( 0f,   0f,  0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplyOverheadHeavy(float t01)
        {
            // Z pre-tilt ≈ 60–70°, yaw ≈ 190° → 45°
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,   0f) },
                new(){ timePercent=0.20f, pos=new Vector3(-0.03f, 0.06f, 0.08f), rotEuler=new Vector3(-26f,  10f,  32f) },
                new(){ timePercent=0.45f, pos=new Vector3( 0.00f, 0.12f, 0.12f), rotEuler=new Vector3(-70f, 190f,  60f) },
                new(){ timePercent=0.60f, pos=new Vector3( 0.02f,-0.02f, 0.32f), rotEuler=new Vector3(-12f,  45f,  18f) },
                new(){ timePercent=0.80f, pos=new Vector3( 0.01f,-0.01f, 0.18f), rotEuler=new Vector3( -6f,   8f,   6f) },
                new(){ timePercent=1.00f, pos=new Vector3( 0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,   0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }

        private void ApplySlashHeavy(float t01)
        {
            var phases = new MeleeAnimPhasesSimple.AnimationPhase[]
            {
                new(){ timePercent=0.00f, pos=new Vector3(0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
                new(){ timePercent=0.18f, pos=new Vector3(0.02f, 0.02f, 0.06f), rotEuler=new Vector3( -6f,  50f,-12f) },
                new(){ timePercent=0.45f, pos=new Vector3(0.05f, 0.00f, 0.34f), rotEuler=new Vector3(-10f, 140f,-18f) },
                new(){ timePercent=0.62f, pos=new Vector3(0.02f,-0.02f, 0.36f), rotEuler=new Vector3( -8f,  60f, -8f) },
                new(){ timePercent=0.82f, pos=new Vector3(0.01f,-0.01f, 0.18f), rotEuler=new Vector3( -4f,  10f, -2f) },
                new(){ timePercent=1.00f, pos=new Vector3(0.00f, 0.00f, 0.00f), rotEuler=new Vector3(  0f,   0f,  0f) },
            };
            MeleeAnimPhasesSimple.ApplyNormalized(owner.transform, owner.originalPosition, owner.originalRotation, t01, phases);
        }
    }
}
