using UnityEngine;
using Helloop.Events;
using Helloop.Data;

namespace Helloop.Systems
{
    [CreateAssetMenu(fileName = "ProgressionSystem", menuName = "Helloop/Systems/ProgressionSystem")]
    public class ProgressionSystem : ScriptableObject
    {
        [Header("Current Progress")]
        public CircleData currentCircle;

        [Header("Default Configuration")]
        public CircleData defaultCircle;

        [Header("Scene Configuration")]
        public string limboSceneName = "Limbo";
        public string circleSceneName = "Circle";
        public string paradiseSceneName = "Paradise";

        [Header("Events")]
        public GameEvent OnCircleChanged;
        public GameEvent OnProgressionUpdated;

        public void GoToNextCircle()
        {
            currentCircle = currentCircle.nextCircle;

            OnCircleChanged?.Raise();
            OnProgressionUpdated?.Raise();
        }

        public void SetInitialCircle(CircleData initialNextCircle)
        {
            currentCircle = defaultCircle;
            OnProgressionUpdated?.Raise();
        }

        public CircleData GetCurrentCircle() => currentCircle;

        public int GetCurrentCircleNumber() => currentCircle?.circleLevel ?? 1;

        public string GetCurrentCircleName() => IsInParadise() ? "Paradise" : currentCircle?.GetFullCircleName() ?? "Unknown";

        public bool IsInParadise() => currentCircle == null;

        public bool IsInLimbo() => currentCircle != null && currentCircle.circleLevel == 1;

        void OnEnable()
        {
            if (currentCircle == null && defaultCircle != null)
            {
                ResetToDefaults();
            }
        }

        public void ResetToDefaults()
        {
            currentCircle = defaultCircle;
        }
    }
}