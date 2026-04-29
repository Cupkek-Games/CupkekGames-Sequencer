using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Pre-marks <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/> nodes in the session set so
    /// <see cref="SequenceRunner"/> skips those steps. <see cref="SequenceGroupSO"/> list entries are traversed
    /// recursively but <strong>group assets are not marked</strong> — only descendants with
    /// <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/> are, so <see cref="SequencerNodeExecutionPolicy.Always"/>
    /// steps in the same group still run.
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    public class SequencerSessionMarkComplete : MonoBehaviour
    {
        [Tooltip(
            "SO assets to pre-mark. For SequenceGroupSO, only OncePerPlaySession descendants are added (the group asset is not). " +
            "Always-policy steps still execute.")]
        [SerializeField]
        private List<SequencerNodeSO> _nodes = new();

        [Tooltip("If off, marking runs in Start() instead of Awake() (after other Awakes). Prefer on for ordering before SequenceRunner.")]
        [SerializeField]
        private bool _markOnAwake = true;

        private void Awake()
        {
            if (_markOnAwake)
                MarkAll();
        }

        private void Start()
        {
            if (!_markOnAwake)
                MarkAll();
        }

        private void MarkAll()
        {
            string host = $"{name} ({GetType().Name})";
            for (int i = 0; i < _nodes.Count; i++)
            {
                SequencerNodeSO node = _nodes[i];
                MarkRecursive(
                    node,
                    $"SequencerSessionMarkComplete list[{i}] from '{host}'");
            }
        }

        /// <summary>
        /// If <paramref name="node"/> is a <see cref="SequenceGroupSO"/>, recurses into its list without marking the group.
        /// Otherwise marks <paramref name="node"/> only when <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/>.
        /// </summary>
        private static void MarkRecursive(SequencerNodeSO node, string reason)
        {
            if (node == null)
                return;

            if (node is SequenceGroupSO group)
            {
                IReadOnlyList<SequencerNodeSO> children = group.Sequences;
                for (int c = 0; c < children.Count; c++)
                {
                    SequencerNodeSO child = children[c];
                    string childReason = $"{reason} > [{c}] '{child?.name ?? "null"}'";
                    MarkRecursive(child, childReason);
                }

                return;
            }

            if (node.ExecutionPolicy != SequencerNodeExecutionPolicy.OncePerPlaySession)
                return;

            SequencerSession.MarkCompletedThisSession(node, reason);
        }
    }
}
