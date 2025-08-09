using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Helloop.Events;

namespace Helloop.UI
{
    public class StatusEffectUIController : MonoBehaviour
    {
        [Header("Status Effects UI")]
        public GameObject dashCooldownIcon;

        [Header("Events")]
        public GameEvent OnDashAvailable;
        public GameEvent OnDashUnavailable;

        private bool dashIconActive = false;
        private Coroutine dashIconCoroutine;

        void Start()
        {
            InitializeStatusEffects();
            SubscribeToEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            OnDashAvailable?.Subscribe(HideDashCooldownIcon);
            OnDashUnavailable?.Subscribe(ShowDashCooldownIcon);
        }

        void UnsubscribeFromEvents()
        {
            OnDashAvailable?.Unsubscribe(HideDashCooldownIcon);
            OnDashUnavailable?.Unsubscribe(ShowDashCooldownIcon);
        }

        void InitializeStatusEffects()
        {
            if (dashCooldownIcon != null)
                dashCooldownIcon.SetActive(false);
        }

        void ShowDashCooldownIcon()
        {
            if (dashCooldownIcon == null || dashIconActive) return;

            dashIconActive = true;
            dashCooldownIcon.SetActive(true);

            if (dashIconCoroutine != null)
                StopCoroutine(dashIconCoroutine);

            dashIconCoroutine = StartCoroutine(PulseDashIcon());
        }

        void HideDashCooldownIcon()
        {
            if (dashCooldownIcon == null || !dashIconActive) return;

            dashIconActive = false;
            dashCooldownIcon.SetActive(false);

            if (dashIconCoroutine != null)
            {
                StopCoroutine(dashIconCoroutine);
                dashIconCoroutine = null;
            }
        }

        IEnumerator PulseDashIcon()
        {
            if (!dashCooldownIcon.TryGetComponent<Image>(out var iconImage))
                yield break;

            while (dashIconActive)
            {
                float time = 0f;
                while (time < 1f && dashIconActive)
                {
                    time += Time.deltaTime * 2f;
                    float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(time * Mathf.PI) + 1f) / 2f);
                    Color color = iconImage.color;
                    color.a = alpha;
                    iconImage.color = color;
                    yield return null;
                }
            }
        }
    }
}