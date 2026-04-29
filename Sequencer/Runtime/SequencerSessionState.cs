using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Tracks <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/> completion for SO assets.
    /// Cleared on each Editor Play Mode entry / player start via <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/>
    /// (before the first scene’s <c>Awake</c>), not <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/> —
    /// the latter can run only once per domain when Enter Play Mode disables Domain Reload, leaving this set stale between plays.
    /// </summary>
    internal static class SequencerSessionState
    {
        private static readonly HashSet<SequencerNodeSO> CompletedOnceThisSession = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeFirstSceneLoad()
        {
            int had = CompletedOnceThisSession.Count;
            CompletedOnceThisSession.Clear();
            Debug.Log(
                "[SequencerSession] Cleared completion set " +
                $"(had {had} entr{(had == 1 ? "y" : "ies")}, BeforeSceneLoad / new play session).");
        }

        public static bool HasCompletedThisSession(SequencerNodeSO node)
        {
            return CompletedOnceThisSession.Contains(node);
        }

        public static void MarkCompletedThisSession(SequencerNodeSO node, string debugReason)
        {
            if (node == null)
                return;

            bool firstAdd = CompletedOnceThisSession.Add(node);
            Debug.Log(
                "[SequencerSession] MarkComplete " +
                $"node='{node.name}' instanceId={node.GetInstanceID()} " +
                $"reason={debugReason} " +
                $"firstAdd={firstAdd} " +
                $"(firstAdd=false → duplicate mark / same SO already in set)");
        }
    }
}
