using CupkekGames.Luna.Navigation;
using CupkekGames.SceneManagement;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Scene-component counterpart of <see cref="RevealLoadingScreenNodeSO"/> for scenes
    /// WITHOUT a sequencer runner (e.g. sub-scenes entered mid-flow, like building
    /// interiors): optionally boots a Manual-boot NavHost from Start() — after the
    /// scene's ServiceProviders have registered in Awake() — then completes a deferred
    /// loading transition once that host reports ready, so a load issued with
    /// <c>deferFadeOutUntilManualComplete: true</c> reveals on actual readiness.
    /// TryComplete no-ops when nothing is pending (cold starts).
    /// </summary>
    [DisallowMultipleComponent]
    public class RevealLoadingScreenOnNavReady : MonoBehaviour
    {
        [Tooltip("Host to boot/watch. Empty = the NavHost on this GameObject.")]
        [SerializeField] private NavHost _host;

        [Tooltip("Call Boot() on the host from Start(). Pair with Boot = Manual hosts whose views " +
                 "read scene-local services registered during Awake() — an OnAwake boot races them.")]
        [SerializeField] private bool _bootHostOnStart = true;

        [Tooltip("Which loader's deferred transition to complete on ready.")]
        [SerializeField] private DeferredLoadingTransitionTarget _target =
            DeferredLoadingTransitionTarget.SceneLoaderAddressable;

        private void Start()
        {
            if (_host == null) _host = GetComponent<NavHost>();
            if (_host == null)
            {
                Debug.LogError(
                    $"[RevealLoadingScreenOnNavReady] '{name}': no NavHost assigned or on this GameObject — " +
                    "a deferred loading transition into this scene will never complete.", this);
                return;
            }

            if (_bootHostOnStart) _host.Boot();

            _host.WhenReady(CompleteDeferredTransition);
        }

        private void CompleteDeferredTransition()
        {
            switch (_target)
            {
                case DeferredLoadingTransitionTarget.SceneLoaderAddressable:
#if UNITY_ADDRESSABLES
                    if (SceneLoaderAddressable.Instance != null)
                        SceneLoaderAddressable.Instance.TryCompleteDeferredLoadingTransition();
#else
                    Debug.LogWarning(
                        "[RevealLoadingScreenOnNavReady] UNITY_ADDRESSABLES is not defined; " +
                        "use SceneLoaderBuildIndex or enable Addressables.", this);
#endif
                    break;

                case DeferredLoadingTransitionTarget.SceneLoaderBuildIndex:
                    if (SceneLoader.Instance != null)
                        SceneLoader.Instance.TryCompleteDeferredLoadingTransition();
                    break;
            }
        }
    }
}
