using UnityEngine;
using Helloop.Systems;
using Helloop.UI;

namespace Helloop.Interactions
{
    public class Portal : MonoBehaviour, IInteractable
    {
        [Header("System Reference")]
        public ProgressionSystem progressionSystem;

        private bool isBeingUsed = false;
        private bool hasPlayerExited = false;
        [SerializeField] private float portalFadeIn = 0.35f;


        void Start()
        {
            if (progressionSystem == null)
            {
                Debug.LogWarning("Portal: ProgressionSystemData not assigned!");
            }

            StartCoroutine(CheckInitialPlayerPosition());
        }

        System.Collections.IEnumerator CheckInitialPlayerPosition()
        {
            yield return null;

            Collider playerCollider = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Collider>();
            Collider myCollider = GetComponent<Collider>();

            if (playerCollider != null && myCollider != null)
            {
                if (myCollider.bounds.Intersects(playerCollider.bounds))
                {
                    hasPlayerExited = false;
                }
                else
                {
                    hasPlayerExited = true;
                }
            }
            else
            {
                hasPlayerExited = true;
            }
        }

        public void Interact()
        {
            if (!isBeingUsed && hasPlayerExited)
            {
                StartCoroutine(UsePortal());
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && hasPlayerExited)
            {
                Interact();
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                hasPlayerExited = true;
            }
        }

        public string GetInteractionText()
        {
            if (isBeingUsed)
                return "Teleporting...";

            if (!hasPlayerExited)
                return "Step away and return to continue";

            if (progressionSystem != null)
            {
                if (progressionSystem.IsInLimbo())
                {
                    return "Press E to descend to Circle 2 - Lust";
                }
                else if (progressionSystem.GetCurrentCircle() != null)
                {
                    if (progressionSystem.GetCurrentCircle().nextCircle != null)
                    {
                        return $"Press E to enter {progressionSystem.GetCurrentCircle().nextCircle.GetFullCircleName()}";
                    }
                    else
                    {
                        return "Press E to go to Paradise!";
                    }
                }
            }

            return "Press E to continue";
        }

        public bool CanInteract()
        {
            return !isBeingUsed && hasPlayerExited;
        }

        public void OnInteractionEnter() { }
        public void OnInteractionExit() { }

        System.Collections.IEnumerator UsePortal()
        {
            isBeingUsed = true;
            if (progressionSystem != null)
            {

                if (PortalTransitionOverlay.Instance != null)
                    PortalTransitionOverlay.Instance.FadeInNow(portalFadeIn);

                yield return new WaitForSecondsRealtime(portalFadeIn);

                progressionSystem.GoToNextCircle();
            }

        }
    }
}