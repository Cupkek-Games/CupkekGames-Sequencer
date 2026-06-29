using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Runs its children CONCURRENTLY and completes once every child has finished — the parallel
    /// counterpart to <see cref="SequenceGroupSO"/>. Each child is started as a real coroutine on the
    /// run's <see cref="SequencerRuntime.CoroutineHost"/>, so Unity honors its yields (WaitForSeconds,
    /// AsyncOperation, nested sequences). Wall-clock cost is the slowest child, not the sum.
    ///
    /// <para>
    /// Each branch runs on its own <see cref="SequencerRuntime.ForkBranch"/> child runtime, so the
    /// cycle guard is per-branch: sibling branches that legitimately reach the same node don't
    /// cross-trip each other, while a real cycle back to an ancestor is still caught. A node reachable
    /// from two branches simply runs once per branch (a <c>OncePerPlaySession</c> node still de-dups
    /// via the session set). If the runtime has no coroutine host, this falls back to sequential
    /// execution with a warning rather than silently doing nothing.
    /// </para>
    ///
    /// <para>
    /// ⚠ Because a shared node runs once <i>per branch</i>, do NOT place the same <b>stateful</b> node
    /// — one that tracks per-asset instances across Execute/Dispose, e.g. <c>InstantiatePrefabNodeSO</c>
    /// or <c>SetGameObjectActiveNodeSO</c> — in two concurrent branches: that tracking isn't
    /// concurrency-safe, so one branch's bookkeeping clobbers the other's and a dispose batch leaks.
    /// Keep concurrent branches disjoint for such nodes (idempotent / <c>OncePerPlaySession</c> nodes
    /// are fine to share).
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Parallel Group (SO)")]
    public class ParallelGroupSO : SequencerNodeSO
    {
        [SerializeField] private List<SequencerNodeSO> _sequences = new();

        public IReadOnlyList<SequencerNodeSO> Sequences => _sequences;

        public override IReadOnlyList<SequencerNodeSO> Children => _sequences;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            MonoBehaviour host = runtime.CoroutineHost;

            if (host == null)
            {
                Debug.LogWarning(
                    $"[SequencerExec] ParallelGroup '{name}': runtime has no CoroutineHost — " +
                    "running children sequentially instead of in parallel.");
                for (int i = 0; i < _sequences.Count; i++)
                    yield return runtime.ExecuteNode(_sequences[i]);
                yield break;
            }

            // Shared counter, decremented as each child finishes. Coroutines are cooperative
            // (single-threaded), so no locking is needed.
            int remaining = 0;
            for (int i = 0; i < _sequences.Count; i++)
            {
                SequencerNodeSO child = _sequences[i];
                if (child == null) continue;

                if (runtime.LogExecution)
                    Debug.Log(
                        "[SequencerExec] Parallel child START " +
                        $"group='{name}' [{i + 1}/{_sequences.Count}] -> '{child.name}' ({child.GetType().Name})");

                remaining++;
                // ForkBranch: each child gets its own cycle-guard path (copied from here, so a real
                // cycle back to an ancestor is still caught) while sharing the deferred-dispose stack,
                // so disposes flush with the owning run. Started on the host MonoBehaviour, which the
                // SequenceRunner cancels via StopAllCoroutines on teardown/restart.
                host.StartCoroutine(RunChild(runtime.ForkBranch(), child, () => remaining--));
            }

            while (remaining > 0)
                yield return null;
        }

        private static IEnumerator RunChild(SequencerRuntime runtime, SequencerNodeSO child, Action onDone)
        {
            yield return runtime.ExecuteNode(child);
            onDone();
        }
    }
}
