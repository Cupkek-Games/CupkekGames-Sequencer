using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
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
        // Completed when the run in flight finishes; null while idle or before anyone waits.
        private UniTaskCompletionSource _idle;

        public IReadOnlyList<SequencerNodeSO> Sequences => _sequences;

        /// <summary>
        /// True while a Run() is in flight. Observability seam for boot orchestration
        /// and tests ("is any runner still booting?") — no reflection required.
        /// </summary>
        public bool IsRunning => _activeRun != null;

        /// <summary>
        /// Completes when no Run() is in flight: at once while idle, else when the run finishes
        /// (a restart mid-run waits for the new run). Cancelled by the token, or when the runner is
        /// destroyed before its run finished.
        /// </summary>
        public UniTask WaitIdleAsync(CancellationToken token)
        {
            if (!IsRunning) return UniTask.CompletedTask;
            _idle ??= new UniTaskCompletionSource();
            return _idle.Task.AttachExternalCancellation(token);
        }

        private void Awake()
        {
            if (_runOnAwake)
                StartSequence();
        }

        private void OnDestroy()
        {
            // Cancel the Run() loop AND any in-flight ParallelGroup child coroutines before disposing.
            StopAllCoroutines();
            _activeRun = null;
            _idle?.TrySetCanceled();
            _idle = null;

            _runtime?.FlushDeferredDispose();
            _runtime = null;
        }

        public void StartSequence()
        {
            // StopAllCoroutines, not just StopCoroutine(_activeRun): ParallelGroupSO fans its children
            // out as separate coroutines on this MonoBehaviour, so a mid-run restart must cancel those
            // too — otherwise they keep running against the runtime we're about to replace.
            StopAllCoroutines();
            _activeRun = null;

            _runtime?.FlushDeferredDispose();
            _runtime = new SequencerRuntime(_logExecution, this);
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
            UniTaskCompletionSource idle = _idle;
            _idle = null;
            idle?.TrySetResult();
        }
    }
}
