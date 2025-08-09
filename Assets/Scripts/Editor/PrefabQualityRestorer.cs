
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Helloop.Editor
{
#if UNITY_EDITOR
    public class PrefabQualityRestorer : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<Object> selectedAssets = new List<Object>();
        private List<string> foundTextures = new List<string>();
        private List<string> foundModels = new List<string>();
        private Dictionary<string, AssetQualityProfile> qualityProfiles = new Dictionary<string, AssetQualityProfile>();

        [System.Serializable]
        public class AssetQualityProfile
        {
            public string name;
            public int textureSize;
            public TextureImporterCompression compression;
            public bool highQuality;
            public Color profileColor;
        }

        [MenuItem("Helloop/Prefab Quality Restorer")]
        public static void ShowWindow()
        {
            GetWindow<PrefabQualityRestorer>("Prefab Quality Restorer");
        }

        void OnEnable()
        {
            SetupQualityProfiles();
        }

        void SetupQualityProfiles()
        {
            qualityProfiles.Clear();

            qualityProfiles["Hero Assets"] = new AssetQualityProfile
            {
                name = "Hero Assets (Player, Main Characters)",
                textureSize = 1024,
                compression = TextureImporterCompression.Compressed,
                highQuality = true,
                profileColor = Color.red
            };

            qualityProfiles["Important Objects"] = new AssetQualityProfile
            {
                name = "Important Objects (Weapons, Key Items)",
                textureSize = 512,
                compression = TextureImporterCompression.Compressed,
                highQuality = true,
                profileColor = Color.yellow
            };

            qualityProfiles["UI Elements"] = new AssetQualityProfile
            {
                name = "UI Elements (Menus, HUD)",
                textureSize = 1024,
                compression = TextureImporterCompression.Compressed,
                highQuality = true,
                profileColor = Color.cyan
            };

            qualityProfiles["Fix Pixelated Enemy"] = new AssetQualityProfile
            {
                name = "Fix Pixelated Enemy (High Quality)",
                textureSize = 1024,
                compression = TextureImporterCompression.CompressedHQ,
                highQuality = true,
                profileColor = Color.magenta
            };

            qualityProfiles["Environment"] = new AssetQualityProfile
            {
                name = "Environment Objects",
                textureSize = 512,
                compression = TextureImporterCompression.Compressed,
                highQuality = false,
                profileColor = Color.green
            };
        }

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField("🎨 Prefab-Aware Quality Restorer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Works with prefabs! Automatically finds all textures and models used by your selected prefabs/objects.", MessageType.Info);

            EditorGUILayout.Space();

            DrawAssetSelection();

            EditorGUILayout.Space();

            DrawFoundDependencies();

            EditorGUILayout.Space();

            DrawQualityProfiles();

            EditorGUILayout.Space();

            DrawQuickActions();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawAssetSelection()
        {
            EditorGUILayout.LabelField("📂 Step 1: Select Assets (Prefabs, Models, Textures)", EditorStyles.boldLabel);

            if (GUILayout.Button("🎯 Use Currently Selected Assets", GUILayout.Height(25)))
            {
                selectedAssets.Clear();
                selectedAssets.AddRange(Selection.objects);

                foundTextures.Clear();
                foundModels.Clear();

                FindDependencies();
            }

            if (selectedAssets.Count > 0)
            {
                EditorGUILayout.LabelField($"Selected {selectedAssets.Count} assets:");

                for (int i = 0; i < Mathf.Min(5, selectedAssets.Count); i++)
                {
                    if (selectedAssets[i] != null)
                    {
                        EditorGUILayout.BeginHorizontal();

                        string assetType = GetAssetTypeString(selectedAssets[i]);
                        EditorGUILayout.LabelField($"{assetType} {selectedAssets[i].name}");

                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                        {
                            EditorGUIUtility.PingObject(selectedAssets[i]);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (selectedAssets.Count > 5)
                {
                    EditorGUILayout.LabelField($"... and {selectedAssets.Count - 5} more");
                }

                if (GUILayout.Button("Clear Selection"))
                {
                    selectedAssets.Clear();
                    foundTextures.Clear();
                    foundModels.Clear();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select prefabs, models, or textures in Project window, then click 'Use Currently Selected Assets'", MessageType.Info);
            }
        }

        void DrawFoundDependencies()
        {
            if (foundTextures.Count == 0 && foundModels.Count == 0) return;

            EditorGUILayout.LabelField("🔍 Step 2: Found Dependencies", EditorStyles.boldLabel);

            if (foundTextures.Count > 0)
            {
                EditorGUILayout.LabelField($"📷 Found {foundTextures.Count} textures:");
                for (int i = 0; i < Mathf.Min(8, foundTextures.Count); i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  • {System.IO.Path.GetFileName(foundTextures[i])}");

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(foundTextures[i]);
                        if (texture != null)
                        {
                            Selection.activeObject = texture;
                            EditorGUIUtility.PingObject(texture);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (foundTextures.Count > 8)
                {
                    EditorGUILayout.LabelField($"  ... and {foundTextures.Count - 8} more textures");
                }
            }

            if (foundModels.Count > 0)
            {
                EditorGUILayout.LabelField($"🎮 Found {foundModels.Count} models:");
                for (int i = 0; i < Mathf.Min(5, foundModels.Count); i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  • {System.IO.Path.GetFileName(foundModels[i])}");

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        var model = AssetDatabase.LoadAssetAtPath<GameObject>(foundModels[i]);
                        if (model != null)
                        {
                            Selection.activeObject = model;
                            EditorGUIUtility.PingObject(model);
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (foundModels.Count > 5)
                {
                    EditorGUILayout.LabelField($"  ... and {foundModels.Count - 5} more models");
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("🔄 Refresh Dependencies"))
            {
                FindDependencies();
            }
        }

        void DrawQualityProfiles()
        {
            if (foundTextures.Count == 0 && foundModels.Count == 0)
            {
                EditorGUILayout.HelpBox("Select assets and find dependencies first!", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("⭐ Step 3: Apply Quality Profile", EditorStyles.boldLabel);

            foreach (var profile in qualityProfiles.Values)
            {
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = profile.profileColor;
                if (GUILayout.Button($"Apply: {profile.name}", GUILayout.Height(30)))
                {
                    ApplyQualityProfile(profile);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"   Size: {profile.textureSize}px, Quality: {(profile.highQuality ? "High" : "Normal")}");
                EditorGUILayout.Space();
            }
        }

        void DrawQuickActions()
        {
            if (foundTextures.Count == 0 && foundModels.Count == 0) return;

            EditorGUILayout.LabelField("⚡ Step 4: Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("⬆️ Increase All Quality"))
            {
                IncreaseQuality();
            }

            if (GUILayout.Button("⬇️ Decrease All Quality"))
            {
                DecreaseQuality();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Set texture size for all found textures:");
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("2048px")) SetAllTextureSize(2048);
            if (GUILayout.Button("1024px")) SetAllTextureSize(1024);
            if (GUILayout.Button("512px")) SetAllTextureSize(512);
            if (GUILayout.Button("256px")) SetAllTextureSize(256);

            EditorGUILayout.EndHorizontal();
        }

        void FindDependencies()
        {
            foundTextures.Clear();
            foundModels.Clear();

            HashSet<string> allDependencies = new HashSet<string>();

            foreach (var asset in selectedAssets)
            {
                if (asset == null) continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);

                string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);

                foreach (string dep in dependencies)
                {
                    allDependencies.Add(dep);
                }
            }

            foreach (string dependency in allDependencies)
            {
                if (dependency.EndsWith(".png") || dependency.EndsWith(".jpg") || dependency.EndsWith(".tga") || dependency.EndsWith(".psd"))
                {
                    foundTextures.Add(dependency);
                }
                else if (dependency.EndsWith(".fbx") || dependency.EndsWith(".obj") || dependency.EndsWith(".dae"))
                {
                    foundModels.Add(dependency);
                }
            }

            Debug.Log($"🔍 Found {foundTextures.Count} textures and {foundModels.Count} models in dependencies");
        }

        void ApplyQualityProfile(AssetQualityProfile profile)
        {
            int processedTextures = 0;
            int processedModels = 0;

            EditorUtility.DisplayProgressBar("Applying Quality Profile", $"Applying {profile.name}...", 0);

            for (int i = 0; i < foundTextures.Count; i++)
            {
                string path = foundTextures[i];
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer != null)
                {
                    importer.maxTextureSize = profile.textureSize;
                    importer.textureCompression = profile.compression;

                    if (profile.highQuality)
                    {
                        var platformSettings = importer.GetPlatformTextureSettings("Standalone");
                        platformSettings.compressionQuality = 100;
                        importer.SetPlatformTextureSettings(platformSettings);
                    }

                    importer.SaveAndReimport();
                    processedTextures++;
                }

                EditorUtility.DisplayProgressBar("Applying Quality Profile",
                    $"Processing textures {i + 1}/{foundTextures.Count}",
                    (float)i / (foundTextures.Count + foundModels.Count));
            }

            for (int i = 0; i < foundModels.Count; i++)
            {
                string path = foundModels[i];
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer != null)
                {
                    if (profile.highQuality)
                    {
                        importer.meshCompression = ModelImporterMeshCompression.Off;
                    }
                    else
                    {
                        importer.meshCompression = ModelImporterMeshCompression.Medium;
                    }

                    importer.SaveAndReimport();
                    processedModels++;
                }

                EditorUtility.DisplayProgressBar("Applying Quality Profile",
                    $"Processing models {i + 1}/{foundModels.Count}",
                    (float)(foundTextures.Count + i) / (foundTextures.Count + foundModels.Count));
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Applied {profile.name}: {processedTextures} textures, {processedModels} models");
            EditorUtility.DisplayDialog("Quality Profile Applied",
                $"✅ Applied {profile.name}!\n\n" +
                $"Processed:\n" +
                $"• {processedTextures} textures\n" +
                $"• {processedModels} models\n\n" +
                "Check your prefabs - they should look better now!",
                "Great!");
        }

        void IncreaseQuality()
        {
            int processed = 0;

            foreach (string path in foundTextures)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    int newSize = Mathf.Min(importer.maxTextureSize * 2, 2048);
                    if (newSize != importer.maxTextureSize)
                    {
                        importer.maxTextureSize = newSize;
                        importer.SaveAndReimport();
                        processed++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Increased quality for {processed} textures");
        }

        void DecreaseQuality()
        {
            int processed = 0;

            foreach (string path in foundTextures)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    int newSize = Mathf.Max(importer.maxTextureSize / 2, 64);
                    if (newSize != importer.maxTextureSize)
                    {
                        importer.maxTextureSize = newSize;
                        importer.SaveAndReimport();
                        processed++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Decreased quality for {processed} textures");
        }

        void SetAllTextureSize(int size)
        {
            int processed = 0;

            foreach (string path in foundTextures)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.maxTextureSize = size;
                    importer.SaveAndReimport();
                    processed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Set texture size to {size}px for {processed} textures");
        }

        string GetAssetTypeString(Object asset)
        {
            if (asset is GameObject) return "🎮";
            if (asset is Texture2D) return "📷";
            if (asset is Material) return "🎨";
            if (asset is Mesh) return "📐";
            return "📄";
        }
    }
#endif
}