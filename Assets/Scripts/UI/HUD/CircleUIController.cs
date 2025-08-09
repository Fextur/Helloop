using UnityEngine;
using TMPro;
using Helloop.Systems;

namespace Helloop.UI
{
    public class CircleUIController : MonoBehaviour
    {
        [Header("World Info UI")]
        public TextMeshProUGUI floorNameText;

        [Header("System References")]
        public ProgressionSystem progressionSystem;

        void Start()
        {
            SubscribeToEvents();
            UpdateFloorUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            if (progressionSystem != null)
            {
                progressionSystem.OnProgressionUpdated?.Subscribe(UpdateFloorUI);
            }
        }

        void UnsubscribeFromEvents()
        {
            if (progressionSystem != null)
            {
                progressionSystem.OnProgressionUpdated?.Unsubscribe(UpdateFloorUI);
            }
        }

        void UpdateFloorUI()
        {
            if (floorNameText != null && progressionSystem != null)
            {
                floorNameText.text = progressionSystem.GetCurrentCircleName();
            }
        }
    }
}