using System.Collections;
using CupkekGames.Luna;
using UnityEngine;

namespace CupkekGames.Systems
{
    /// <summary>
    /// Sequencer equivalent of <see cref="SceneLoaderStartup"/>: requests a build-index load via
    /// <see cref="SceneLoader"/> using a transition from <see cref="SceneTransitionDatabase"/>.
    /// Does not wait for the load to finish — same fire-and-forget behavior as the MonoBehaviour’s <c>Start</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Load Scene (SceneLoader Startup)")]
    public class SceneLoaderStartupNodeSO : SequencerNodeSO
    {
        [SerializeField]
        private string _startSceneName = "";

        [Header("Index is used if name is empty")]
        [SerializeField]
        private int _startSceneIndex = 1;

        [SerializeField]
        private string _transitionKey = "Fade";

        [Tooltip("Keeps the loading transition visible until SceneLoader.CompleteDeferredLoadingTransition() (e.g. sequencer in the loaded scene).")]
        [SerializeField]
        private bool _deferFadeOutUntilManualComplete;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (SceneLoader.Instance == null)
            {
                Debug.LogWarning(
                    "SceneLoaderStartupNodeSO: SceneLoader.Instance is null — add SceneLoader to the scene or an instantiated bootstrap prefab.");
                yield break;
            }

            if (SceneTransitionDatabase.Instance == null)
            {
                Debug.LogWarning("SceneLoaderStartupNodeSO: SceneTransitionDatabase.Instance is null.");
                yield break;
            }

            SceneTransition transition =
                SceneTransitionDatabase.Instance.Transitions.GetValue(_transitionKey);

            if (transition == null)
            {
                Debug.LogWarning(
                    $"SceneLoaderStartupNodeSO: no SceneTransition for key '{_transitionKey}'.");
                yield break;
            }

            if (string.IsNullOrEmpty(_startSceneName))
                SceneLoader.Instance.LoadScene(_startSceneIndex, transition, _deferFadeOutUntilManualComplete);
            else
                SceneLoader.Instance.LoadScene(_startSceneName, transition, _deferFadeOutUntilManualComplete);

            yield break;
        }
    }
}
