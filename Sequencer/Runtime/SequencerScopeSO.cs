using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Identity asset for a cross-scene lifetime ("scope"): DontDestroyOnLoad instances that must
    /// survive scene loads within a flow and be destroyed together at an explicit boundary.
    /// Spawned into by <see cref="EnsureScopeNodeSO"/>, ended by <see cref="DisposeScopeNodeSO"/>
    /// (or <see cref="DisposeScope"/> from code). Holds runtime state only — nothing serializes;
    /// state resets on each play-session start (BeforeSceneLoad, same rationale as
    /// <see cref="SequencerSessionState"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Scope")]
    public class SequencerScopeSO : ScriptableObject
    {
        private static readonly HashSet<SequencerScopeSO> ActiveScopes = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeFirstSceneLoad()
        {
            foreach (SequencerScopeSO scope in ActiveScopes)
                scope.ClearState();

            ActiveScopes.Clear();
        }

        private readonly List<GameObject> _instances = new();
        private object _identity;

        /// <summary>
        /// The identity the live scope was created for (see
        /// <see cref="EnsureScopeNodeSO.ResolveIdentity"/>). Null when the scope is not alive or
        /// was created without an identity.
        /// </summary>
        public object Identity => _identity;

        /// <summary>
        /// True while at least one registered instance is still alive. Externally destroyed
        /// instances make the scope read dead, so a later Ensure re-spawns it.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                for (int i = 0; i < _instances.Count; i++)
                {
                    if (_instances[i] != null)
                        return true;
                }

                return false;
            }
        }

        public IReadOnlyList<GameObject> Instances => _instances;

        public void Begin(object identity)
        {
            _instances.Clear();
            _identity = identity;
            ActiveScopes.Add(this);
        }

        public void Register(GameObject instance)
        {
            if (instance == null)
                return;

            _instances.Add(instance);
            ActiveScopes.Add(this);
        }

        public void DisposeScope()
        {
            int destroyed = 0;
            for (int i = 0; i < _instances.Count; i++)
            {
                GameObject instance = _instances[i];
                if (instance != null)
                {
                    Destroy(instance);
                    destroyed++;
                }
            }

            if (destroyed > 0)
                Debug.Log($"[SequencerScope] Dispose scope='{name}' destroyed={destroyed}");

            ClearState();
            ActiveScopes.Remove(this);
        }

        private void ClearState()
        {
            _instances.Clear();
            _identity = null;
        }
    }
}
