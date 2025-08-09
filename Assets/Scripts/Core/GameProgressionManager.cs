using UnityEngine;
using UnityEngine.SceneManagement;
using Helloop.Systems;
using Helloop.Data;
using Helloop.Enemies;

namespace Helloop.Core
{
    public class GameProgressionManager : MonoBehaviour
    {
        [Header("System References")]
        public ProgressionSystem progressionSystem;
        public RoomSystem roomSystem;


        [Header("Limbo Configuration")]
        [Tooltip("Only set this in Limbo scene - the first circle to transition to (Circle 2)")]
        public CircleData nextCircleData;

        void Start()
        {
            if (progressionSystem == null)
            {
                Debug.LogError("ProgressionSystemData not assigned to GameProgressionManager!");
                return;
            }

            SubscribeToEvents();
            InitializeProgression();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            if (progressionSystem != null)
            {
                progressionSystem.OnCircleChanged?.Subscribe(HandleCircleChange);

            }
        }
        void UnsubscribeFromEvents()
        {
            if (progressionSystem != null)
            {
                progressionSystem.OnCircleChanged?.Unsubscribe(HandleCircleChange);
            }
        }


        void InitializeProgression()
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == progressionSystem.limboSceneName)
            {
                progressionSystem.SetInitialCircle(nextCircleData);
            }
        }

        void HandleCircleChange()
        {
            if (roomSystem != null)
            {
                roomSystem.ClearRooms();
            }
            if (progressionSystem.IsInParadise())
            {
                SceneManager.LoadScene(progressionSystem.paradiseSceneName);
            }
            else
            {
                SceneManager.LoadScene(progressionSystem.circleSceneName);
            }
        }
    }
}