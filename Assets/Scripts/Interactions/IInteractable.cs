using UnityEngine;

namespace Helloop.Interactions
{
    public interface IInteractable
    {
        void Interact();

        string GetInteractionText();

        bool CanInteract();

        void OnInteractionEnter();

        void OnInteractionExit();
    }
}