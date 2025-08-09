using UnityEngine;
using System.Collections;

namespace Helloop.Interactions
{
    public class DoorController : MonoBehaviour, IInteractable
    {
        [Header("Door Settings")]
        public bool isOpen = false;
        public float openAngle = 90f;
        public float openSpeed = 2f;
        public bool canClose = true;

        [Header("Door Rotation")]
        public Transform doorHinge;
        public bool rotateClockwise = true;

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;

        [Header("Lock Settings")]
        public bool isLocked = false;
        public string lockedMessage = "Door is locked";

        private Quaternion closedRotation;
        private Quaternion openRotation;
        private AudioSource audioSource;
        private bool isAnimating = false;

        void Start()
        {
            if (doorHinge == null)
                doorHinge = transform;

            closedRotation = doorHinge.rotation;

            Vector3 openEuler = closedRotation.eulerAngles;
            openEuler.y += rotateClockwise ? openAngle : -openAngle;
            openRotation = Quaternion.Euler(openEuler);

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public void Interact()
        {
            if (isLocked)
            {
                PlayLockedSound();
                return;
            }

            if (isAnimating) return;

            if (isOpen)
            {
                if (canClose)
                    CloseDoor();
            }
            else
            {
                OpenDoor();
            }
        }

        public string GetInteractionText()
        {
            if (isLocked)
                return lockedMessage;

            if (isAnimating)
                return "Wait...";

            if (isOpen && canClose)
                return "Press E to Close";
            else if (!isOpen)
                return "Press E to Open";
            else
                return "";
        }

        public bool CanInteract()
        {
            return !isAnimating && (isLocked || !isOpen || canClose);
        }

        public void OnInteractionEnter()
        {
        }

        public void OnInteractionExit()
        {
        }

        void OpenDoor()
        {
            if (isAnimating || isOpen) return;

            StartCoroutine(AnimateDoor(openRotation, true));
            PlayOpenSound();
        }

        void CloseDoor()
        {
            if (isAnimating || !isOpen || !canClose) return;

            StartCoroutine(AnimateDoor(closedRotation, false));
            PlayCloseSound();
        }

        IEnumerator AnimateDoor(Quaternion targetRotation, bool opening)
        {
            isAnimating = true;
            Quaternion startRotation = doorHinge.rotation;
            float elapsedTime = 0f;
            float animationTime = 1f / openSpeed;

            while (elapsedTime < animationTime)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / animationTime;
                float smoothTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                doorHinge.rotation = Quaternion.Lerp(startRotation, targetRotation, smoothTime);
                yield return null;
            }

            doorHinge.rotation = targetRotation;
            isOpen = opening;
            isAnimating = false;
        }

        void PlayOpenSound()
        {
            if (openSound != null && audioSource != null)
                audioSource.PlayOneShot(openSound);
        }

        void PlayCloseSound()
        {
            if (closeSound != null && audioSource != null)
                audioSource.PlayOneShot(closeSound);
        }

        void PlayLockedSound()
        {
            if (lockedSound != null && audioSource != null)
                audioSource.PlayOneShot(lockedSound);
        }

        public void LockDoor()
        {
            isLocked = true;
        }

        public void UnlockDoor()
        {
            isLocked = false;
        }

        public void SetDoorState(bool open, bool immediate = false)
        {
            if (immediate)
            {
                isOpen = open;
                doorHinge.rotation = open ? openRotation : closedRotation;
            }
            else
            {
                if (open && !isOpen)
                    OpenDoor();
                else if (!open && isOpen && canClose)
                    CloseDoor();
            }
        }
    }
}