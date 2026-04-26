using System.Collections;

namespace CupkekGames.Systems
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
