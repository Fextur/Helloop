using System.Collections;
using UnityEngine;
using Helloop.Systems;

namespace Helloop.UI
{
    [DisallowMultipleComponent]
    public sealed class PortalTransitionOverlay : MonoBehaviour
    {
        public static PortalTransitionOverlay Instance { get; private set; }

        [Header("References")]
        public CanvasGroup overlay;
        public ProgressionSystem progressionSystem;
        public RoomSystem roomSystem;

        [Header("Timings")]
        [Header("Timings")]
        public float fadeInDuration = 0.35f;
        public float fadeOutDuration = 0.8f;

        [Header("Ease Curves")]
        public AnimationCurve easeIn = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve easeOut = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Lifecycle")]
        public bool surviveSceneLoads = true;

        Coroutine running;

        void Awake()
        {
            if (Instance == null) { Instance = this; if (surviveSceneLoads) DontDestroyOnLoad(gameObject); }
            else if (Instance != this) { Destroy(gameObject); return; }

            if (overlay == null) overlay = GetComponent<CanvasGroup>();
            if (overlay != null)
            {
                overlay.alpha = 0f;
                overlay.blocksRaycasts = false;
                overlay.interactable = false;
            }
        }

        void OnEnable()
        {
            if (progressionSystem != null && progressionSystem.OnCircleChanged != null)
                progressionSystem.OnCircleChanged.Subscribe(OnPortalUsed);

            if (roomSystem != null && roomSystem.OnGenerationComplete != null)
                roomSystem.OnGenerationComplete.Subscribe(OnGenerationComplete);
        }

        void OnDisable()
        {
            if (progressionSystem != null && progressionSystem.OnCircleChanged != null)
                progressionSystem.OnCircleChanged.Unsubscribe(OnPortalUsed);

            if (roomSystem != null && roomSystem.OnGenerationComplete != null)
                roomSystem.OnGenerationComplete.Unsubscribe(OnGenerationComplete);
        }

        public void FadeInNow(float durationOverride = -1f)
        {
            if (durationOverride > 0f) fadeInDuration = durationOverride;
            StartFade(1f, fadeInDuration, easeIn);
        }

        void OnPortalUsed() => StartFade(1f, fadeInDuration, easeIn);

        void OnGenerationComplete() => StartFade(0f, fadeOutDuration, easeOut);

        void StartFade(float target, float duration, AnimationCurve curve)
        {
            if (overlay == null) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(FadeRoutine(target, duration, curve));
        }

        IEnumerator FadeRoutine(float targetAlpha, float duration, AnimationCurve curve)
        {
            float start = overlay.alpha;
            if (targetAlpha > start) { overlay.blocksRaycasts = true; overlay.interactable = false; }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                overlay.alpha = Mathf.Lerp(start, targetAlpha, curve.Evaluate(k));
                yield return null;
            }
            overlay.alpha = targetAlpha;

            if (Mathf.Approximately(targetAlpha, 0f))
            {
                overlay.blocksRaycasts = false;
                overlay.interactable = false;
            }
        }
    }
}
