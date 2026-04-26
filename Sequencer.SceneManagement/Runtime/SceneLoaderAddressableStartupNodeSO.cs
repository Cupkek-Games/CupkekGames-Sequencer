#if UNITY_ADDRESSABLES
using System.Collections;
using System.Collections.Generic;
using CupkekGames.Luna;
using UnityEngine;

namespace CupkekGames.Systems
{
    /// <summary>
    /// Sequencer equivalent of addressables bootstrap (see <see cref="InitializationLoader"/>): loads one or more
    /// <see cref="SceneSO"/> via <see cref="SceneLoaderAddressable"/> with a <see cref="SceneTransitionDatabase"/> transition.
    /// Fire-and-forget like <see cref="SceneLoaderStartupNodeSO"/>; use <see cref="CompleteDeferredLoadingTransitionNodeSO"/>
    /// with target <see cref="DeferredLoadingTransitionTarget.SceneLoaderAddressable"/> when deferring fade-out.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Load Scene (Addressables Startup)")]
    public class SceneLoaderAddressableStartupNodeSO : SequencerNodeSO
    {
        [Tooltip("Scenes to load (additive). Null entries are skipped.")]
        [SerializeField]
        private List<SceneSO> _scenesToLoad = new();

        [SerializeField]
        private string _transitionKey = "Fade";

        [Tooltip("Keeps the loading transition visible until SceneLoaderAddressable.CompleteDeferredLoadingTransition().")]
        [SerializeField]
        private bool _deferFadeOutUntilManualComplete;

        [Tooltip("If true, calls SceneLoaderAddressable.SetActiveScene on the first non-null scene before load (same idea as InitializationLoader).")]
        [SerializeField]
        private bool _setActiveSceneFromFirst = true;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            List<SceneSO> scenes = new List<SceneSO>();
            for (int i = 0; i < _scenesToLoad.Count; i++)
            {
                if (_scenesToLoad[i] != null)
                    scenes.Add(_scenesToLoad[i]);
            }

            if (scenes.Count == 0)
            {
                Debug.LogWarning("SceneLoaderAddressableStartupNodeSO: Scenes To Load has no valid SceneSO entries.");
                yield break;
            }

            if (SceneLoaderAddressable.Instance == null)
            {
                Debug.LogWarning(
                    "SceneLoaderAddressableStartupNodeSO: SceneLoaderAddressable.Instance is null — add it to the scene or bootstrap prefab.");
                yield break;
            }

            if (SceneTransitionDatabase.Instance == null)
            {
                Debug.LogWarning("SceneLoaderAddressableStartupNodeSO: SceneTransitionDatabase.Instance is null.");
                yield break;
            }

            SceneTransition transition =
                SceneTransitionDatabase.Instance.Transitions.GetValue(_transitionKey);

            if (transition == null)
            {
                Debug.LogWarning(
                    $"SceneLoaderAddressableStartupNodeSO: no SceneTransition for key '{_transitionKey}'.");
                yield break;
            }

            if (_setActiveSceneFromFirst)
                SceneLoaderAddressable.Instance.SetActiveScene(scenes[0]);

            SceneLoaderAddressable.Instance.LoadScene(scenes, transition, _deferFadeOutUntilManualComplete);

            yield break;
        }
    }
}
#endif
