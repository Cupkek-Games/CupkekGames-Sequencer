using UnityEngine;

namespace CupkekGames.Systems
{
    /// <summary>
    /// Base class for SO sequencer nodes.
    /// </summary>
    public abstract class SequencerNodeSO : ScriptableObject, ISequencerNode
    {
        [Header("Execution")]
        [Tooltip("OncePerPlaySession: this asset runs once until you exit Play mode; safe to reuse the same SO from SequenceRunners in every scene.")]
        [SerializeField]
        private SequencerNodeExecutionPolicy _executionPolicy = SequencerNodeExecutionPolicy.Always;

        public SequencerNodeExecutionPolicy ExecutionPolicy => _executionPolicy;

        public abstract System.Collections.IEnumerator Execute(SequencerRuntime runtime);

        /// <summary>
        /// Called when the owning <see cref="SequenceRunner"/> is destroyed or a new run replaces the previous one
        /// (see <see cref="SequencerRuntime.FlushDeferredDispose"/>), not immediately after <see cref="Execute"/>.
        /// </summary>
        public virtual void Dispose(SequencerRuntime runtime)
        {
        }
    }
}
