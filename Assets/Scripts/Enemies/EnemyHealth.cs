using UnityEngine;

namespace Helloop.Enemies
{
    [RequireComponent(typeof(Enemy))]

    public class EnemyHealth : MonoBehaviour
    {
        private Enemy enemy;

        void Start()
        {
            enemy = GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogError($"EnemyHealth on {gameObject.name} requires an Enemy component!");
            }
        }

        public void TakeDamage(float amount)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(amount);

            }
        }

        public float GetCurrentHealth()
        {
            if (enemy != null && enemy.enemyData != null)
            {
                return enemy.GetHealthPercentage() * enemy.enemyData.maxHealth;
            }
            return 0f;
        }

        public float GetMaxHealth()
        {
            if (enemy != null && enemy.enemyData != null)
            {
                return enemy.enemyData.maxHealth;
            }
            return 100f;
        }
    }
}