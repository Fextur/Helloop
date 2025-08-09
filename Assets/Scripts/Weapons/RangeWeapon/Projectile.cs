using System.Collections.Generic;
using UnityEngine;
using Helloop.Events;
using Helloop.Enemies;
using Helloop.Environment;

namespace Helloop.Weapons
{
    public class Projectile : MonoBehaviour
    {
        [Header("Event System")]
        public EnemyHitEvent onEnemyHit;

        [Header("Collision Settings")]
        [Tooltip("Layers that should stop the projectile (walls, floors, etc.)")]
        public LayerMask stopLayers = -1;

        private float speed;
        private float damage;
        private float falloffDistance;
        private Vector3 spawnPosition;
        private HashSet<Collider> hitEnemies = new();

        public void Initialize(float speed, float damage, float falloff)
        {
            this.speed = speed;
            this.damage = damage;
            this.falloffDistance = falloff;
            spawnPosition = transform.position;
        }

        void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;

            if (Vector3.Distance(spawnPosition, transform.position) > falloffDistance)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Enemy"))
            {
                if (!hitEnemies.Contains(other))
                {
                    hitEnemies.Add(other);
                    if (other.TryGetComponent<EnemyHealth>(out var health))
                    {
                        health.TakeDamage(damage);

                        if (onEnemyHit != null)
                        {
                            onEnemyHit.Raise(health);
                        }
                    }
                }
                return;
            }

            if (other.TryGetComponent<DestructibleObject>(out var destructible))
            {
                Vector3 impactPoint = transform.position;
                Vector3 impactDirection = transform.forward;

                destructible.TakeDamage(damage, impactPoint, impactDirection);
                Destroy(gameObject);
                return;
            }

            if (ShouldStopProjectile(other))
            {
                Destroy(gameObject);
            }
        }

        private bool ShouldStopProjectile(Collider collider)
        {
            return ((1 << collider.gameObject.layer) & stopLayers) != 0;
        }
    }
}