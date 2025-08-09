using UnityEngine;
using Helloop.Events;

namespace Helloop.Systems
{
    [CreateAssetMenu(fileName = "PlayerSystem", menuName = "Helloop/Systems/PlayerSystem")]
    public class PlayerSystem : ScriptableObject
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        public float currentHealth;

        [Header("Lives Settings")]
        public int maxLives = 3;
        public int currentLives;

        [Header("Respawn Settings")]
        public Vector3 checkpointPosition;
        public Quaternion checkpointRotation;
        public bool hasCheckpoint = false;

        [Header("Stealth Settings")]
        public bool isStealth = false;

        [Header("Detection Tracking")]
        private int detectionCount = 0;
        public bool IsBeingDetected => detectionCount > 0;

        [Header("Audio Settings")]
        public AudioClip hurtSound;
        public AudioClip deathSound;
        public float hurtSoundCooldown = 0.5f;
        public bool deathSoundOverridesHurt = true;

        [Header("Events")]
        public GameEvent OnHealthChanged;
        public GameEvent OnLivesChanged;
        public GameEvent OnPlayerDied;
        public GameEvent OnPlayerRespawned;
        public GameEvent OnCheckpointSet;
        public GameEvent OnStealthChanged;
        public GameEvent OnPlayerDetected;
        public GameEvent OnPlayerUndetected;

        [Header("Player Position Tracking")]
        public Transform currentPlayer;
        public bool playerExists = false;

        private AudioSource audioSource;
        private float lastHurtSoundTime = -1f;
        private bool isRegisteringPlayer = false;

        public void RegisterDetection()
        {
            detectionCount++;
            if (detectionCount == 1)
            {
                OnPlayerDetected?.Raise();
            }
        }

        public void UnregisterDetection()
        {
            detectionCount = Mathf.Max(0, detectionCount - 1);
            if (detectionCount == 0)
            {
                OnPlayerUndetected?.Raise();
            }
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);

            OnHealthChanged?.Raise();

            if (currentHealth <= 0)
            {
                Die();
            }
            else if (hurtSound != null && CanPlayHurtSound() && audioSource != null)
            {
                audioSource.PlayOneShot(hurtSound);
                MarkHurtSoundPlayed();
            }
        }

        public void SetFullHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Raise();
        }

        void Die()
        {
            currentLives--;
            OnLivesChanged?.Raise();

            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }

            if (currentLives > 0)
            {
                Respawn();
            }
            else
            {
                GameOver();
            }
        }

        void Respawn()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Raise();
            OnPlayerRespawned?.Raise();
        }

        void GameOver()
        {
            OnPlayerDied?.Raise();
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
            hasCheckpoint = true;
            OnCheckpointSet?.Raise();
        }

        public void SetStealth(bool stealthState)
        {
            if (isStealth != stealthState)
            {
                isStealth = stealthState;

                if (OnStealthChanged != null)
                {
                    OnStealthChanged.Raise();
                }
            }
        }

        public void ForceExitStealth()
        {
            SetStealth(false);
        }

        public float GetHealthPercentage()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }

        public bool CanPlayHurtSound()
        {
            return Time.time >= lastHurtSoundTime + hurtSoundCooldown;
        }

        public void MarkHurtSoundPlayed()
        {
            lastHurtSoundTime = Time.time;
        }

        void OnEnable()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            currentHealth = maxHealth;
            currentLives = maxLives;
            checkpointPosition = Vector3.zero;
            checkpointRotation = Quaternion.identity;
            hasCheckpoint = false;
            isStealth = false;
            lastHurtSoundTime = -1f;
            detectionCount = 0;
            currentPlayer = null;
            playerExists = false;
            isRegisteringPlayer = false;
        }

        public void SetAudioSource(AudioSource source)
        {
            audioSource = source;
        }

        public void RegisterPlayer(Transform player)
        {
            if (isRegisteringPlayer) return;

            if (currentPlayer == player && playerExists) return;

            isRegisteringPlayer = true;

            currentPlayer = player;
            playerExists = player != null;

            isRegisteringPlayer = false;
        }

        public void UnregisterPlayer()
        {
            currentPlayer = null;
            playerExists = false;

            OnPlayerDied?.Raise();
        }

        public Transform GetPlayer() => currentPlayer;
        public bool HasPlayer() => playerExists && currentPlayer != null;
    }
}