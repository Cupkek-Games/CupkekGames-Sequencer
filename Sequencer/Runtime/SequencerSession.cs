namespace CupkekGames.Systems
{
    /// <summary>
    /// Public API for play-session bookkeeping used by <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/>.
    /// </summary>
    public static class SequencerSession
    {
        public static void MarkCompletedThisSession(SequencerNodeSO node, string debugReason = "unspecified")
        {
            SequencerSessionState.MarkCompletedThisSession(node, debugReason);
        }

        public static bool HasCompletedThisSession(SequencerNodeSO node)
        {
            return SequencerSessionState.HasCompletedThisSession(node);
        }
    }
}
