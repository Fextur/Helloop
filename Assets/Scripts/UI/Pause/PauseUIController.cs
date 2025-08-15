using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Helloop.Systems;
using Helloop.Core;

namespace Helloop.UI
{
    [RequireComponent(typeof(PauseInputController))]

    public class PauseUIController : MonoBehaviour
    {

        [Header("System Reference")]
        public GameStateSystem gameStateSystem;
        public ProgressionSystem progressionSystem;

        [Header("UI References")]
        public GameObject pausePanel;
        public Button resumeButton;
        public Button restartButton;
        public Button mainMenuButton;
        public Button exitButton;

        [Header("Settings UI")]
        public Slider mouseSensitivitySlider;
        public TextMeshProUGUI sensitivityValueText;
        public TextMeshProUGUI pauseTitle;

        [Header("Audio")]
        public AudioClip pauseSound;
        public AudioClip resumeSound;
        public AudioClip buttonClickSound;

        [Header("Visual Effects")]
        public CanvasGroup canvasGroup;

        private AudioSource audioSource;

        void Start()
        {
            SetupAudioSource();
            SetupUI();
            SubscribeToEvents();
            LoadSettings();

            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        void SubscribeToEvents()
        {
            if (gameStateSystem != null)
            {
                gameStateSystem.OnGamePaused?.Subscribe(OnGamePaused);
                gameStateSystem.OnGameResumed?.Subscribe(OnGameResumed);
                gameStateSystem.OnMouseSensitivityChanged?.Subscribe(UpdateSensitivityDisplay);
            }
        }

        void UnsubscribeFromEvents()
        {
            if (gameStateSystem != null)
            {
                gameStateSystem.OnGamePaused?.Unsubscribe(OnGamePaused);
                gameStateSystem.OnGameResumed?.Unsubscribe(OnGameResumed);
                gameStateSystem.OnMouseSensitivityChanged?.Unsubscribe(UpdateSensitivityDisplay);
            }
        }

        void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        void SetupUI()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(ResumeGame);

            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);

            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.minValue = gameStateSystem?.minSensitivity ?? 10f;
                mouseSensitivitySlider.maxValue = gameStateSystem?.maxSensitivity ?? 500f;
                mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (canvasGroup == null && pausePanel != null)
            {
                canvasGroup = pausePanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = pausePanel.AddComponent<CanvasGroup>();
            }

            if (pauseTitle != null)
            {
                pauseTitle.text = "GAME PAUSED";
            }
        }

        void OnGamePaused()
        {
            PlaySound(pauseSound);

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                StartCoroutine(FadeInPanel());
            }

            RefreshSensitivityDisplay();
            gameStateSystem?.SetTransitioning(false);
        }

        void OnGameResumed()
        {
            PlaySound(resumeSound);
            StartCoroutine(ResumeGameCoroutine());
        }

        IEnumerator ResumeGameCoroutine()
        {
            gameStateSystem?.SetTransitioning(true);

            if (canvasGroup != null)
            {
                yield return StartCoroutine(FadeOutPanel());
            }

            if (pausePanel != null)
                pausePanel.SetActive(false);

            gameStateSystem?.SetTransitioning(false);
        }

        IEnumerator FadeInPanel()
        {
            if (canvasGroup == null || gameStateSystem == null) yield break;

            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = gameStateSystem.fadeInDuration;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        IEnumerator FadeOutPanel()
        {
            if (canvasGroup == null || gameStateSystem == null) yield break;

            canvasGroup.alpha = 1f;
            float elapsed = 0f;
            float duration = gameStateSystem.fadeInDuration;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        void PlayButtonSound()
        {
            PlaySound(buttonClickSound);
        }

        public void ResumeGame()
        {
            if (gameStateSystem != null)
                gameStateSystem.ResumeGame();
        }

        public void RestartGame()
        {
            PlayButtonSound();

            if (gameStateSystem != null)
            {
                gameStateSystem.ResetToDefaults();
            }

            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            GameRestartManager.PerformFullRestartAndLoadScene(currentSceneName == progressionSystem.circleSceneName ? progressionSystem.circleSceneName : gameStateSystem.limboSceneName);
        }

        public void GoToMainMenu()
        {
            PlayButtonSound();

            if (gameStateSystem != null)
            {
                gameStateSystem.ResetToDefaults();
            }

            GameRestartManager.PerformFullRestartAndLoadScene(gameStateSystem?.mainMenuSceneName ?? "MainMenu");
        }

        public void ExitGame()
        {
            PlayButtonSound();

            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void OnSensitivityChanged(float value)
        {
            if (gameStateSystem == null) return;

            gameStateSystem.SetMouseSensitivity(value);
        }

        void UpdateSensitivityDisplay()
        {
            if (gameStateSystem == null) return;

            if (mouseSensitivitySlider != null)
            {
                mouseSensitivitySlider.value = gameStateSystem.mouseSensitivity;
            }

            UpdateSensitivityText(gameStateSystem.mouseSensitivity);
        }

        void UpdateSensitivityText(float value)
        {
            if (sensitivityValueText != null)
            {
                sensitivityValueText.text = Mathf.RoundToInt(value).ToString();
            }
        }

        void LoadSettings()
        {
            if (gameStateSystem == null) return;

            gameStateSystem.LoadMouseSensitivity();
            UpdateSensitivityDisplay();
        }

        void RefreshSensitivityDisplay()
        {
            if (gameStateSystem != null)
            {
                UpdateSensitivityDisplay();
            }
        }
    }
}