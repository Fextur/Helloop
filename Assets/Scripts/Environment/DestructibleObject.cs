using UnityEngine;

namespace Helloop.Environment
{
    public class DestructibleObject : MonoBehaviour
    {
        [Header("Destructible Settings")]
        public float maxHealth = 50f;
        public GameObject explodedPrefab;

        [Header("Optional Effects")]
        public AudioClip destructionSound;

        private float currentHealth;
        private bool isDestroyed = false;

        void Start()
        {
            currentHealth = maxHealth;
        }
        public void TakeDamage(float damage, Vector3 impactPoint, Vector3 impactDirection)
        {
            if (isDestroyed) return;

            currentHealth -= damage;

            if (currentHealth <= 0)
            {
                DestroyObject();
            }
        }


        void DestroyObject()
        {
            if (isDestroyed) return;
            isDestroyed = true;

            if (destructionSound != null)
            {
                AudioSource.PlayClipAtPoint(destructionSound, transform.position);
            }

            if (explodedPrefab != null)
            {
                Instantiate(explodedPrefab, transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }

    }
}