using UnityEngine;
using UnityEngine.SceneManagement;
using Helloop.Systems;
using Helloop.Audio;
using Helloop.Enemies;

namespace Helloop.Core
{
    public static class GameRestartManager
    {
        [Header("Scene Settings")]
        public static string limboSceneName = "Limbo";
        public static string deathSceneName = "Death";
        public static string victorySceneName = "Victory";

        public static void LoadDeathScreen(string currentFloorName = null)
        {
            StopGameMusic();

            CleanupPersistentObjects();
            SceneManager.LoadScene(deathSceneName);
        }

        public static void LoadVictoryScreen()
        {
            StopGameMusic();

            CleanupPersistentObjects();
            SceneManager.LoadScene(victorySceneName);
        }

        public static void PerformFullRestartAndLoadScene(string targetScene)
        {
            StopGameMusicImmediate();

            CleanupPersistentObjects();
            ResetGameState();
            ResetSystems();
            SceneManager.LoadScene(targetScene);
        }

        static void StopGameMusic()
        {
            PersistentMusicManager musicManager = PersistentMusicManager.Instance;
            if (musicManager != null)
            {
                musicManager.StopGameMusic();
            }
        }

        static void StopGameMusicImmediate()
        {
            PersistentMusicManager musicManager = PersistentMusicManager.Instance;
            if (musicManager != null)
            {
                musicManager.StopMusicImmediate();
            }
        }

        static void CleanupPersistentObjects()
        {
            DestroyPersistentManager(PersistentMusicManager.Instance, "PersistentMusicManager");
        }

        static void DestroyPersistentManager(MonoBehaviour manager, string name)
        {
            if (manager != null)
            {
                Object.Destroy(manager.gameObject);
            }
        }

        static void ResetGameState()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            System.GC.Collect();
        }

        static void ResetSystems()
        {
            PlayerSystem[] playerSystems = Resources.FindObjectsOfTypeAll<PlayerSystem>();
            foreach (PlayerSystem playerSystem in playerSystems)
            {
                if (playerSystem != null)
                {
                    playerSystem.ResetToDefaults();
                }
            }

            WeaponSystem[] weaponSystems = Resources.FindObjectsOfTypeAll<WeaponSystem>();
            foreach (WeaponSystem weaponSystem in weaponSystems)
            {
                if (weaponSystem != null)
                {
                    weaponSystem.ResetToDefaults();
                }
            }

            ProgressionSystem[] progressionSystems = Resources.FindObjectsOfTypeAll<ProgressionSystem>();
            foreach (ProgressionSystem progressionSystem in progressionSystems)
            {
                if (progressionSystem != null)
                {
                    progressionSystem.ResetToDefaults();
                }
            }
        }
    }
}