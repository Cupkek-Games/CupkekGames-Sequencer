using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// A fixed true/false <see cref="SequencerConditionSO"/> — the trivial condition. Useful for
    /// authoring or testing a <see cref="BranchNodeSO"/> before a real predicate exists, or to flip a
    /// branch from a single shared asset.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Conditions/Constant")]
    public class ConstantConditionSO : SequencerConditionSO
    {
        [SerializeField] private bool _value = true;

        public override bool Evaluate(SequencerRuntime runtime) => _value;
    }
}
