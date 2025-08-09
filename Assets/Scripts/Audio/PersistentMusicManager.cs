using UnityEngine;
using System.Collections;

namespace Helloop.Audio
{
    public class PersistentMusicManager : MonoBehaviour
    {
        [Header("Music Settings")]
        public AudioClip gameThemeMusic;
        public float musicVolume = 0.7f;

        [Header("Loop Settings")]
        public bool enableSeamlessLoop = true;
        public float loopFadeTime = 1f;

        private AudioSource currentMusicSource;
        private bool isMusicPlaying = false;
        private Coroutine loopCoroutine;

        public static PersistentMusicManager Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        void OnDestroy()
        {
            StopSeamlessLoop();
        }

        private void InitializeAudio()
        {
            currentMusicSource = gameObject.AddComponent<AudioSource>();
            currentMusicSource.playOnAwake = false;
            currentMusicSource.loop = !enableSeamlessLoop;
            currentMusicSource.volume = musicVolume;
        }

        public void StartGameMusic()
        {
            if (isMusicPlaying || gameThemeMusic == null) return;

            currentMusicSource.clip = gameThemeMusic;
            currentMusicSource.volume = musicVolume;
            currentMusicSource.Play();
            isMusicPlaying = true;

            if (enableSeamlessLoop)
            {
                StartSeamlessLoop();
            }
        }

        public void StopGameMusic()
        {
            if (!isMusicPlaying || currentMusicSource == null) return;

            StopSeamlessLoop();
            StartCoroutine(FadeOutMusic(2f));
        }

        public void StopMusicImmediate()
        {
            StopSeamlessLoop();

            if (currentMusicSource != null)
            {
                currentMusicSource.Stop();
            }

            isMusicPlaying = false;
        }

        private void StartSeamlessLoop()
        {
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
            }

            loopCoroutine = StartCoroutine(SeamlessLoopCoroutine());
        }

        private void StopSeamlessLoop()
        {
            if (loopCoroutine != null)
            {
                StopCoroutine(loopCoroutine);
                loopCoroutine = null;
            }
        }

        private IEnumerator SeamlessLoopCoroutine()
        {
            while (isMusicPlaying && gameThemeMusic != null && currentMusicSource != null)
            {
                float timeUntilEnd = gameThemeMusic.length - currentMusicSource.time;

                if (timeUntilEnd <= loopFadeTime)
                {
                    StartCoroutine(RestartMusic());
                    yield return new WaitForSeconds(loopFadeTime);
                }

                yield return new WaitForSeconds(0.1f);
            }

            loopCoroutine = null;
        }

        private IEnumerator RestartMusic()
        {
            float originalVolume = currentMusicSource.volume;

            yield return StartCoroutine(FadeVolume(currentMusicSource, originalVolume, 0f, loopFadeTime * 0.5f));

            currentMusicSource.time = 0f;

            yield return StartCoroutine(FadeVolume(currentMusicSource, 0f, originalVolume, loopFadeTime * 0.5f));
        }

        private IEnumerator FadeOutMusic(float fadeTime)
        {
            float startVolume = currentMusicSource.volume;

            yield return StartCoroutine(FadeVolume(currentMusicSource, startVolume, 0f, fadeTime));

            currentMusicSource.Stop();
            isMusicPlaying = false;
        }

        private IEnumerator FadeVolume(AudioSource source, float startVolume, float targetVolume, float fadeTime)
        {
            float elapsed = 0f;

            while (elapsed < fadeTime && source != null)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / fadeTime);
                yield return null;
            }

            if (source != null)
            {
                source.volume = targetVolume;
            }
        }
    }
}