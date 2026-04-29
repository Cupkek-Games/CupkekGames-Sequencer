namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Controls how often a sequencer node runs when referenced from multiple <see cref="SequenceRunner"/> instances or scenes.
    /// </summary>
    public enum SequencerNodeExecutionPolicy
    {
        /// <summary>Run every time the sequence runs.</summary>
        Always = 0,

        /// <summary>
        /// Run at most once per play session (from Enter Play Mode until exit). Resets automatically on the next run.
        /// Use when the same SO is wired into runners in several scenes so bootstrap does not repeat.
        /// </summary>
        OncePerPlaySession = 1
    }
}
