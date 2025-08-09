#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class AudioOptimizerWindow : EditorWindow
{
    // ---------- UI ----------
    [MenuItem("Tools/Optimization/Audio Optimizer")]
    private static void Open()
    {
        var w = GetWindow<AudioOptimizerWindow>("Audio Optimizer");
        w.minSize = new Vector2(520, 420);
    }

    [Serializable] private class Stats { public int total, changed, skipped; }

    private bool onlyInSelection = true;
    private bool dryRun = true;
    private bool includePackages = false; // usually keep off
    private string[] nameHintsMusic = new[] { "music", "ambience", "ambient", "bgm", "loop" };
    private string[] nameHintsUI = new[] { "ui_", "click", "hover", "menu" };
    private string[] nameHintsVO = new[] { "vo_", "voice", "voiceover", "line_" };

    private float uiMaxLenSec = 0.45f;     // very short UI blips
    private float sfxMaxLenSec = 3.0f;     // typical one-shots
    private float voMaxLenSec = 15.0f;    // voice lines; longer => treat as music/amb

    private Vector2 scroll;
    private readonly List<string> log = new();

    void OnGUI()
    {
        GUILayout.Label("Scan Scope", EditorStyles.boldLabel);
        onlyInSelection = EditorGUILayout.ToggleLeft("Only in Project selection (folders/files)", onlyInSelection);
        includePackages = EditorGUILayout.ToggleLeft("Include Packages (rarely needed)", includePackages);

        GUILayout.Space(8);
        GUILayout.Label("Classification Hints (optional)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Name contains → treated as that category");
        DrawHints("Music/Ambience", ref nameHintsMusic);
        DrawHints("UI SFX", ref nameHintsUI);
        DrawHints("Voice", ref nameHintsVO);

        GUILayout.Space(8);
        GUILayout.Label("Length thresholds (seconds)", EditorStyles.boldLabel);
        uiMaxLenSec = EditorGUILayout.Slider("UI max length", uiMaxLenSec, 0.05f, 1.0f);
        sfxMaxLenSec = EditorGUILayout.Slider("SFX max length", sfxMaxLenSec, 0.5f, 6.0f);
        voMaxLenSec = EditorGUILayout.Slider("VO max length", voMaxLenSec, 3.0f, 60.0f);

        GUILayout.Space(8);
        dryRun = EditorGUILayout.ToggleLeft("Dry Run (preview changes, don’t apply)", dryRun);

        GUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(dryRun ? "Preview" : "Optimize", GUILayout.Height(32)))
                Run();

            if (GUILayout.Button("Clear Log"))
                log.Clear();
        }

        GUILayout.Space(6);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var line in log) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(6);
        EditorGUILayout.HelpBox(
            "Heuristics:\n" +
            "• UI (≤0.45s): ADPCM, Mono, Decompress On Load, 16–22kHz.\n" +
            "• SFX (≤3s): Vorbis Q≈0.35, Mono, Decompress On Load, 22kHz.\n" +
            "• VO (≤15s): Vorbis Q≈0.4, Mono, Compressed In Memory, 24–32kHz.\n" +
            "• Music/Ambience: Vorbis Q≈0.5–0.6, Stereo, Streaming, 44.1kHz.\n" +
            "Name hints override length buckets (e.g., files with 'music' treated as Music).",
            MessageType.Info);
    }

    void DrawHints(string label, ref string[] arr)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, GUILayout.Width(120));
            string joined = string.Join(", ", arr);
            string edited = EditorGUILayout.TextField(joined);
            if (edited != joined)
                arr = edited.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLowerInvariant()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }
    }

    // ---------- Core ----------
    enum Kind { UI, SFX, VO, Music }

    void Run()
    {
        log.Clear();
        var stats = new Stats();

        var guids = FindAudioClipGuids();
        stats.total = guids.Length;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
            {
                stats.skipped++;
                continue;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { stats.skipped++; continue; }

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (importer == null) { stats.skipped++; continue; }

            var before = importer.defaultSampleSettings;

            var kind = Classify(path, clip);
            var after = SuggestSettings(kind, clip, before);

            bool changed = !SampleSettingsEqual(before, after)
                           || importer.forceToMono != ForceToMono(kind)
                           || importer.loadInBackground != LoadInBackground(kind);

            if (dryRun)
            {
                if (changed)
                    log.Add($"[PREVIEW] {path}\n  {before.ToPretty()}  -->  {after.ToPretty()}  | mono:{importer.forceToMono}->{ForceToMono(kind)}");
                continue;
            }

            if (changed)
            {
                Undo.RecordObject(importer, "Optimize Audio Import");
                importer.forceToMono = ForceToMono(kind);
                importer.loadInBackground = LoadInBackground(kind);
                importer.defaultSampleSettings = after;

                try
                {
                    AssetDatabase.WriteImportSettingsIfDirty(path);
                    importer.SaveAndReimport();
                    stats.changed++;
                    log.Add($"[OK] {path} → {kind}  {after.ToPretty()}");
                }
                catch (Exception ex)
                {
                    stats.skipped++;
                    log.Add($"[SKIP] {path}  error: {ex.Message}");
                }
            }
            else
            {
                stats.skipped++;
            }
        }

        log.Add($"— Done. Total: {stats.total}, Changed: {stats.changed}, Skipped: {stats.skipped} —");
    }

    string[] FindAudioClipGuids()
    {
        if (!onlyInSelection)
            return AssetDatabase.FindAssets("t:AudioClip", includePackages ? null : new[] { "Assets" });

        var roots = Selection.assetGUIDs;
        if (roots == null || roots.Length == 0)
            return AssetDatabase.FindAssets("t:AudioClip", includePackages ? null : new[] { "Assets" });

        var paths = roots.Select(AssetDatabase.GUIDToAssetPath)
                         .Where(p => AssetDatabase.IsValidFolder(p))
                         .ToArray();

        if (paths.Length == 0)
            paths = new[] { "Assets" };

        return AssetDatabase.FindAssets("t:AudioClip", paths);
    }

    Kind Classify(string path, AudioClip clip)
    {
        string n = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        float len = clip.length;

        // Name hints (strong)
        if (nameHintsMusic.Any(h => n.Contains(h))) return Kind.Music;
        if (nameHintsUI.Any(h => n.Contains(h))) return len <= uiMaxLenSec ? Kind.UI : Kind.SFX;
        if (nameHintsVO.Any(h => n.Contains(h))) return len <= voMaxLenSec ? Kind.VO : Kind.Music;

        // Length buckets (fallback)
        if (len <= uiMaxLenSec) return Kind.UI;
        if (len <= sfxMaxLenSec) return Kind.SFX;
        if (len <= voMaxLenSec) return Kind.VO;
        return Kind.Music;
    }

    AudioImporterSampleSettings SuggestSettings(Kind kind, AudioClip clip, AudioImporterSampleSettings current)
    {
        var s = current; // start from current to minimize churn

        // Defaults that we’ll override per kind
        s.quality = 0.5f;
        s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.loadType = AudioClipLoadType.DecompressOnLoad;

        switch (kind)
        {
            case Kind.UI:
                // Very short: ADPCM is tiny decode cost and fine at 16–22kHz.
                s.compressionFormat = AudioCompressionFormat.ADPCM;
                s.quality = 1.0f; // ignored by ADPCM
                s.sampleRateOverride = (uint)(clip.frequency >= 22050 ? 22050 : 16000);
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                break;

            case Kind.SFX:
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.35f;
                s.sampleRateOverride = 22050;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                break;

            case Kind.VO:
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.4f;
                // Keep a bit more high end for intelligibility
                s.sampleRateOverride = (uint)Mathf.Clamp(clip.frequency, 24000, 32000);
                s.loadType = AudioClipLoadType.CompressedInMemory;
                break;

            case Kind.Music:
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = Mathf.Clamp01(0.55f);
                s.sampleRateOverride = 44100;
                s.loadType = AudioClipLoadType.Streaming;
                break;
        }

        return s;
    }

    static bool ForceToMono(Kind kind)
        => kind == Kind.UI || kind == Kind.SFX || kind == Kind.VO;

    static bool LoadInBackground(Kind kind)
        => kind == Kind.Music || kind == Kind.VO;

    static bool Preload(Kind kind)
        => kind == Kind.UI || kind == Kind.SFX; // instant response for one-shots

    static bool SampleSettingsEqual(AudioImporterSampleSettings a, AudioImporterSampleSettings b)
    {
        return a.compressionFormat == b.compressionFormat
            && Mathf.Approximately(a.quality, b.quality)
            && a.sampleRateSetting == b.sampleRateSetting
            && a.sampleRateOverride == b.sampleRateOverride
            && a.loadType == b.loadType;
    }
}

// ---- pretty-print helpers ----
static class AudioImporterExtensions
{
    public static string ToPretty(this AudioImporterSampleSettings s)
    {
        return $"[{s.compressionFormat} q:{s.quality:0.00} {SR(s)} {s.loadType}]";
    }

    static string SR(AudioImporterSampleSettings s)
    {
        return s.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate
            ? $"{s.sampleRateOverride / 1000f:0.#}kHz"
            : "SR:Preserve";
    }
}
#endif
