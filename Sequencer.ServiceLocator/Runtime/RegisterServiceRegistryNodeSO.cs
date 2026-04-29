using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CupkekGames.Services;

namespace CupkekGames.Sequencer
{
    /// <summary>
    /// Sequencer step for registering SO-based registries/providers/services.
    /// </summary>
    [CreateAssetMenu(menuName = "CupkekGames/Sequencer/Register Services")]
    public class RegisterServiceRegistryNodeSO : SequencerNodeSO
    {
        [Tooltip("Additional ServiceRegistrySO assets, same as ServiceRegistry._registries.")]
        [SerializeField]
        private List<ServiceRegistrySO> _registries = new();

        [Header("Providers (ServiceProviderSO assets)")]
        [Tooltip("ScriptableObject providers (e.g. DataSerializerRegistrar). MonoBehaviour ServiceProvider belongs under Components or a prefab.")]
        [SerializeField]
        private List<ServiceProviderSO> _providers = new();

        [Header("ScriptableObject service entries")]
        [Tooltip("Same as ServiceRegistry._serviceEntries — optional register-as interface per entry.")]
        [SerializeField]
        private List<ServiceEntry> _serviceEntries = new();

        [Tooltip(
            "When true, runs the same teardown order as ServiceRegistry.UnregisterAll when the sequence run is disposed " +
            "(SequenceRunner destroyed or a new StartSequence flushes the previous run).")]
        [SerializeField]
        private bool _unregisterOnDispose;

        public override IEnumerator Execute(SequencerRuntime runtime)
        {
            RegisterAll();
            yield break;
        }

        public override void Dispose(SequencerRuntime runtime)
        {
            if (!_unregisterOnDispose)
                return;

            UnregisterAll();
        }

        private void RegisterAll()
        {
            foreach (ServiceRegistrySO registry in _registries)
            {
                if (registry != null)
                    registry.RegisterAll();
            }

            foreach (ServiceProviderSO provider in _providers)
            {
                if (provider != null)
                    provider.RegisterServices();
            }

            foreach (ServiceEntry entry in _serviceEntries)
            {
                if (entry?.Instance == null)
                    continue;
                Type serviceType = entry.ResolveServiceType();
                if (serviceType != null)
                    ServiceLocator.Register(entry.Instance, serviceType);
            }
        }

        private void UnregisterAll()
        {
            foreach (ServiceRegistrySO registry in _registries)
            {
                if (registry != null)
                    registry.UnregisterAll();
            }

            foreach (ServiceProviderSO provider in _providers)
            {
                if (provider != null)
                    provider.UnregisterServices();
            }

            foreach (ServiceEntry entry in _serviceEntries)
            {
                if (entry?.Instance == null)
                    continue;
                Type serviceType = entry.ResolveServiceType();
                if (serviceType != null)
                    ServiceLocator.Remove(serviceType);
            }
        }
    }
}
