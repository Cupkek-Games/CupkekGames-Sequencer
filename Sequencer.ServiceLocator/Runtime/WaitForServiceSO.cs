using System;
using System.Collections;
using UnityEngine;
using CupkekGames.Services;

namespace CupkekGames.Sequencer
{
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Wait For Service")]
    public class WaitForServiceSO : SequencerNodeSO
    {
        [SerializeField] private string _serviceTypeName;
        [SerializeField] private float _timeoutSeconds = 5f;
        [SerializeField] private bool _logWarningOnTimeout = true;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            if (string.IsNullOrWhiteSpace(_serviceTypeName))
            {
                Debug.LogWarning("WaitForServiceSO: service type name is empty. Continuing.");
                yield break;
            }

            Type serviceType = Type.GetType(_serviceTypeName);
            if (serviceType == null)
            {
                Debug.LogError($"WaitForServiceSO: unable to resolve type '{_serviceTypeName}'.");
                yield break;
            }

            float start = Time.realtimeSinceStartup;
            while (!ServiceLocator.RegisteredServices.ContainsKey(serviceType))
            {
                if (_timeoutSeconds > 0 && Time.realtimeSinceStartup - start >= _timeoutSeconds)
                {
                    if (_logWarningOnTimeout)
                    {
                        Debug.LogWarning(
                            $"WaitForServiceSO: timeout while waiting for service '{serviceType.FullName}'.");
                    }
                    yield break;
                }

                yield return null;
            }
        }

        public override void Dispose(SequencerRuntime runtime)
        {
        }
    }
}
