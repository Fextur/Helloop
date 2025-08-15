using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Helloop.Systems;
using Helloop.Core;

namespace Helloop.UI
{
    public class UniversalScreen : MonoBehaviour
    {
        [Header("UI References")]
        public Button startButton;
        public Button startTutorialButton;
        public Button exitButton;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI subtitleText;

        [Header("Content Configuration")]
        public string[] possibleTitles;
        public string[] possibleSubtitles;

        [Header("Dynamic Content (Death Screen Only)")]
        public ProgressionSystem progressionSystem;

        [Header("Audio Settings")]
        public AudioClip backgroundMusic;
        public AudioClip buttonClickSound;
        [Range(0f, 1f)] public float maxMusicVolume = 0.8f;

        [Header("Transition Settings")]
        [Range(0.1f, 5f)] public float fadeInDuration = 2f;
        [Range(0.1f, 3f)] public float fadeOutDuration = 1.5f;

        [Header("Scene Settings")]
        public string limboSceneName = "Limbo";
        public string circleSceneName = "Circle";

        private AudioSource audioSource;
        private bool isTransitioning = false;
        private Coroutine currentMusicFadeCoroutine;

        void Start()
        {
            SetupCursor();
            SetupAudioSource();
            SetupButtons();
            InitializeScreen();
        }

        void SetupCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void SetupAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0f;
        }

        void SetupButtons()
        {
            if (startButton != null)
                startButton.onClick.AddListener(HandleStartAction);
            if (startTutorialButton != null)
                startButton.onClick.AddListener(HandleStartTutorialAction);
            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);
        }

        void InitializeScreen()
        {
            SetRandomTexts();
            StartBackgroundMusic();
        }

        void SetRandomTexts()
        {
            if (titleText != null && possibleTitles.Length > 0)
                titleText.text = possibleTitles[Random.Range(0, possibleTitles.Length)];

            if (subtitleText != null)
            {
                if (progressionSystem != null)
                {
                    string currentFloor = progressionSystem.GetCurrentCircleName();
                    subtitleText.text = $"You made it to: {currentFloor}";
                }
                else if (possibleSubtitles.Length > 0)
                {
                    subtitleText.text = possibleSubtitles[Random.Range(0, possibleSubtitles.Length)];
                }
            }
        }

        void HandleStartTutorialAction()
        {
            PlayButtonSound();
            StartCoroutine(TransitionWithFade(() =>
                GameRestartManager.PerformFullRestartAndLoadScene(limboSceneName)));
        }

        void HandleStartAction()
        {
            PlayButtonSound();
            StartCoroutine(TransitionWithFade(() =>
                GameRestartManager.PerformFullRestartAndLoadScene(circleSceneName)));
        }

        void ExitGame()
        {
            PlayButtonSound();
            StartCoroutine(TransitionWithFade(() =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }));
        }

        IEnumerator TransitionWithFade(System.Action action)
        {
            if (isTransitioning) yield break;
            isTransitioning = true;

            SetButtonsInteractable(false);

            if (currentMusicFadeCoroutine != null)
                StopCoroutine(currentMusicFadeCoroutine);
            currentMusicFadeCoroutine = StartCoroutine(FadeMusicOut());

            yield return currentMusicFadeCoroutine;

            action?.Invoke();
        }

        void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (exitButton != null) exitButton.interactable = interactable;
        }

        void StartBackgroundMusic()
        {
            if (backgroundMusic != null && audioSource != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
                if (currentMusicFadeCoroutine != null)
                    StopCoroutine(currentMusicFadeCoroutine);
                currentMusicFadeCoroutine = StartCoroutine(FadeMusicIn());
            }
        }

        IEnumerator FadeMusicIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(0f, maxMusicVolume, elapsed / fadeInDuration);
                yield return null;
            }
            audioSource.volume = maxMusicVolume;
            currentMusicFadeCoroutine = null;
        }

        IEnumerator FadeMusicOut()
        {
            float elapsed = 0f;
            float startVolume = audioSource.volume;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            audioSource.volume = 0f;
            audioSource.Stop();
            currentMusicFadeCoroutine = null;
        }

        void PlayButtonSound()
        {
            if (buttonClickSound != null)
                AudioSource.PlayClipAtPoint(buttonClickSound, Camera.main.transform.position);
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}