using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Mono sequencer runner. Runs configured SO sequences on Awake by default.
    /// </summary>
    public class SequenceRunner : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] private bool _runOnAwake = true;

        [Tooltip("Logs [SequencerExec] BEGIN/END/SKIP and root steps (filter Console by SequencerExec).")]
        [SerializeField]
        private bool _logExecution = true;

        [Header("Sequence List")]
        [SerializeField] private List<SequencerNodeSO> _sequences = new();

        private Coroutine _activeRun;
        private SequencerRuntime _runtime;

        public IReadOnlyList<SequencerNodeSO> Sequences => _sequences;

        private void Awake()
        {
            if (_runOnAwake)
                StartSequence();
        }

        private void OnDestroy()
        {
            if (_activeRun != null)
            {
                StopCoroutine(_activeRun);
                _activeRun = null;
            }

            _runtime?.FlushDeferredDispose();
            _runtime = null;
        }

        public void StartSequence()
        {
            if (_activeRun != null)
            {
                StopCoroutine(_activeRun);
                _activeRun = null;
            }

            _runtime?.FlushDeferredDispose();
            _runtime = new SequencerRuntime(_logExecution);
            _activeRun = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            SequencerRuntime runtime = _runtime;
            if (runtime.LogExecution)
            {
                Debug.Log(
                    "[SequencerExec] StartSequence " +
                    $"runner='{gameObject.name}' scene='{SceneManager.GetActiveScene().name}' " +
                    $"rootSteps={_sequences.Count}");
            }

            for (int i = 0; i < _sequences.Count; i++)
            {
                SequencerNodeSO step = _sequences[i];
                if (runtime.LogExecution && step != null)
                {
                    Debug.Log(
                        "[SequencerExec] Root step " +
                        $"[{i + 1}/{_sequences.Count}] -> '{step.name}' ({step.GetType().Name})");
                }

                yield return runtime.ExecuteNode(step);
            }

            if (runtime.LogExecution)
                Debug.Log($"[SequencerExec] StartSequence finished runner='{gameObject.name}'");

            _activeRun = null;
        }
    }
}
