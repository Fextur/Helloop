using UnityEngine;
using Helloop.Events;
using Helloop.Interactions;

namespace Helloop.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(WeaponManager))]
    public class InteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        public float interactionRange = 3f;
        public LayerMask interactionLayers = -1;
        public KeyCode interactionKey = KeyCode.E;

        [Header("Events")]
        public InteractionEvent OnInteractionAvailable;
        public GameEvent OnInteractionUnavailable;

        [Header("Camera Reference")]
        public Camera playerCamera;

        private IInteractable currentInteractable;
        private bool hasCurrentInteractable = false;

        private float lastInteractionCheck = 0f;
        private float interactionCheckInterval = 0.15f;

        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        void Update()
        {
            if (Time.time - lastInteractionCheck >= interactionCheckInterval)
            {
                lastInteractionCheck = Time.time;
                CheckForInteractable();
            }

            HandleInteractionInput();
        }

        void CheckForInteractable()
        {
            if (playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, interactionLayers, QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable != null && interactable.CanInteract())
                {
                    if (currentInteractable != interactable)
                    {
                        SetCurrentInteractable(interactable);
                    }
                }
                else
                {
                    ClearCurrentInteractable();
                }
            }
            else
            {
                ClearCurrentInteractable();
            }
        }

        void SetCurrentInteractable(IInteractable interactable)
        {
            if (currentInteractable != null)
                currentInteractable.OnInteractionExit();

            currentInteractable = interactable;
            currentInteractable.OnInteractionEnter();

            hasCurrentInteractable = true;

            string interactionText = currentInteractable.GetInteractionText();
            OnInteractionAvailable?.Raise(interactionText);
        }

        void ClearCurrentInteractable()
        {
            if (!hasCurrentInteractable) return;

            currentInteractable?.OnInteractionExit();
            currentInteractable = null;
            hasCurrentInteractable = false;

            OnInteractionUnavailable?.Raise();
        }

        void HandleInteractionInput()
        {
            if (Input.GetKeyDown(interactionKey) && currentInteractable != null)
            {
                currentInteractable.Interact();

                if (!currentInteractable.CanInteract())
                {
                    ClearCurrentInteractable();
                }
                else
                {
                    string newText = currentInteractable.GetInteractionText();
                    OnInteractionAvailable?.Raise(newText);
                }
            }
        }
    }
}