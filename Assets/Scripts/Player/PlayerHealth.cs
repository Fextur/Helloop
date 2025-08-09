using UnityEngine;
using Helloop.Systems;
using Helloop.Core;

namespace Helloop.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(WeaponManager))]
    [RequireComponent(typeof(InteractionController))]

    public class PlayerHealth : MonoBehaviour
    {
        [Header("System Reference")]
        public PlayerSystem playerSystem;
        public WeaponSystem weaponSystem;
        public ProgressionSystem progressionSystem;

        private AudioSource audioSource;

        void Start()
        {
            SetupAudio();

            if (playerSystem != null)
            {
                playerSystem.OnPlayerDied.Subscribe(HandleGameOver);
                playerSystem.OnPlayerRespawned.Subscribe(HandleRespawn);
                playerSystem.SetAudioSource(audioSource);

                InitializePlayer();
            }
        }

        void OnDestroy()
        {
            if (playerSystem != null)
            {
                playerSystem.OnPlayerDied.Unsubscribe(HandleGameOver);
                playerSystem.OnPlayerRespawned.Unsubscribe(HandleRespawn);
                playerSystem.UnregisterPlayer();
            }
        }

        void SetupAudio()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.playOnAwake = false;
        }

        void InitializePlayer()
        {
            if (playerSystem != null)
            {
                playerSystem.SetCheckpoint(transform.position, transform.rotation);

                playerSystem.RegisterPlayer(transform);
            }
        }

        void HandleRespawn()
        {
            RestorePlayerResources();
            TeleportToCheckpoint();
        }

        void RestorePlayerResources()
        {
            if (weaponSystem != null)
            {
                if (weaponSystem.HasRangedWeapon())
                {
                    weaponSystem.RefillAmmo();
                }

                if (weaponSystem.HasMeleeWeapon())
                {
                    weaponSystem.RestoreDurability();
                }
            }
        }

        void TeleportToCheckpoint()
        {
            if (playerSystem == null || !playerSystem.hasCheckpoint) return;

            if (TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
            }

            transform.position = playerSystem.checkpointPosition;
            transform.rotation = playerSystem.checkpointRotation;

            if (cc != null)
            {
                cc.enabled = true;
            }
        }

        void HandleGameOver()
        {
            PlayDeathSound();

            string currentFloorName = "Unknown Floor";

            if (progressionSystem != null)
            {
                currentFloorName = progressionSystem.GetCurrentCircleName();
            }
            else
            {
                Debug.LogWarning("PlayerHealth: ProgressionSystemData not assigned! Using 'Unknown Floor'.");
            }

            GameRestartManager.LoadDeathScreen(currentFloorName);
        }

        void PlayDeathSound()
        {
            if (playerSystem == null || playerSystem.deathSound == null || audioSource == null) return;

            if (playerSystem.deathSoundOverridesHurt && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            audioSource.PlayOneShot(playerSystem.deathSound);
        }
    }
}