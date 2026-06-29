using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// A pluggable boolean predicate evaluated by <see cref="BranchNodeSO"/> (and any future
    /// condition-driven node). Subclass it where the data it needs lives — e.g. a "service is
    /// registered" condition belongs in the ServiceLocator integration assembly, not core, so the
    /// core stays dependency-free.
    /// </summary>
    public abstract class SequencerConditionSO : ScriptableObject
    {
        /// <summary>
        /// Evaluate the predicate. <paramref name="runtime"/> is the active run, passed so future
        /// conditions can read run-scoped state (a blackboard) without a static channel.
        /// </summary>
        public abstract bool Evaluate(SequencerRuntime runtime);
    }
}
