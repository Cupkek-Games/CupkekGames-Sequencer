using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Evaluates a <see cref="SequencerConditionSO"/> once and runs ONE of two child lists
    /// sequentially — <see cref="_whenTrue"/> or <see cref="_whenFalse"/>. The unused branch is
    /// skipped entirely. Conditions are pluggable SOs, so the same Branch node expresses any
    /// predicate (a service is present, a save exists, a flag is set, …) without a new node type.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Branch (SO)")]
    public class BranchNodeSO : SequencerNodeSO
    {
        [Tooltip("Predicate evaluated once when this node runs. A null condition is treated as false.")]
        [SerializeField] private SequencerConditionSO _condition;

        [SerializeField] private List<SequencerNodeSO> _whenTrue = new();
        [SerializeField] private List<SequencerNodeSO> _whenFalse = new();

        // Static traversal only (session pre-marking): expose BOTH branches, since which one runs
        // isn't known until Execute evaluates the condition. Allocates — not used on the hot path.
        public override IReadOnlyList<SequencerNodeSO> Children
        {
            get
            {
                var all = new List<SequencerNodeSO>(_whenTrue.Count + _whenFalse.Count);
                all.AddRange(_whenTrue);
                all.AddRange(_whenFalse);
                return all;
            }
        }

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (_condition == null)
                Debug.LogWarning(
                    $"[SequencerExec] Branch '{name}': no condition assigned — treated as false.");

            bool result = _condition != null && _condition.Evaluate(runtime);
            List<SequencerNodeSO> branch = result ? _whenTrue : _whenFalse;

            if (runtime.LogExecution)
                Debug.Log(
                    $"[SequencerExec] Branch '{name}' -> {(result ? "WhenTrue" : "WhenFalse")} " +
                    $"({branch.Count} step(s))");

            for (int i = 0; i < branch.Count; i++)
                yield return runtime.ExecuteNode(branch[i]);
        }
    }
}
