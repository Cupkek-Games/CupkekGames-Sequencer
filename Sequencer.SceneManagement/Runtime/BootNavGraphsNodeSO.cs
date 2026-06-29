using System.Collections;
using CupkekGames.Luna.Navigation;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Boots every <see cref="NavHostBoot.Manual"/> NavHost in the scene via
    /// <see cref="LunaLayers.BootDeferred"/>, so their views spawn now that the sequencer has
    /// registered services / instantiated dependency prefabs. Place this AFTER the
    /// service/dependency setup nodes and BEFORE the readiness wait / reveal node, so a view's
    /// <c>Awake</c> never runs before what it reads exists.
    ///
    /// <para>
    /// Idempotent: <see cref="NavHostBoot.OnAwake"/> hosts (already booted in their Awake) and
    /// persistent hosts booted in an earlier scene are no-ops. Fans out over the runtime registry,
    /// so it needs no scene references and works for any number of hosts.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Boot Nav Graphs")]
    public class BootNavGraphsNodeSO : SequencerNodeSO
    {
        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            LunaLayers.BootDeferred();
            yield break;
        }
    }
}
