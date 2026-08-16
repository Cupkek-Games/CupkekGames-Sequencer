using System.Collections.Generic;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Tracks <see cref="SequencerNodeExecutionPolicy.OncePerPlaySession"/> completion for SO assets.
    /// Cleared per play session by the generated statics cleanup.
    ///
    /// This previously used <see cref="RuntimeInitializeLoadType.BeforeSceneLoad"/> and claimed
    /// <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/> runs only once per domain when
    /// Enter Play Mode disables Domain Reload. That was not true in 6000.6 — both load types re-fire
    /// every play session. Either would have worked; the attribute now covers it.
    /// </summary>
    internal static partial class SequencerSessionState
    {
        [AutoStaticsCleanup]
        private static HashSet<SequencerNodeSO> CompletedOnceThisSession = new();

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
                $"node='{node.name}' instanceId={node.GetHashCode()} " +
                $"reason={debugReason} " +
                $"firstAdd={firstAdd} " +
                $"(firstAdd=false → duplicate mark / same SO already in set)");
        }
    }
}
