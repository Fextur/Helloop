using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Helloop.Events;
using Helloop.Enemies;

namespace Helloop.UI
{
    public class EnemyUIController : MonoBehaviour
    {
        [Header("Enemy Health UI")]
        public GameObject enemyHealthPanel;
        public Slider enemyHealthBar;
        public TextMeshProUGUI enemyNameText;

        [Header("Event System")]
        public EnemyHitEvent onEnemyHit;

        private Coroutine hideEnemyHealthCoroutine;

        void Start()
        {
            SubscribeToEvents();
            InitializeUI();
        }

        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void SubscribeToEvents()
        {
            if (onEnemyHit != null)
            {
                onEnemyHit.Subscribe(ShowEnemyHealth);
            }
        }

        void UnsubscribeFromEvents()
        {
            if (onEnemyHit != null)
            {
                onEnemyHit.Unsubscribe(ShowEnemyHealth);
            }
        }

        void InitializeUI()
        {
            if (enemyHealthPanel != null)
                enemyHealthPanel.SetActive(false);
        }

        public void ShowEnemyHealth(EnemyHealth enemyHealth)
        {
            if (enemyHealthPanel == null || enemyHealth == null) return;

            enemyHealthPanel.SetActive(true);

            if (enemyNameText != null)
            {
                Enemy enemy = enemyHealth.GetComponent<Enemy>();
                if (enemy?.enemyData != null)
                    enemyNameText.text = enemy.enemyData.enemyName;
                else
                    enemyNameText.text = "Enemy";
            }

            if (enemyHealthBar != null)
            {
                float healthPercentage = enemyHealth.GetCurrentHealth() / enemyHealth.GetMaxHealth();
                enemyHealthBar.value = healthPercentage;
            }

            if (hideEnemyHealthCoroutine != null)
                StopCoroutine(hideEnemyHealthCoroutine);

            hideEnemyHealthCoroutine = StartCoroutine(HideEnemyHealthAfterDelay());
        }

        IEnumerator HideEnemyHealthAfterDelay()
        {
            yield return new WaitForSeconds(3f);

            if (enemyHealthPanel != null)
                enemyHealthPanel.SetActive(false);
        }
    }
}