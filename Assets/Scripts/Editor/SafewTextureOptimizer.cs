
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Helloop.Editor
{

#if UNITY_EDITOR
    public class SafeTextureOptimizer : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> selectedTextures = new List<string>();
        private Dictionary<string, TextureImportSettings> originalSettings = new Dictionary<string, TextureImportSettings>();

        [System.Serializable]
        public class TextureImportSettings
        {
            public int maxTextureSize;
            public TextureImporterCompression compression;
            public bool isReadable;
            public bool mipmapEnabled;
            public string path;
        }

        [MenuItem("Helloop/Safe Texture Optimizer")]
        public static void ShowWindow()
        {
            GetWindow<SafeTextureOptimizer>("Safe Texture Optimizer");
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("🛡️ SAFE Texture Optimizer for Helloop", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This tool lets you test optimizations safely on small batches!", MessageType.Info);

            EditorGUILayout.Space();

            DrawBackupSection();

            EditorGUILayout.Space();

            DrawAnalysisSection();

            EditorGUILayout.Space();

            DrawSafeTestingSection();

            EditorGUILayout.Space();

            DrawRevertSection();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawBackupSection()
        {
            EditorGUILayout.LabelField("📦 Step 1: Create Backup", EditorStyles.boldLabel);

            if (GUILayout.Button("🛡️ BACKUP ALL TEXTURE SETTINGS", GUILayout.Height(30)))
            {
                BackupAllTextureSettings();
            }

            EditorGUILayout.HelpBox("Always backup first! This saves current settings so you can revert.", MessageType.Warning);
        }

        void DrawAnalysisSection()
        {
            EditorGUILayout.LabelField("🔍 Step 2: Analyze Your Textures", EditorStyles.boldLabel);

            if (GUILayout.Button("📊 Find Largest Textures"))
            {
                AnalyzeLargestTextures();
            }

            if (selectedTextures.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {selectedTextures.Count} large textures:");

                for (int i = 0; i < Mathf.Min(10, selectedTextures.Count); i++)
                {
                    string path = selectedTextures[i];
                    EditorGUILayout.BeginHorizontal();

                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture != null)
                    {
                        EditorGUILayout.LabelField($"{texture.name} - {texture.width}x{texture.height}");

                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = texture;
                            EditorGUIUtility.PingObject(texture);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        void DrawSafeTestingSection()
        {
            EditorGUILayout.LabelField("🧪 Step 3: Safe Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("🔬 Test on 5 LARGEST Textures Only", GUILayout.Height(25)))
            {
                TestOptimizeTopTextures(5);
            }

            if (GUILayout.Button("🔬 Test on 20 Textures", GUILayout.Height(25)))
            {
                TestOptimizeTopTextures(20);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("💪 Full Optimization (Only after testing!):");

            if (GUILayout.Button("⚡ OPTIMIZE ALL TEXTURES", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Full Optimization",
                    "This will optimize ALL textures. Make sure you've tested first!",
                    "I've tested, do it!", "Cancel"))
                {
                    OptimizeAllTextures();
                }
            }

            EditorGUILayout.HelpBox("Only click 'OPTIMIZE ALL' after you've tested and you're happy with results!", MessageType.Warning);
        }

        void DrawRevertSection()
        {
            EditorGUILayout.LabelField("🔄 Step 4: Revert if Needed", EditorStyles.boldLabel);

            if (originalSettings.Count > 0)
            {
                EditorGUILayout.LabelField($"Backup contains {originalSettings.Count} texture settings");

                if (GUILayout.Button("🔄 REVERT ALL CHANGES", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Revert Changes",
                        "This will restore all textures to their original settings.",
                        "Yes, revert", "Cancel"))
                    {
                        RevertAllChanges();
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No backup found. Create backup first!", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("💾 Save Backup to File"))
            {
                SaveBackupToFile();
            }

            if (GUILayout.Button("📁 Load Backup from File"))
            {
                LoadBackupFromFile();
            }
        }

        void BackupAllTextureSettings()
        {
            originalSettings.Clear();
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");

            EditorUtility.DisplayProgressBar("Backing up", "Saving texture settings...", 0);

            for (int i = 0; i < textureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    originalSettings[path] = new TextureImportSettings
                    {
                        maxTextureSize = importer.maxTextureSize,
                        compression = importer.textureCompression,
                        isReadable = importer.isReadable,
                        mipmapEnabled = importer.mipmapEnabled,
                        path = path
                    };
                }

                EditorUtility.DisplayProgressBar("Backing up", $"Texture {i + 1}/{textureGuids.Length}", (float)i / textureGuids.Length);
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"✅ Backed up {originalSettings.Count} texture settings");
            EditorUtility.DisplayDialog("Backup Complete",
                $"✅ Successfully backed up {originalSettings.Count} texture settings!\nYou can now safely test optimizations.",
                "Great!");
        }

        void AnalyzeLargestTextures()
        {
            selectedTextures.Clear();
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
            List<(string path, long size, int width, int height)> textureInfo = new List<(string, long, int, int)>();

            foreach (string guid in textureGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture != null)
                {
                    long size = 0;
                    if (File.Exists(path))
                    {
                        FileInfo fileInfo = new FileInfo(path);
                        size = fileInfo.Length;
                    }

                    textureInfo.Add((path, size, texture.width, texture.height));
                }
            }

            textureInfo.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));

            selectedTextures = textureInfo.Take(50).Select(x => x.path).ToList();

            Debug.Log($"📊 Found {selectedTextures.Count} largest textures");
        }

        void TestOptimizeTopTextures(int count)
        {
            if (selectedTextures.Count == 0)
            {
                EditorUtility.DisplayDialog("No Textures", "Run 'Find Largest Textures' first!", "OK");
                return;
            }

            List<string> texturesToOptimize = selectedTextures.Take(count).ToList();

            EditorUtility.DisplayProgressBar("Testing Optimization", "Optimizing test textures...", 0);

            for (int i = 0; i < texturesToOptimize.Count; i++)
            {
                string path = texturesToOptimize[i];
                OptimizeTexture(path);

                EditorUtility.DisplayProgressBar("Testing Optimization",
                    $"Optimizing {i + 1}/{texturesToOptimize.Count}",
                    (float)i / texturesToOptimize.Count);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log($"✅ Test optimization complete on {count} textures");
            EditorUtility.DisplayDialog("Test Complete",
                $"✅ Optimized {count} textures as a test!\n\n" +
                "🔍 Check your game - does it still look good?\n" +
                "📦 Build the game and check the new size!\n\n" +
                "If happy: run full optimization\n" +
                "If not: revert changes",
                "Got it!");
        }

        void OptimizeAllTextures()
        {
            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");

            EditorUtility.DisplayProgressBar("Full Optimization", "Optimizing all textures...", 0);

            for (int i = 0; i < textureGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                OptimizeTexture(path);

                EditorUtility.DisplayProgressBar("Full Optimization",
                    $"Optimizing {i + 1}/{textureGuids.Length}",
                    (float)i / textureGuids.Length);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log($"✅ Full optimization complete on {textureGuids.Length} textures");
        }

        void OptimizeTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = false;

            if (importer.maxTextureSize > 1024)
            {
                importer.maxTextureSize = 1024;
                changed = true;
            }

            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                changed = true;
            }

            if (importer.isReadable && !IsUITexture(path))
            {
                importer.isReadable = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        void RevertAllChanges()
        {
            int reverted = 0;

            EditorUtility.DisplayProgressBar("Reverting", "Restoring original settings...", 0);

            var settingsList = originalSettings.ToList();
            for (int i = 0; i < settingsList.Count; i++)
            {
                var kvp = settingsList[i];
                string path = kvp.Key;
                var settings = kvp.Value;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.maxTextureSize = settings.maxTextureSize;
                    importer.textureCompression = settings.compression;
                    importer.isReadable = settings.isReadable;
                    importer.mipmapEnabled = settings.mipmapEnabled;
                    importer.SaveAndReimport();
                    reverted++;
                }

                EditorUtility.DisplayProgressBar("Reverting",
                    $"Restoring {i + 1}/{settingsList.Count}",
                    (float)i / settingsList.Count);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            Debug.Log($"✅ Reverted {reverted} textures to original settings");
            EditorUtility.DisplayDialog("Revert Complete",
                $"✅ Successfully reverted {reverted} textures!",
                "Great!");
        }

        void SaveBackupToFile()
        {
            string path = EditorUtility.SaveFilePanel("Save Backup", "", "texture_backup", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = JsonUtility.ToJson(new Serialization<TextureImportSettings>(originalSettings.Values.ToList()));
                File.WriteAllText(path, json);
                Debug.Log($"✅ Backup saved to {path}");
            }
        }

        void LoadBackupFromFile()
        {
            string path = EditorUtility.OpenFilePanel("Load Backup", "", "json");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var loaded = JsonUtility.FromJson<Serialization<TextureImportSettings>>(json);

                originalSettings.Clear();
                foreach (var setting in loaded.target)
                {
                    originalSettings[setting.path] = setting;
                }

                Debug.Log($"✅ Loaded backup with {originalSettings.Count} settings");
            }
        }

        bool IsUITexture(string path)
        {
            return path.ToLower().Contains("ui") ||
                   path.ToLower().Contains("hud") ||
                   path.ToLower().Contains("menu");
        }

        [System.Serializable]
        public class Serialization<T>
        {
            public List<T> target;
            public Serialization(List<T> target) { this.target = target; }
        }
    }
#endif

}