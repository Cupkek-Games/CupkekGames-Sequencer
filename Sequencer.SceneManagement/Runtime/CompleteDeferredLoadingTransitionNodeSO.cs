using System.Collections;
using CupkekGames.SceneManagement;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    public enum DeferredLoadingTransitionTarget
    {
        /// <summary>Addressable scene loader (fade completes after manual step).</summary>
        SceneLoaderAddressable = 0,

        /// <summary>Build-index scene loader used by bootstrap scenes.</summary>
        SceneLoaderBuildIndex = 1
    }

    /// <summary>
    /// Completes a deferred loading transition after a load was requested with
    /// <c>deferFadeOutUntilManualComplete</c>. Pair with bootstrap/sequencer work before hiding the loading UI.
    /// When you play directly in a scene (no init / no SceneLoader load), there is no pending defer — enable
    /// <see cref="_fadeOutWhenNoPendingSceneLoad"/> to FadeOut by transition key instead.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Complete Deferred Loading Transition")]
    public class CompleteDeferredLoadingTransitionNodeSO : SequencerNodeSO
    {
        [Tooltip("GameFull / SceneLoader (build index) = SceneLoaderBuildIndex. Addressables pipeline = SceneLoaderAddressable.")]
        [SerializeField]
        private DeferredLoadingTransitionTarget _target = DeferredLoadingTransitionTarget.SceneLoaderBuildIndex;

        [Tooltip("Seconds to wait before completing (unscaled).")]
        [SerializeField]
        [Min(0f)]
        private float _delaySeconds;

        [Header("Cold start (play scene directly)")]
        [Tooltip(
            "If true, when no SceneLoader deferred load is pending (e.g. you hit Play on MainMenu without init), " +
            "still call FadeOut on SceneTransitionDatabase using Fallback Transition Key.")]
        [SerializeField]
        private bool _fadeOutWhenNoPendingSceneLoad = true;

        [Tooltip("Same key as SceneLoaderStartup / SceneTransitionDatabase (e.g. Fade).")]
        [SerializeField]
        private string _fallbackTransitionKey = "Fade";

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (_delaySeconds > 0f)
                yield return new WaitForSecondsRealtime(_delaySeconds);

            bool completed = false;

            switch (_target)
            {
                case DeferredLoadingTransitionTarget.SceneLoaderAddressable:
#if UNITY_ADDRESSABLES
                    if (SceneLoaderAddressable.Instance != null)
                        completed = SceneLoaderAddressable.Instance.TryCompleteDeferredLoadingTransition();
                    else
                        Debug.LogWarning(
                            "CompleteDeferredLoadingTransitionNodeSO: SceneLoaderAddressable.Instance is null.");
#else
                    Debug.LogWarning(
                        "CompleteDeferredLoadingTransitionNodeSO: UNITY_ADDRESSABLES is not defined; use SceneLoaderBuildIndex or enable Addressables.");
#endif
                    break;

                case DeferredLoadingTransitionTarget.SceneLoaderBuildIndex:
                    if (SceneLoader.Instance != null)
                        completed = SceneLoader.Instance.TryCompleteDeferredLoadingTransition();
                    else
                        Debug.LogWarning(
                            "CompleteDeferredLoadingTransitionNodeSO: SceneLoader.Instance is null (FadeOut by key may still run).");
                    break;
            }

            if (!completed && _fadeOutWhenNoPendingSceneLoad)
                SceneLoader.FadeOutTransitionByKey(_fallbackTransitionKey);

            yield break;
        }
    }
}
