using System;
using UnityEditor;
using UnityEngine;

namespace CupkekGames.Sequencer.Editor
{
    /// <summary>Editor window front-end for <see cref="ShaderVariantLogImporter"/>.</summary>
    public class ShaderVariantLogImporterWindow : EditorWindow
    {
        private const string PrefLogPath = "CupkekGames.Sequencer.ShaderVariantLogImporter.LogPath";
        private const string PrefSkipAllHidden = "CupkekGames.Sequencer.ShaderVariantLogImporter.SkipAllHidden";

        private ShaderVariantCollection _target;
        private string _logPath;
        private bool _skipAllHidden;
        private string _lastResult = string.Empty;

        [MenuItem("Tools/CupkekGames/Sequencer/Shader Variant Log Importer")]
        public static void Open()
        {
            GetWindow<ShaderVariantLogImporterWindow>("Shader Variants");
        }

        private void OnEnable()
        {
            _logPath = EditorPrefs.GetString(PrefLogPath, string.Empty);
            _skipAllHidden = EditorPrefs.GetBool(PrefSkipAllHidden, false);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Player-accurate warm-up list, public API only:\n" +
                "1) Project Settings › Graphics › Shader Loading › Log Shader Compilation = on.\n" +
                "2) Make a Development Build and play through every scene, day and night, menus.\n" +
                "3) Pick that build's Player.log below and Import: every 'Compiled shader:' line becomes a variant.\n" +
                "4) Assign the collection to a WarmUpShadersNodeSO.\n" +
                "Quick in-Editor alternative: delete Library/ShaderCache once (editor closed), play through, " +
                "Project Settings › Graphics › Save to asset, then Prune here.",
                MessageType.Info);

            _target = (ShaderVariantCollection)EditorGUILayout.ObjectField("Target collection", _target, typeof(ShaderVariantCollection), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                string newPath = EditorGUILayout.TextField("Player.log", _logPath);
                if (GUILayout.Button("…", GUILayout.Width(28f)))
                {
                    string start = string.IsNullOrEmpty(_logPath) ? DefaultLogFolder() : System.IO.Path.GetDirectoryName(_logPath);
                    string picked = EditorUtility.OpenFilePanel("Select Player.log", start, "log");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        newPath = picked;
                    }
                }

                if (newPath != _logPath)
                {
                    _logPath = newPath;
                    EditorPrefs.SetString(PrefLogPath, _logPath);
                }
            }

            bool skip = EditorGUILayout.ToggleLeft(
                new GUIContent("Skip every Hidden/ shader",
                    "Off: only editor/debug shaders (Hidden/Internal-*, Hidden/Editor*, SceneView, Handles, ...) are skipped; " +
                    "runtime Hidden/ shaders such as URP post-processing stay in. On: skip all Hidden/ shaders."),
                _skipAllHidden);
            if (skip != _skipAllHidden)
            {
                _skipAllHidden = skip;
                EditorPrefs.SetBool(PrefSkipAllHidden, _skipAllHidden);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(_target == null || string.IsNullOrEmpty(_logPath)))
            {
                if (GUILayout.Button("Import Player.log into target", GUILayout.Height(28f)))
                {
                    try
                    {
                        ShaderVariantLogImporter.ImportResult r = ShaderVariantLogImporter.Import(_target, _logPath, _skipAllHidden);
                        _lastResult = $"{r.Lines} log lines → +{r.Added} variants ({r.AlreadyPresent} already present, " +
                                      $"{r.Skipped} skipped, {r.Unresolved} unresolved" +
                                      (string.IsNullOrEmpty(r.LastUnresolved) ? ")" : $", last: {r.LastUnresolved})");
                        Debug.Log($"[ShaderVariantLogImporter] {_lastResult} → '{AssetDatabase.GetAssetPath(_target)}' now {_target.shaderCount} shaders / {_target.variantCount} variants.");
                    }
                    catch (Exception ex)
                    {
                        _lastResult = ex.Message;
                        Debug.LogError($"[ShaderVariantLogImporter] {ex.Message}");
                    }
                }
            }

            if (_target != null)
            {
                EditorGUILayout.LabelField("Target contents", $"{_target.shaderCount} shaders / {_target.variantCount} variants");
                if (GUILayout.Button("Prune editor/denied shaders from target"))
                {
                    int removed = ShaderVariantLogImporter.Prune(_target, _skipAllHidden);
                    _lastResult = $"Pruned {removed} shader entr{(removed == 1 ? "y" : "ies")}.";
                    Debug.Log($"[ShaderVariantLogImporter] {_lastResult} '{AssetDatabase.GetAssetPath(_target)}' now {_target.shaderCount} shaders / {_target.variantCount} variants.");
                }
            }

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_lastResult, MessageType.None);
            }
        }

        private static string DefaultLogFolder()
        {
            // Windows player logs live under %USERPROFILE%\AppData\LocalLow\<Company>\<Product>\.
            string localLow = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
            string product = System.IO.Path.Combine(localLow, Application.companyName, Application.productName);
            return System.IO.Directory.Exists(product) ? product : localLow;
        }
    }
}
