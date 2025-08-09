using UnityEngine;
using Helloop.Enemies;

namespace Helloop.Events
{
    [CreateAssetMenu(fileName = "EnemyHitEvent", menuName = "Helloop/Events/EnemyHitEvent")]
    public class EnemyHitEvent : ScriptableObject
    {
        private System.Action<EnemyHealth> listeners;

        public void Raise(EnemyHealth enemyHealth)
        {
            listeners?.Invoke(enemyHealth);
        }

        public void Subscribe(System.Action<EnemyHealth> listener)
        {
            listeners += listener;
        }

        public void Unsubscribe(System.Action<EnemyHealth> listener)
        {
            listeners -= listener;
        }

        void OnDisable()
        {
            listeners = null;
        }
    }
}