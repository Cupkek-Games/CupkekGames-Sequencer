using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Activates or deactivates scene objects by name after earlier sequencer steps (e.g. service registration).
    /// Use with an inactive content root: keep gameplay/UI under a root disabled in the scene, enable it as the last node.
    /// Resolves names against the active scene's root objects first, then optional deep search (includes inactive children).
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Set GameObject Active")]
    public class SetGameObjectActiveNodeSO : SequencerNodeSO
    {
        [Tooltip("Object names to match (e.g. your disabled GameRoot).")]
        [SerializeField] private List<string> _targetNames = new();

        [SerializeField] private bool _active = true;

        [Tooltip("If true, search descendants when no root matches the name.")]
        [SerializeField] private bool _searchChildrenIfNotFoundAtRoot = true;

        [Tooltip("If true, On Dispose restores the previous activeSelf state captured in Execute.")]
        [SerializeField] private bool _restorePreviousStateOnDispose;

        private readonly List<(GameObject go, bool wasActive)> _captured = new();

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            _captured.Clear();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("SetGameObjectActiveNodeSO: no valid active scene.");
                yield break;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (string rawName in _targetNames)
            {
                string name = rawName?.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                GameObject found = FindByName(roots, name, _searchChildrenIfNotFoundAtRoot);
                if (found == null)
                {
                    Debug.LogWarning($"SetGameObjectActiveNodeSO: no GameObject named '{name}' in scene '{scene.name}'.");
                    continue;
                }

                if (_restorePreviousStateOnDispose)
                    _captured.Add((found, found.activeSelf));

                found.SetActive(_active);
            }
        }

        public override void Dispose(SequencerRuntime runtime)
        {
            if (!_restorePreviousStateOnDispose)
                return;

            for (int i = _captured.Count - 1; i >= 0; i--)
            {
                (GameObject go, bool wasActive) = _captured[i];
                if (go != null)
                    go.SetActive(wasActive);
            }

            _captured.Clear();
        }

        private static GameObject FindByName(GameObject[] roots, string name, bool searchChildren)
        {
            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;
                if (root.name == name)
                    return root;
            }

            if (!searchChildren)
                return null;

            foreach (GameObject root in roots)
            {
                if (root == null)
                    continue;
                Transform hit = FindDeep(root.transform, name);
                if (hit != null)
                    return hit.gameObject;
            }

            return null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
