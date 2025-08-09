using UnityEngine;
using Helloop.Events;

namespace Helloop.Systems
{
    [CreateAssetMenu(fileName = "GameStateSystem", menuName = "Helloop/Systems/GameStateSystem")]
    public class GameStateSystem : ScriptableObject
    {
        [Header("Game State")]
        public bool isPaused = false;
        public bool isTransitioning = false;

        [Header("Settings")]
        public float mouseSensitivity = 100f;
        public float minSensitivity = 10f;
        public float maxSensitivity = 500f;
        public float defaultSensitivity = 100f;

        [Header("Audio Settings")]
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;

        [Header("Visual Settings")]
        public float fadeInDuration = 0.3f;

        [Header("Scene Settings")]
        public string mainMenuSceneName = "MainMenu";
        public string limboSceneName = "Limbo";

        [Header("Input Settings")]
        public float hurtSoundCooldown = 0.5f;

        [Header("Events")]
        public GameEvent OnGamePaused;
        public GameEvent OnGameResumed;
        public GameEvent OnSettingsChanged;
        public GameEvent OnMouseSensitivityChanged;

        public void PauseGame()
        {
            if (isPaused || isTransitioning) return;

            isPaused = true;
            isTransitioning = true;
            Time.timeScale = 0f;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            OnGamePaused?.Raise();
        }

        public void ResumeGame()
        {
            if (!isPaused) return;

            isPaused = false;
            Time.timeScale = 1f;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnGameResumed?.Raise();
        }

        public void SetTransitioning(bool transitioning)
        {
            isTransitioning = transitioning;
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, minSensitivity, maxSensitivity);
            PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
            PlayerPrefs.Save();

            OnMouseSensitivityChanged?.Raise();
            OnSettingsChanged?.Raise();
        }

        public void LoadMouseSensitivity()
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, minSensitivity, maxSensitivity);
        }

        public bool ShouldBlockInput()
        {
            if (isPaused) return true;
            return IsMouseOverUI();
        }

        public bool IsMouseOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        void OnEnable()
        {
            LoadMouseSensitivity();
            InitializeGameState();
        }

        public void InitializeGameState()
        {
            isPaused = false;
            isTransitioning = false;
            Time.timeScale = 1f;
        }

        public void ResetToDefaults()
        {
            isPaused = false;
            isTransitioning = false;
            Time.timeScale = 1f;
            LoadMouseSensitivity();
        }
    }
}