using UnityEngine;

namespace Helloop.Weapons
{
    /// <summary>
    /// Minimal, percent-based melee animation helper.
    ///
    /// Assumptions:
    ///   - Weapon local (0,0,0) is the hand grip/pivot.
    ///   - Local +Z points forward along the weapon.
    ///
    /// How to use:
    ///   1) Create an array of AnimationPhase { pos, rotEuler, timePercent }.
    ///      - timePercent is in [0..1], sorted ascending (0=start, 1=end).
    ///   2) On each frame call either:
    ///        ApplyNormalized(target, basePos, baseRot, t01, phases)
    ///        // where t01 = elapsed / duration
    ///      or
    ///        Apply(target, basePos, baseRot, elapsedSeconds, totalSeconds, phases)
    ///      The helper will interpolate between the two bracketing phases and set
    ///      target.localPosition/localRotation.
    ///
    /// Notes:
    ///   - If t is before the first phase, we clamp to phase 0.
    ///   - If t is after the last phase, we clamp to the last phase.
    ///   - Rotations use Quaternion slerp between Euler-defined keys.
    ///   - No GC: no allocations per frame.
    /// </summary>
    public static class MeleeAnimPhasesSimple
    {
        [System.Serializable]
        public struct AnimationPhase
        {
            public Vector3 pos;        // LOCAL position offset (meters)
            public Vector3 rotEuler;   // LOCAL rotation (degrees). X=pitch, Y=yaw, Z=roll.
            [Range(0f, 1f)]
            public float timePercent;  // 0..1 of the clip duration
        }

        /// <summary>
        /// Apply using absolute times (seconds). Internally converts to normalized 0..1.
        /// </summary>
        public static void Apply(Transform target,
                                 Vector3 baseLocalPos,
                                 Quaternion baseLocalRot,
                                 float elapsedSeconds,
                                 float totalSeconds,
                                 AnimationPhase[] phases)
        {
            float t01 = (totalSeconds <= 0f) ? 0f : Mathf.Clamp01(elapsedSeconds / totalSeconds);
            ApplyNormalized(target, baseLocalPos, baseLocalRot, t01, phases);
        }

        /// <summary>
        /// Apply using normalized time (0..1).
        /// </summary>
        public static void ApplyNormalized(Transform target,
                                           Vector3 baseLocalPos,
                                           Quaternion baseLocalRot,
                                           float t01,
                                           AnimationPhase[] phases)
        {
            if (target == null || phases == null || phases.Length == 0)
                return;

            t01 = Mathf.Clamp01(t01);

            // Find segment i such that t01 ∈ [phase[i], phase[i+1]]
            int n = phases.Length;

            if (t01 <= phases[0].timePercent)
            {
                // Clamp to first
                SetPose(target, baseLocalPos, baseLocalRot, phases[0]);
                return;
            }

            if (t01 >= phases[n - 1].timePercent)
            {
                // Clamp to last
                SetPose(target, baseLocalPos, baseLocalRot, phases[n - 1]);
                return;
            }

            int seg = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (t01 <= phases[i + 1].timePercent)
                {
                    seg = i;
                    break;
                }
            }

            ref readonly AnimationPhase a = ref phases[seg];
            ref readonly AnimationPhase b = ref phases[seg + 1];

            float t0 = a.timePercent;
            float t1 = b.timePercent;
            float u = (t01 - t0) / Mathf.Max(0.0001f, t1 - t0); // linear within segment

            // Interpolate local rotation via slerp between Euler-defined keys
            Quaternion qa = Quaternion.Euler(a.rotEuler);
            Quaternion qb = Quaternion.Euler(b.rotEuler);
            Quaternion qL = Quaternion.Slerp(qa, qb, u);

            // Interpolate local position (offset)
            Vector3 pL = Vector3.LerpUnclamped(a.pos, b.pos, u);

            // Compose over base local pose
            target.localRotation = baseLocalRot * qL;
            target.localPosition = baseLocalPos + pL;
        }

        private static void SetPose(Transform target, Vector3 baseLocalPos, Quaternion baseLocalRot, AnimationPhase p)
        {
            target.localRotation = baseLocalRot * Quaternion.Euler(p.rotEuler);
            target.localPosition = baseLocalPos + p.pos;
        }
    }
}
