using UnityEngine;
using Helloop.Interactions;

namespace Helloop.Audio
{
    public class DoorMusicTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("Should the music start when this door opens?")]
        public bool triggerMusicOnOpen = true;

        [Tooltip("Delay before starting music after door opens")]
        public float musicStartDelay = 1f;


        [Tooltip("Manager")]
        public PersistentMusicManager musicManager;

        private DoorController doorController;
        private bool hasTriggeredMusic = false;

        void Start()
        {
            doorController = GetComponent<DoorController>();
        }

        void Update()
        {
            if (triggerMusicOnOpen && !hasTriggeredMusic && doorController != null)
            {
                if (doorController.isOpen)
                {
                    TriggerGameMusic();
                }
            }
        }

        void TriggerGameMusic()
        {
            if (hasTriggeredMusic) return;

            hasTriggeredMusic = true;

            if (musicStartDelay > 0f)
            {
                Invoke(nameof(StartMusicDelayed), musicStartDelay);
            }
            else
            {
                StartMusicDelayed();
            }
        }

        void StartMusicDelayed()
        {
            if (musicManager != null)
            {
                musicManager.StartGameMusic();
            }
        }

    }
}