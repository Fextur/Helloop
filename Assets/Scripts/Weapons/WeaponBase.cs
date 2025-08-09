using UnityEngine;

namespace Helloop.Weapons
{
    public abstract class WeaponBase : MonoBehaviour
    {
        protected Vector3 originalPosition;
        protected Quaternion originalRotation;
        public AudioSource audioSource;

        protected virtual void Start()
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

    }
}