using UnityEngine;

namespace Helloop.Events
{
    [CreateAssetMenu(fileName = "InteractionEvent", menuName = "Helloop/Events/InteractionEvent")]
    public class InteractionEvent : ScriptableObject
    {
        private System.Action<string> listeners;

        public void Raise(string interactionText)
        {
            listeners?.Invoke(interactionText);
        }

        public void Subscribe(System.Action<string> listener)
        {
            listeners += listener;
        }

        public void Unsubscribe(System.Action<string> listener)
        {
            listeners -= listener;
        }

        void OnDisable()
        {
            listeners = null;
        }
    }
}