using UnityEngine;
using Helloop.Systems;

namespace Helloop.UI
{
    [RequireComponent(typeof(PauseUIController))]

    public class PauseInputController : MonoBehaviour
    {
        [Header("System Reference")]
        public GameStateSystem gameStateSystem;

        void Update()
        {
            if (gameStateSystem == null) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (gameStateSystem.isPaused)
                    gameStateSystem.ResumeGame();
                else if (!gameStateSystem.isTransitioning)
                    gameStateSystem.PauseGame();
            }
        }
    }
}