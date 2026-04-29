using System.Collections;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// SO-based sequencer node contract.
    /// </summary>
    public interface ISequencerNode
    {
        IEnumerator Execute(SequencerRuntime runtime);
        void Dispose(SequencerRuntime runtime);
    }
}
