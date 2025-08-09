using UnityEngine;
using System.Collections;
using Helloop.Core;

namespace Helloop.Environment
{
    public class VictoryTrigger : MonoBehaviour
    {
        [Header("Victory Settings")]
        public float delayBeforeVictory = 4f;

        [Header("Audio")]
        public AudioClip victorySound;
        public AudioClip explosionEcho;

        [Header("Camera Effects")]
        public float cameraShakeDuration = 3f;
        public float cameraShakeIntensity = 0.5f;

        [Header("Visual Effects")]
        public ParticleSystem additionalVictoryEffect;
        public Light victoryLight;

        private AudioSource audioSource;
        private bool victoryTriggered = false;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            StartCoroutine(VictoryCountdown());
            StartCoroutine(VictoryShake());
        }

        IEnumerator VictoryCountdown()
        {
            if (explosionEcho != null && audioSource != null)
            {
                audioSource.PlayOneShot(explosionEcho);
            }

            yield return new WaitForSeconds(delayBeforeVictory);
            TriggerVictory();
        }

        void TriggerVictory()
        {
            if (victoryTriggered) return;

            victoryTriggered = true;

            if (victorySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(victorySound);
            }

            TriggerFinalEffects();
            StartCoroutine(LoadVictoryScreenDelayed());
        }

        void TriggerFinalEffects()
        {
            if (additionalVictoryEffect != null)
            {
                additionalVictoryEffect.Play();
            }

            if (victoryLight != null)
            {
                victoryLight.enabled = true;
                StartCoroutine(PulseVictoryLight());
            }
        }

        IEnumerator PulseVictoryLight()
        {
            if (victoryLight == null) yield break;

            float originalIntensity = victoryLight.intensity;
            float elapsed = 0f;
            float pulseTime = 1f;

            while (elapsed < pulseTime)
            {
                elapsed += Time.deltaTime;
                float intensity = Mathf.Lerp(originalIntensity, originalIntensity * 3f,
                                           Mathf.Sin(elapsed * Mathf.PI * 4f) * 0.5f + 0.5f);
                victoryLight.intensity = intensity;
                yield return null;
            }

            victoryLight.intensity = originalIntensity;
        }

        IEnumerator VictoryShake()
        {
            Camera playerCamera = Camera.main;
            if (playerCamera == null) yield break;

            Vector3 originalPosition = playerCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < cameraShakeDuration && !victoryTriggered)
            {
                float currentIntensity = cameraShakeIntensity * (1f - elapsed / cameraShakeDuration);
                float x = Random.Range(-1f, 1f) * currentIntensity;
                float y = Random.Range(-1f, 1f) * currentIntensity;

                playerCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            playerCamera.transform.localPosition = originalPosition;
        }

        IEnumerator LoadVictoryScreenDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            GameRestartManager.LoadVictoryScreen();
        }

    }
}