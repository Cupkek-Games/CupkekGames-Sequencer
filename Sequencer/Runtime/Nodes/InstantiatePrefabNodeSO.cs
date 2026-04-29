using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Sequencer
{
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Instantiate Prefabs")]
    public class InstantiatePrefabNodeSO : SequencerNodeSO
    {
        [SerializeField] private List<GameObject> _prefabs = new();
        [SerializeField] private bool _dontDestroyOnLoad = true;
        [Tooltip("If true, spawned instances are Destroyed when the owning SequenceRunner disposes the run " +
                 "(OnDestroy or before a new StartSequence), not when this step’s Execute returns.")]
        [SerializeField] private bool _destroyOnDispose;

        private readonly List<GameObject> _spawned = new();

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            _spawned.Clear();

            for (int i = 0; i < _prefabs.Count; i++)
            {
                GameObject prefab = _prefabs[i];
                if (prefab == null)
                    continue;

                GameObject instance = Instantiate(prefab);
                if (_dontDestroyOnLoad)
                    DontDestroyOnLoad(instance);

                _spawned.Add(instance);
            }

            yield break;
        }

        public override void Dispose(SequencerRuntime runtime)
        {
            if (!_destroyOnDispose)
                return;

            for (int i = 0; i < _spawned.Count; i++)
            {
                GameObject instance = _spawned[i];
                if (instance != null)
                    Destroy(instance);
            }

            _spawned.Clear();
        }
    }
}
