using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Warms shader variants while the loading screen is still up, so the first time a
    /// material hits the screen the driver doesn't stall to compile it. Feed it
    /// <see cref="ShaderVariantCollection"/> assets recorded from a representative
    /// play-through: Project Settings › Graphics › "Save to asset" (cold Library/ShaderCache)
    /// or a development build's Player.log via Tools › CupkekGames › Sequencer › Shader Variant
    /// Log Importer. Place it BEFORE the node that reveals the scene (e.g. RevealLoadingScreenNodeSO).
    ///
    /// <para>
    /// Warm-up is synchronous per call; large collections are spread across frames with
    /// <see cref="_variantsPerFrame"/> so the loading screen keeps animating. Collections
    /// already warmed this session are skipped.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Warm Up Shaders")]
    public class WarmUpShadersNodeSO : SequencerNodeSO
    {
        [Tooltip("Variant collections to warm, in order. Author them from a play-through: Project Settings › " +
                 "Graphics › Save to asset (cold shader cache) or Tools › CupkekGames › Sequencer › Shader Variant Log Importer.")]
        [SerializeField] private List<ShaderVariantCollection> _collections = new();

        [Tooltip("Variants warmed per frame (0 = each whole collection in one call). Spreading the " +
                 "work keeps the loading screen responsive; 64–256 is a sensible range.")]
        [SerializeField] [Min(0)] private int _variantsPerFrame = 128;

        [Tooltip("Skip collections whose isWarmedUp is already true (re-runs of the sequence).")]
        [SerializeField] private bool _skipWarmedUpCollections = true;

        [Tooltip("Coarse fallback: Shader.WarmupAllShaders() after the collections. Warms every " +
                 "loaded shader's variants — slow on large projects; prefer authored collections.")]
        [SerializeField] private bool _warmUpAllLoadedShaders;

        [Tooltip("Log one summary line (collections, variants, milliseconds) when done.")]
        [SerializeField] private bool _logSummary = true;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            float startedAt = Time.realtimeSinceStartup;
            int warmedCollections = 0;
            int warmedVariants = 0;

            foreach (ShaderVariantCollection collection in _collections)
            {
                if (collection == null)
                {
                    Debug.LogWarning("WarmUpShadersNodeSO: null ShaderVariantCollection entry skipped.", this);
                    continue;
                }

                if (_skipWarmedUpCollections && collection.isWarmedUp)
                {
                    continue;
                }

                if (_variantsPerFrame > 0)
                {
                    while (!collection.WarmUpProgressively(_variantsPerFrame))
                    {
                        yield return null;
                    }
                }
                else
                {
                    collection.WarmUp();
                }

                warmedCollections++;
                warmedVariants += collection.variantCount;
            }

            if (_warmUpAllLoadedShaders)
            {
                Shader.WarmupAllShaders();
            }

            if (_logSummary)
            {
                float ms = (Time.realtimeSinceStartup - startedAt) * 1000f;
                Debug.Log($"WarmUpShadersNodeSO: warmed {warmedCollections} collection(s), {warmedVariants} variant(s)" +
                          (_warmUpAllLoadedShaders ? " + all loaded shaders" : string.Empty) +
                          $" in {ms:F0} ms.", this);
            }
        }
    }
}
