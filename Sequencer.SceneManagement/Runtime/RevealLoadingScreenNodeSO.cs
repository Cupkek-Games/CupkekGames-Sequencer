using System.Collections;
using CupkekGames.Luna.Navigation;
using CupkekGames.SceneManagement;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    public enum DeferredLoadingTransitionTarget
    {
        /// <summary>Addressable scene loader (fade completes after this manual step).</summary>
        SceneLoaderAddressable = 0,

        /// <summary>Build-index scene loader used by bootstrap scenes.</summary>
        SceneLoaderBuildIndex = 1
    }

    /// <summary>
    /// Reveals the scene: waits until the navigation UI is ready, then completes the deferred
    /// loading transition so the loader fades out. Folds the former <c>WaitForNavReadyNodeSO</c>
    /// (the wait) and <c>CompleteDeferredLoadingTransitionNodeSO</c> (the complete + cold-start
    /// fallback) into a single node. Place it AFTER <see cref="BootNavGraphsNodeSO"/>.
    ///
    /// <para>
    /// <b>Policy-free re: input.</b> Whether the reveal waits for a "press to continue" is decided
    /// by WHICH transition the scene-load was started with — a <c>…WithInput</c> transition raises
    /// <c>SceneTransition.LoadingScreenContinueRequested</c> on its FadeOut, and the
    /// <c>LoadingScreen</c> owns the prompt + input. This node just completes the deferred
    /// transition; it never knows about input.
    /// </para>
    ///
    /// <para>
    /// Only hosts with <c>NavHost.GatesAllReady = true</c> count toward the wait. The
    /// <see cref="_timeoutSeconds"/> safety cap guarantees the loader can't deadlock if a host hangs.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Reveal Loading Screen")]
    public class RevealLoadingScreenNodeSO : SequencerNodeSO
    {
        [Header("Wait for nav readiness")]
        [Tooltip("Wait until LunaLayers.AllReady (every gating NavHost finished loading its panels). " +
                 "Off = complete the transition immediately.")]
        [SerializeField] private bool _waitForNavReady = true;

        [Tooltip("If no NavHosts are registered yet, treat as ready once at least one frame has " +
                 "elapsed (so same-frame host Awakes can register first) instead of waiting the timeout.")]
        [SerializeField] private bool _treatNoHostsAsReady = true;

        [Tooltip("Always wait at least this long (unscaled seconds) before revealing, so the loader " +
                 "can't blink out instantly even when the UI is already ready. 0 = no floor.")]
        [SerializeField] [Min(0f)] private float _minVisibleSeconds = 0f;

        [Tooltip("Safety cap (unscaled seconds): reveal even without nav-ready after this long " +
                 "(logs a warning). 0 = wait forever (not recommended).")]
        [SerializeField] [Min(0f)] private float _timeoutSeconds = 8f;

        [Tooltip("After ready, hold this much longer (unscaled seconds) so entry transitions can " +
                 "settle before revealing. 0 = no hold.")]
        [SerializeField] [Min(0f)] private float _postReadyHoldSeconds = 0f;

        [Header("Complete the deferred transition")]
        [Tooltip("GameFull / SceneLoader (build index) = SceneLoaderBuildIndex. Addressables = SceneLoaderAddressable.")]
        [SerializeField] private DeferredLoadingTransitionTarget _target = DeferredLoadingTransitionTarget.SceneLoaderBuildIndex;

        [Header("Cold start (play a scene directly)")]
        [Tooltip("If no SceneLoader deferred load is pending (e.g. you hit Play on the scene without " +
                 "going through init), still FadeOut by Fallback Transition Key so a visible loader hides.")]
        [SerializeField] private bool _fadeOutWhenNoPendingSceneLoad = true;

        [Tooltip("Same key as SceneLoaderStartup / SceneTransitionDatabase (e.g. Fade).")]
        [SerializeField] private string _fallbackTransitionKey = "Fade";

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (_waitForNavReady)
            {
                float start = Time.unscaledTime;
                while (true)
                {
                    float elapsed = Time.unscaledTime - start;
                    bool ready = IsReady(elapsed);
                    bool minMet = elapsed >= _minVisibleSeconds;
                    bool timedOut = _timeoutSeconds > 0f && elapsed >= _timeoutSeconds;

                    if (minMet && (ready || timedOut))
                    {
                        if (!ready && timedOut)
                            Debug.LogWarning(
                                $"[SequencerExec] RevealLoadingScreen '{name}': revealing after {elapsed:0.##}s " +
                                $"without nav-ready (timeout {_timeoutSeconds:0.##}s, hosts={LunaLayers.All.Count}, " +
                                $"AllReady={LunaLayers.AllReady}).");
                        break;
                    }

                    yield return null;
                }

                if (_postReadyHoldSeconds > 0f)
                    yield return new WaitForSecondsRealtime(_postReadyHoldSeconds);
            }

            CompleteDeferredTransition();
        }

        // elapsed gates the "no hosts yet" short-circuit behind one frame, so a same-frame race
        // (this node running before the scene's NavHosts have Awoken + registered) can't pass
        // instantly and defeat the gate it was placed to enforce.
        private bool IsReady(float elapsed)
        {
            if (LunaLayers.All.Count == 0) return _treatNoHostsAsReady && elapsed > 0f;
            return LunaLayers.AllReady;
        }

        private void CompleteDeferredTransition()
        {
            bool completed = false;

            switch (_target)
            {
                case DeferredLoadingTransitionTarget.SceneLoaderAddressable:
#if UNITY_ADDRESSABLES
                    if (SceneLoaderAddressable.Instance != null)
                        completed = SceneLoaderAddressable.Instance.TryCompleteDeferredLoadingTransition();
                    else
                        Debug.LogWarning("RevealLoadingScreenNodeSO: SceneLoaderAddressable.Instance is null.");
#else
                    Debug.LogWarning(
                        "RevealLoadingScreenNodeSO: UNITY_ADDRESSABLES is not defined; use SceneLoaderBuildIndex or enable Addressables.");
#endif
                    break;

                case DeferredLoadingTransitionTarget.SceneLoaderBuildIndex:
                    if (SceneLoader.Instance != null)
                        completed = SceneLoader.Instance.TryCompleteDeferredLoadingTransition();
                    else
                        Debug.LogWarning(
                            "RevealLoadingScreenNodeSO: SceneLoader.Instance is null (FadeOut by key may still run).");
                    break;
            }

            // Cold start: no deferred load pending → FadeOut by key so a StartVisible loader hides.
            if (!completed && _fadeOutWhenNoPendingSceneLoad)
                SceneLoader.FadeOutTransitionByKey(_fallbackTransitionKey);
        }
    }
}
