using UnityEngine;
using TMPro;
using Helloop.Events;

namespace Helloop.UI
{
    public class InteractionUIController : MonoBehaviour
    {
        [Header("Interaction UI")]
        public GameObject interactionUI;
        public TextMeshProUGUI interactionText;

        [Header("Events")]
        public InteractionEvent OnInteractionAvailable;
        public GameEvent OnInteractionUnavailable;

        void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            OnInteractionAvailable?.Subscribe(ShowInteraction);
            OnInteractionUnavailable?.Subscribe(HideInteraction);
        }

        void UnsubscribeFromEvents()
        {
            OnInteractionAvailable?.Unsubscribe(ShowInteraction);
            OnInteractionUnavailable?.Unsubscribe(HideInteraction);
        }

        void InitializeUI()
        {
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }

        void ShowInteraction(string text)
        {
            if (interactionText != null)
                interactionText.text = text;

            if (interactionUI != null)
                interactionUI.SetActive(true);
        }

        void HideInteraction()
        {
            if (interactionUI != null)
                interactionUI.SetActive(false);
        }
    }
}