using UnityEngine;

namespace Helloop.Events
{
    [CreateAssetMenu(fileName = "GameEvent", menuName = "Helloop/Events/GameEvent")]
    public class GameEvent : ScriptableObject
    {
        private System.Action listeners;

        public void Raise()
        {
            listeners?.Invoke();
        }

        public void Subscribe(System.Action listener)
        {
            listeners += listener;
        }

        public void Unsubscribe(System.Action listener)
        {
            listeners -= listener;
        }

        void OnDisable()
        {
            listeners = null;
        }
    }
}