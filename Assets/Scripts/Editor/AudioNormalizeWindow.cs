#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class AudioNormalizeWindow : EditorWindow
{
    [MenuItem("Tools/Optimization/Audio Normalizer")]
    private static void Open()
    {
        var w = GetWindow<AudioNormalizeWindow>("Audio Normalizer");
        w.minSize = new Vector2(560, 420);
    }

    // ---- UI params ----
    bool onlyInSelection = true;
    bool dryRun = true;
    float targetRmsDb = -16f;     // common SFX/UI target
    float maxGainDb = 6f;       // clamp boost/cut to avoid artifacts
    float peakHeadroomDb = 1.0f;  // leave -1 dBFS true peak headroom
    int wavBitDepth = 16;       // 16-bit PCM (32-bit float also available)
    bool normalizeMono = true;   // force mono if source is mono; stereo kept stereo

    Vector2 scroll;
    readonly List<string> log = new();

    void OnGUI()
    {
        GUILayout.Label("Scan", EditorStyles.boldLabel);
        onlyInSelection = EditorGUILayout.ToggleLeft("Only in Project selection (folders/files)", onlyInSelection);

        GUILayout.Space(6);
        GUILayout.Label("Targeting", EditorStyles.boldLabel);
        targetRmsDb = EditorGUILayout.Slider(new GUIContent("Target RMS (dBFS)", "Perceived loudness anchor; -16 to -18 dBFS is common for SFX/UI"), targetRmsDb, -24f, -10f);
        maxGainDb = EditorGUILayout.Slider(new GUIContent("Max correction (±dB)"), maxGainDb, 1f, 12f);
        peakHeadroomDb = EditorGUILayout.Slider(new GUIContent("Peak headroom (dB)", "Distance from 0 dBFS after normalization"), peakHeadroomDb, 0.1f, 3f);

        GUILayout.Space(6);
        GUILayout.Label("Output", EditorStyles.boldLabel);
        wavBitDepth = EditorGUILayout.IntPopup("WAV Bit Depth", wavBitDepth,
            new[] { "16-bit PCM", "32-bit Float" }, new[] { 16, 32 });
        normalizeMono = EditorGUILayout.ToggleLeft("Respect mono (mono in → mono out)", normalizeMono);

        GUILayout.Space(6);
        dryRun = EditorGUILayout.ToggleLeft("Dry Run (analyze only, don’t write files)", dryRun);

        GUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(dryRun ? "Preview" : "Normalize", GUILayout.Height(32)))
                Run();

            if (GUILayout.Button("Clear Log"))
                log.Clear();
        }

        GUILayout.Space(6);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var line in log) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.HelpBox(
            "What this does:\n" +
            "• Reads clip PCM (auto-decompresses), measures RMS & peak.\n" +
            "• Computes gain to reach target RMS, clamped by ±Max correction and Peak headroom.\n" +
            "• Writes a new *normalized* WAV (suffix _norm) next to the original (non-destructive).\n" +
            "• You swap references when ready (safe with git).",
            MessageType.Info);
    }

    void Run()
    {
        log.Clear();
        var guids = FindAudioClipGuids();
        if (guids.Length == 0) { log.Add("No AudioClips found."); return; }

        int changed = 0, skipped = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".aif", StringComparison.OrdinalIgnoreCase) &&
                !path.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase))
            { skipped++; continue; }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) { skipped++; continue; }

            if (!clip.LoadAudioData()) { log.Add($"[SKIP] {path} (failed to LoadAudioData)"); skipped++; continue; }

            // Pull data
            int channels = clip.channels;
            int samples = clip.samples;
            var data = new float[samples * channels];
            if (!clip.GetData(data, 0)) { log.Add($"[SKIP] {path} (GetData failed)"); skipped++; continue; }

            // Analyze per-channel RMS and peak, then combine RMS across channels
            var peaks = new float[channels];
            var rmsSq = new double[channels];

            for (int i = 0; i < samples; i++)
            {
                for (int c = 0; c < channels; c++)
                {
                    float v = data[i * channels + c];
                    float av = Mathf.Abs(v);
                    if (av > peaks[c]) peaks[c] = av;
                    rmsSq[c] += (double)v * (double)v;
                }
            }

            // Integrated RMS across channels (energy-mean)
            double sumEnergy = 0;
            for (int c = 0; c < channels; c++) sumEnergy += rmsSq[c] / samples;
            double meanEnergy = sumEnergy / Math.Max(1, channels);
            float rms = (float)Math.Sqrt(Math.Max(1e-12, meanEnergy));
            float rmsDb = LinearToDb(rms);

            // Overall peak across channels
            float peak = 0f; for (int c = 0; c < channels; c++) peak = Mathf.Max(peak, peaks[c]);
            float peakDb = LinearToDb(peak);

            // Compute desired gain to hit target RMS, clamp, then ensure peak headroom
            float gainDb = Mathf.Clamp(targetRmsDb - rmsDb, -maxGainDb, maxGainDb);
            float postPeakDb = peakDb + gainDb;
            float maxAllowedPeakDb = -peakHeadroomDb; // e.g., -1 dBFS
            if (postPeakDb > maxAllowedPeakDb)
            {
                float reduce = postPeakDb - maxAllowedPeakDb; // how much to lower
                gainDb -= reduce;
            }
            float gainLin = DbToLinear(gainDb);

            string info = $"{Path.GetFileName(path)}  len:{clip.length:0.00}s  ch:{channels}  RMS:{rmsDb:0.0}dB  Peak:{peakDb:0.0}dB  → gain:{gainDb:+0.0;-0.0}dB";
            if (dryRun)
            {
                log.Add("[PREVIEW] " + info);
                continue;
            }

            // Apply gain and write normalized WAV copy
            var outData = new float[data.Length];
            for (int i = 0; i < outData.Length; i++)
                outData[i] = Mathf.Clamp(data[i] * gainLin, -1f, 1f);

            // Optional mono collapse if source is mono and setting says so
            int outChannels = (normalizeMono && channels == 1) ? 1 : channels;

            var outPath = GetNormalizedPath(path);
            try
            {
                WriteWav(outPath, outData, clip.frequency, outChannels, wavBitDepth);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceSynchronousImport);
                changed++;
                log.Add($"[OK]  {info}  →  {outPath}");
            }
            catch (Exception ex)
            {
                log.Add($"[ERR] {path}  {ex.Message}");
                skipped++;
            }
        }

        log.Add($"— Done. Total: {guids.Length}, Normalized: {changed}, Skipped: {skipped} —");
    }

    string[] FindAudioClipGuids()
    {
        if (!onlyInSelection)
            return AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });

        var roots = Selection.assetGUIDs;
        if (roots == null || roots.Length == 0)
            return AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });

        var paths = roots.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var folders = paths.Where(AssetDatabase.IsValidFolder).ToArray();
        if (folders.Length == 0) folders = new[] { "Assets" };
        return AssetDatabase.FindAssets("t:AudioClip", folders);
    }

    static string GetNormalizedPath(string srcPath)
    {
        var dir = Path.GetDirectoryName(srcPath).Replace("\\", "/");
        var name = Path.GetFileNameWithoutExtension(srcPath);
        return $"{dir}/{name}_norm.wav";
    }

    // ---- Math helpers ----
    static float LinearToDb(float lin) => 20f * Mathf.Log10(Mathf.Clamp(lin, 1e-12f, 1f));
    static float DbToLinear(float db) => Mathf.Pow(10f, db / 20f);

    // ---- WAV writer (16-bit PCM or 32-bit float) ----
    static void WriteWav(string path, float[] samples, int sampleRate, int channels, int bitDepth)
    {
        // If original was stereo but normalizeMono==true only for mono sources, we keep stereo here.
        // We assume samples[] is interleaved with 'channels' as measured; if channels mismatch, re-interleave.
        int srcChannels = channels; // using measured channels for interleave
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            int bytesPerSample = (bitDepth == 32) ? 4 : 2;
            int dataChunkSize = samples.Length * bytesPerSample;
            int fmtChunkSize = 16;
            int audioFormat = (bitDepth == 32) ? 3 : 1; // 3 = IEEE float, 1 = PCM
            int byteRate = sampleRate * channels * bytesPerSample;
            short blockAlign = (short)(channels * bytesPerSample);

            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataChunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(fmtChunkSize);
            bw.Write((short)audioFormat);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write((short)bitDepth);

            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataChunkSize);

            if (bitDepth == 32)
            {
                // 32-bit float
                for (int i = 0; i < samples.Length; i++)
                    bw.Write(samples[i]);
            }
            else
            {
                // 16-bit PCM with proper dithering could be added; plain clamp/scale is fine here
                for (int i = 0; i < samples.Length; i++)
                {
                    short v = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), -32768, 32767);
                    bw.Write(v);
                }
            }
        }
    }
}
#endif
