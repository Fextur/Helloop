using UnityEngine;

namespace Helloop.Data
{
    public enum EnemyType
    {
        Patrol,
        Aggressive,
        Stationary
    }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Helloop/Enemies/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Info")]
        public string enemyName;
        public GameObject enemyPrefab;

        [Header("Stats")]
        public float maxHealth = 100f;
        public float damage = 10f;
        public float attackRange = 2.5f;
        [Tooltip("Range at which enemy will start attacking (should be larger than attackRange)")]
        public float attackDetectionRange = 2f;

        [Header("Behavior")]
        public EnemyType enemyType = EnemyType.Aggressive;
        public float sightRange = 10f;
        public float pursueDistance = 15f;

        [Header("Movement")]
        public float moveSpeed = 3.5f;
        public float attackCooldown = 1f;

        [Header("Attack Timing")]
        [Tooltip("Time delay before damage is actually dealt after attack animation starts")]
        public float attackDamageDelay = 0.5f;
        [Tooltip("Total duration of attack animation (for proper cooldown timing)")]
        public float attackAnimationDuration = 1f;

        [Header("Audio")]
        public AudioClip idleSound;
        public AudioClip hittingSound;
        public AudioClip gettingHitSound;
        public AudioClip deathSound;

        private void OnValidate()
        {
            if (attackRange < attackDetectionRange)
            {
                attackRange = attackDetectionRange + 0.5f;
            }
        }
    }
}