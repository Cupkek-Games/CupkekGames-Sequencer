using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Systems
{
    /// <summary>
    /// Executes SO sequencer nodes with cycle protection.
    /// </summary>
    public sealed class SequencerRuntime
    {
        private readonly HashSet<SequencerNodeSO> _activePath = new();
        private readonly Stack<SequencerNodeSO> _deferredDispose = new();
        private readonly bool _logExecution;

        public SequencerRuntime(bool logExecution = true)
        {
            _logExecution = logExecution;
        }

        public bool LogExecution => _logExecution;

        /// <summary>
        /// Runs <see cref="SequencerNodeSO.Dispose"/> for every node that finished execution on this runtime,
        /// in reverse completion order (nested children before parents). Call when the owning
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
                        $"node='{node.name}' type={node.GetType().Name} instanceId={node.GetInstanceID()} " +
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
                        $"node='{node.name}' type={node.GetType().Name} instanceId={node.GetInstanceID()} " +
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
