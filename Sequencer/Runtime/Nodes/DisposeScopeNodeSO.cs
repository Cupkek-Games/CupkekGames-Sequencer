using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Ends the listed scopes: destroys every instance registered into each
    /// <see cref="SequencerScopeSO"/> and clears its state. Author one at each flow boundary
    /// (e.g. the main-menu scene runner) listing ALL scopes that must not survive past it —
    /// disposing a scope that is not alive is a no-op, so over-listing is safe and prevents leaks.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Dispose Scopes")]
    public class DisposeScopeNodeSO : SequencerNodeSO
    {
        [SerializeField] private List<SequencerScopeSO> _scopes = new();

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            for (int i = 0; i < _scopes.Count; i++)
            {
                SequencerScopeSO scope = _scopes[i];
                if (scope == null)
                {
                    Debug.LogError($"[DisposeScopeNode] '{name}': null scope at index {i}.", this);
                    continue;
                }

                scope.DisposeScope();
            }

            yield break;
        }
    }
}
