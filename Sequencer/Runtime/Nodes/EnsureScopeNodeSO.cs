using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Ensures a <see cref="SequencerScopeSO"/> is alive: skips when it already is (idempotent —
    /// safe to run from every scene inside the flow), otherwise spawns the configured prefabs as
    /// DontDestroyOnLoad and registers them into the scope. When <see cref="ResolveIdentity"/>
    /// returns a different identity than the live scope was created for, the scope is disposed
    /// and re-spawned (e.g. a new save session). End the scope at boundary scenes with
    /// <see cref="DisposeScopeNodeSO"/> — not via runner dispose, which is scene-lifetime.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Ensure Scope")]
    public class EnsureScopeNodeSO : SequencerNodeSO
    {
        [SerializeField] private SequencerScopeSO _scope;

        [Tooltip("Spawned DontDestroyOnLoad and registered into the scope when it is not alive.")]
        [SerializeField] private List<GameObject> _prefabs = new();

        public SequencerScopeSO Scope => _scope;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (_scope == null)
            {
                Debug.LogError($"[EnsureScopeNode] '{name}': no scope assigned.", this);
                yield break;
            }

            object identity = ResolveIdentity();

            if (_scope.IsAlive)
            {
                if (identity == null || ReferenceEquals(identity, _scope.Identity))
                {
                    OnAlreadyAlive();
                    yield break;
                }

                _scope.DisposeScope();
                yield return null;
            }

            _scope.Begin(identity);

            List<GameObject> spawned = new();
            for (int i = 0; i < _prefabs.Count; i++)
            {
                GameObject prefab = _prefabs[i];
                if (prefab == null)
                    continue;

                GameObject instance = Instantiate(prefab);
                DontDestroyOnLoad(instance);
                _scope.Register(instance);
                spawned.Add(instance);
            }

            OnSpawned(spawned);
        }

        /// <summary>
        /// Identity the scope should be bound to (e.g. the current save-session object). A live
        /// scope with a different identity is disposed and re-spawned; null means "no identity
        /// check" and always keeps the live scope. Default: null.
        /// </summary>
        protected virtual object ResolveIdentity() => null;

        /// <summary>Called instead of spawning when the scope is already alive (same identity).</summary>
        protected virtual void OnAlreadyAlive()
        {
        }

        /// <summary>Configure freshly spawned instances (runs once per spawn, after all prefabs).</summary>
        protected virtual void OnSpawned(List<GameObject> instances)
        {
        }
    }
}
