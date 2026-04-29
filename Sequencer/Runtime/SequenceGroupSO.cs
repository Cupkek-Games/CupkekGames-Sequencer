using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Sequence Group (SO)")]
    public class SequenceGroupSO : SequencerNodeSO
    {
        [SerializeField] private List<SequencerNodeSO> _sequences = new();

        public IReadOnlyList<SequencerNodeSO> Sequences => _sequences;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            for (int i = 0; i < _sequences.Count; i++)
            {
                SequencerNodeSO child = _sequences[i];
                if (runtime.LogExecution && child != null)
                {
                    Debug.Log(
                        "[SequencerExec] Group child " +
                        $"group='{name}' [{i + 1}/{_sequences.Count}] -> '{child.name}' ({child.GetType().Name})");
                }

                yield return runtime.ExecuteNode(child);
            }
        }
    }
}
