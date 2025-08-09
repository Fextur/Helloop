using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Helloop.Systems;

namespace Helloop.UI
{
    public class PlayerHealthUIController : MonoBehaviour
    {
        [Header("Health UI")]
        public Slider healthBar;
        public TextMeshProUGUI healthText;

        [Header("Lives UI")]
        public GameObject[] lifeIcons;
        public Color aliveHeartColor = Color.red;
        public Color deadHeartColor = Color.gray;

        [Header("Blood Overlays")]
        public Image lightBloodOverlay;
        public Image heavyBloodOverlay;
        public float lightBloodOpacity = 0.3f;
        public float heavyBloodOpacity = 0.6f;
        public float bloodFadeSpeed = 2f;

        [Header("System References")]
        public PlayerSystem playerSystem;

        private float targetLightBloodAlpha = 0f;
        private float targetHeavyBloodAlpha = 0f;
        private Coroutine bloodOverlayCoroutine;

        void Start()
        {
            SubscribeToEvents();
            InitializeUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (bloodOverlayCoroutine != null)
                StopCoroutine(bloodOverlayCoroutine);
        }

        void SubscribeToEvents()
        {
            if (playerSystem != null)
            {
                playerSystem.OnHealthChanged?.Subscribe(UpdateHealthUI);
                playerSystem.OnLivesChanged?.Subscribe(UpdateLivesUI);
            }
        }

        void UnsubscribeFromEvents()
        {
            if (playerSystem != null)
            {
                playerSystem.OnHealthChanged?.Unsubscribe(UpdateHealthUI);
                playerSystem.OnLivesChanged?.Unsubscribe(UpdateLivesUI);
            }
        }

        void InitializeUI()
        {
            InitializeBloodOverlays();
            UpdateHealthUI();
            UpdateLivesUI();
        }

        void InitializeBloodOverlays()
        {
            if (lightBloodOverlay != null)
            {
                lightBloodOverlay.gameObject.SetActive(true);
                SetImageAlpha(lightBloodOverlay, 0f);
            }

            if (heavyBloodOverlay != null)
            {
                heavyBloodOverlay.gameObject.SetActive(true);
                SetImageAlpha(heavyBloodOverlay, 0f);
            }
        }

        void UpdateHealthUI()
        {
            if (playerSystem == null) return;

            float healthPercentage = playerSystem.GetHealthPercentage();

            if (healthBar != null)
                healthBar.value = healthPercentage;

            if (healthText != null)
                healthText.text = $"{Mathf.Ceil(playerSystem.currentHealth)}/{playerSystem.maxHealth}";

            UpdateBloodOverlayTargets(healthPercentage);
        }

        void UpdateLivesUI()
        {
            if (playerSystem == null || lifeIcons == null) return;

            int currentLives = playerSystem.currentLives;
            int maxLives = playerSystem.maxLives;

            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (i < maxLives)
                {
                    lifeIcons[i].SetActive(true);

                    if (lifeIcons[i].TryGetComponent<Image>(out var heartImage))
                    {
                        heartImage.color = (i < currentLives) ? aliveHeartColor : deadHeartColor;
                        heartImage.transform.localScale = (i >= currentLives) ? Vector3.one * 0.7f : Vector3.one;
                    }
                }
                else
                {
                    lifeIcons[i].SetActive(false);
                }
            }
        }

        void UpdateBloodOverlayTargets(float healthPercentage)
        {
            float twoThirdsHealth = 2f / 3f;
            float oneThirdHealth = 1f / 3f;

            float newLightTarget = 0f;
            float newHeavyTarget = 0f;

            if (healthPercentage <= oneThirdHealth)
            {
                newLightTarget = lightBloodOpacity;
                newHeavyTarget = heavyBloodOpacity;
            }
            else if (healthPercentage <= twoThirdsHealth)
            {
                newLightTarget = lightBloodOpacity;
                newHeavyTarget = 0f;
            }

            if (newLightTarget != targetLightBloodAlpha || newHeavyTarget != targetHeavyBloodAlpha)
            {
                targetLightBloodAlpha = newLightTarget;
                targetHeavyBloodAlpha = newHeavyTarget;

                if (bloodOverlayCoroutine != null)
                    StopCoroutine(bloodOverlayCoroutine);

                bloodOverlayCoroutine = StartCoroutine(UpdateBloodOverlaysCoroutine());
            }
        }

        IEnumerator UpdateBloodOverlaysCoroutine()
        {
            float currentLightAlpha = lightBloodOverlay != null ? lightBloodOverlay.color.a : 0f;
            float currentHeavyAlpha = heavyBloodOverlay != null ? heavyBloodOverlay.color.a : 0f;

            while (Mathf.Abs(currentLightAlpha - targetLightBloodAlpha) > 0.01f ||
                   Mathf.Abs(currentHeavyAlpha - targetHeavyBloodAlpha) > 0.01f)
            {
                currentLightAlpha = Mathf.Lerp(currentLightAlpha, targetLightBloodAlpha, bloodFadeSpeed * Time.deltaTime);
                currentHeavyAlpha = Mathf.Lerp(currentHeavyAlpha, targetHeavyBloodAlpha, bloodFadeSpeed * Time.deltaTime);

                if (lightBloodOverlay != null)
                    SetImageAlpha(lightBloodOverlay, currentLightAlpha);

                if (heavyBloodOverlay != null)
                    SetImageAlpha(heavyBloodOverlay, currentHeavyAlpha);

                yield return null;
            }

            if (lightBloodOverlay != null)
                SetImageAlpha(lightBloodOverlay, targetLightBloodAlpha);

            if (heavyBloodOverlay != null)
                SetImageAlpha(heavyBloodOverlay, targetHeavyBloodAlpha);

            bloodOverlayCoroutine = null;
        }

        void SetImageAlpha(Image image, float alpha)
        {
            if (image != null)
            {
                Color color = image.color;
                color.a = alpha;
                image.color = color;
            }
        }
    }
}