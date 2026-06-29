using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Executes SO sequencer nodes with cycle protection.
    /// </summary>
    public sealed class SequencerRuntime
    {
        private readonly HashSet<SequencerNodeSO> _activePath;
        private readonly Stack<SequencerNodeSO> _deferredDispose;
        private readonly bool _logExecution;
        private readonly MonoBehaviour _coroutineHost;

        public SequencerRuntime(bool logExecution = true, MonoBehaviour coroutineHost = null)
            : this(logExecution, coroutineHost, new Stack<SequencerNodeSO>(), seedPath: null)
        {
        }

        // Forked-branch ctor: shares the root's deferred-dispose stack + host, and SEEDS its
        // cycle-guard path from the forking branch's current path (a COPY) so a real cycle back to an
        // ancestor is still caught, while concurrent siblings — each a separate copy taken before any
        // sibling runs — don't see one another. See ForkBranch / ParallelGroupSO.
        private SequencerRuntime(
            bool logExecution,
            MonoBehaviour coroutineHost,
            Stack<SequencerNodeSO> deferredDispose,
            HashSet<SequencerNodeSO> seedPath)
        {
            _logExecution = logExecution;
            _coroutineHost = coroutineHost;
            _deferredDispose = deferredDispose;
            _activePath = seedPath != null
                ? new HashSet<SequencerNodeSO>(seedPath)
                : new HashSet<SequencerNodeSO>();
        }

        public bool LogExecution => _logExecution;

        /// <summary>
        /// The MonoBehaviour driving this run (the owning <see cref="SequenceRunner"/>). Nodes that
        /// must fan out concurrent coroutines (e.g. <see cref="ParallelGroupSO"/>) start them on this
        /// host so Unity honors their yields (WaitForSeconds, AsyncOperation, nested sequences).
        /// Null when the runtime was constructed without a host — such nodes then run sequentially.
        /// </summary>
        public MonoBehaviour CoroutineHost => _coroutineHost;

        /// <summary>
        /// Create a child runtime for ONE concurrent branch (used by <see cref="ParallelGroupSO"/>). It
        /// shares this runtime's coroutine host and deferred-dispose stack — so disposes still flush
        /// with the owning <see cref="SequenceRunner"/> — but gets its own cycle-guard path seeded
        /// (copied) from this one, so sibling branches don't cross-trip the cycle detector.
        /// </summary>
        public SequencerRuntime ForkBranch() =>
            new SequencerRuntime(_logExecution, _coroutineHost, _deferredDispose, _activePath);

        /// <summary>
        /// Runs <see cref="SequencerNodeSO.Dispose"/> for every node that finished execution on this runtime,
        /// in reverse order of completion. A node is recorded only after its <see cref="SequencerNodeSO.Execute"/>
        /// returns — i.e. after the children it ran — so a container disposes before those children (per lineage;
        /// forked parallel branches interleave but each lineage keeps this order). Call when the owning
        /// <see cref="SequenceRunner"/> is destroyed or before starting a new run on the same runner.
        /// </summary>
        public void FlushDeferredDispose()
        {
            while (_deferredDispose.Count > 0)
            {
                SequencerNodeSO node = _deferredDispose.Pop();
                node.Dispose(this);
            }
        }

        public IEnumerator ExecuteNode(SequencerNodeSO node)
        {
            if (node == null)
                yield break;

            if (node.ExecutionPolicy == SequencerNodeExecutionPolicy.OncePerPlaySession
                && SequencerSessionState.HasCompletedThisSession(node))
            {
                if (_logExecution)
                {
                    Debug.Log(
                        "[SequencerExec] SKIP " +
                        $"node='{node.name}' type={node.GetType().Name} instanceId={node.GetHashCode()} " +
                        $"policy={node.ExecutionPolicy} " +
                        "(session set: pre-mark or completed earlier this play session)");
                }

                yield break;
            }

            if (!_activePath.Add(node))
            {
                Debug.LogError(
                    $"SequencerRuntime: recursive/cyclic sequence reference detected at '{node.name}'.");
                yield break;
            }

            try
            {
                if (_logExecution)
                {
                    Debug.Log(
                        "[SequencerExec] BEGIN " +
                        $"node='{node.name}' type={node.GetType().Name} instanceId={node.GetHashCode()} " +
                        $"policy={node.ExecutionPolicy}");
                }

                yield return node.Execute(this);

                if (_logExecution)
                    Debug.Log($"[SequencerExec] END node='{node.name}' type={node.GetType().Name}");

                if (node.ExecutionPolicy == SequencerNodeExecutionPolicy.OncePerPlaySession)
                    SequencerSessionState.MarkCompletedThisSession(
                        node,
                        "OncePerPlaySession after successful Execute");
            }
            finally
            {
                _deferredDispose.Push(node);
                _activePath.Remove(node);
            }
        }
    }
}
