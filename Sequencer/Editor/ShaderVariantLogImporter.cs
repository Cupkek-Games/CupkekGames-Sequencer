using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CupkekGames.Sequencer.Editor
{
    /// <summary>
    /// Imports the shader variants a <b>Player</b> compiled into a
    /// <see cref="ShaderVariantCollection"/> asset, using only public API. Workflow:
    /// enable Project Settings › Graphics › Shader Loading › <i>Log Shader Compilation</i>
    /// (<see cref="GraphicsSettings.logWhenShaderIsCompiled"/>), make a Development Build,
    /// play through the game, then feed its <c>Player.log</c> to <see cref="Import"/>. Each
    /// <c>Compiled shader: X, pass: Y, stage: Z, keywords A B</c> line becomes one
    /// <see cref="ShaderVariantCollection.Add"/>. Feed the result to <see cref="WarmUpShadersNodeSO"/>.
    ///
    /// <para>
    /// Unity emits those log lines only in Players (the Editor compiles through its own
    /// shader cache and logs nothing), so this is the public-API path to a player-accurate,
    /// post-stripping variant list. For a quick in-Editor collection use Project Settings ›
    /// Graphics › Shader Loading › <i>Save to asset</i> after deleting <c>Library/ShaderCache</c>
    /// once (editor closed), then <see cref="Prune"/> the editor-only shaders.
    /// </para>
    /// </summary>
    public static class ShaderVariantLogImporter
    {
        public readonly struct ImportResult
        {
            public readonly int Lines;
            public readonly int Added;
            public readonly int AlreadyPresent;
            public readonly int Skipped;
            public readonly int Unresolved;
            public readonly string LastUnresolved;

            public ImportResult(int lines, int added, int alreadyPresent, int skipped, int unresolved, string lastUnresolved)
            {
                Lines = lines;
                Added = added;
                AlreadyPresent = alreadyPresent;
                Skipped = skipped;
                Unresolved = unresolved;
                LastUnresolved = lastUnresolved;
            }
        }

        private const string LinePrefix = "Compiled shader: ";

        private static readonly Regex LinePattern = new Regex(
            @"^Compiled shader: (?<shader>.+?), pass: (?<pass>.+?), stage: (?<stage>\w+), keywords (?<keywords>.*)$",
            RegexOptions.Compiled);

        /// <summary>
        /// Runtime shaders that would otherwise match a denied prefix; always kept
        /// (UI Toolkit's runtime renderer lives under <c>Hidden/Internal-UIR*</c>).
        /// </summary>
        public static readonly string[] AllowedShaderPrefixes =
        {
            "Hidden/Internal-UIR",
        };

        /// <summary>Editor/debug shaders that never render in a player; always skipped / pruned.</summary>
        public static readonly string[] DeniedShaderPrefixes =
        {
            "Hidden/Internal-",
            "Hidden/Editor",
            "Hidden/SceneView",
            "Hidden/Handles",
            "Hidden/GIDebug",
            "Hidden/UIToolkit/Editor",
            "Hidden/Universal Render Pipeline/Debug",
            "Hidden/Universal Render Pipeline/Editor",
            "Hidden/HDRP",
            "Hidden/PreviewShader",
        };

        // Order matters: URP passes resolve as ScriptableRenderPipeline; UI/legacy as Normal.
        private static readonly PassType[] PassTypeCandidates =
        {
            PassType.ScriptableRenderPipeline,
            PassType.Normal,
            PassType.ShadowCaster,
            PassType.Meta,
            PassType.ScriptableRenderPipelineDefaultUnlit,
            PassType.ForwardBase,
            PassType.ForwardAdd,
            PassType.Deferred,
            PassType.Vertex,
            PassType.VertexLM,
            PassType.MotionVectors,
        };

        /// <summary>
        /// Parses <paramref name="logPath"/> and adds every compiled variant to
        /// <paramref name="target"/>; saves the asset when anything changed.
        /// </summary>
        public static ImportResult Import(ShaderVariantCollection target, string logPath, bool skipAllHidden)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (string.IsNullOrEmpty(logPath) || !File.Exists(logPath))
            {
                throw new FileNotFoundException("Player log not found.", logPath);
            }

            int lines = 0, added = 0, present = 0, skipped = 0, unresolved = 0;
            string lastUnresolved = string.Empty;
            var seen = new HashSet<string>();

            foreach (string raw in File.ReadLines(logPath))
            {
                string line = raw.TrimEnd('\r');
                if (!line.StartsWith(LinePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                lines++;
                Match match = LinePattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string shaderName = match.Groups["shader"].Value;
                string[] keywords = ParseKeywords(match.Groups["keywords"].Value);
                if (!seen.Add(shaderName + "|" + string.Join(" ", keywords)))
                {
                    continue;
                }

                if (IsDenied(shaderName, skipAllHidden))
                {
                    skipped++;
                    continue;
                }

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    unresolved++;
                    lastUnresolved = shaderName;
                    continue;
                }

                switch (TryAdd(target, shader, keywords))
                {
                    case AddOutcome.Added: added++; break;
                    case AddOutcome.Present: present++; break;
                    default:
                        unresolved++;
                        lastUnresolved = shaderName + " [" + string.Join(" ", keywords) + "]";
                        break;
                }
            }

            if (added > 0)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssetIfDirty(target);
            }

            return new ImportResult(lines, added, present, skipped, unresolved, lastUnresolved);
        }

        /// <summary>Removes denied (and optionally all Hidden/) shaders from a collection.</summary>
        public static int Prune(ShaderVariantCollection target, bool removeAllHidden)
        {
            if (target == null)
            {
                return 0;
            }

            var so = new SerializedObject(target);
            SerializedProperty shaders = so.FindProperty("m_Shaders");
            int removed = 0;
            for (int i = shaders.arraySize - 1; i >= 0; i--)
            {
                var shader = shaders.GetArrayElementAtIndex(i).FindPropertyRelative("first").objectReferenceValue as Shader;
                if (shader == null || IsDenied(shader.name, removeAllHidden))
                {
                    shaders.DeleteArrayElementAtIndex(i);
                    removed++;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssetIfDirty(target);
            return removed;
        }

        public static bool IsDenied(string shaderName, bool skipAllHidden)
        {
            foreach (string prefix in AllowedShaderPrefixes)
            {
                if (shaderName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            if (skipAllHidden && shaderName.StartsWith("Hidden/", StringComparison.Ordinal))
            {
                return true;
            }

            foreach (string prefix in DeniedShaderPrefixes)
            {
                if (shaderName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] ParseKeywords(string keywordText)
        {
            keywordText = keywordText.Trim();
            return keywordText.Length == 0 || keywordText.StartsWith("<no keywords>", StringComparison.Ordinal)
                ? Array.Empty<string>()
                : keywordText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private enum AddOutcome { Added, Present, Unresolved }

        private static AddOutcome TryAdd(ShaderVariantCollection target, Shader shader, string[] keywords)
        {
            foreach (PassType passType in PassTypeCandidates)
            {
                ShaderVariantCollection.ShaderVariant variant;
                try
                {
                    variant = new ShaderVariantCollection.ShaderVariant(shader, passType, keywords);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                if (target.Contains(variant))
                {
                    return AddOutcome.Present;
                }

                target.Add(variant);
                return AddOutcome.Added;
            }

            return AddOutcome.Unresolved;
        }
    }
}
