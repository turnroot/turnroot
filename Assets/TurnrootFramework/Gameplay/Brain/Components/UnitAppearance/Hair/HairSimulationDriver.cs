using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class HairSimulationDriver : MonoBehaviour
    {
        private CharacterData _template;
        private Brain _brain;
        private readonly List<IHairSimulation> _chains = new();

        public void Initialize(CharacterData template, Brain brain)
        {
            _template = template;
            _brain = brain;

            SubscribeToQualityChanges();
            BuildChains();
            RefreshAll();
        }

        private void OnDestroy() => UnsubscribeFromQualityChanges();

        private void SubscribeToQualityChanges()
        {
            if (_brain != null)
            {
                _brain.OnGraphicsQualityChanged += OnGraphicsQualityChanged;
            }
        }

        private void UnsubscribeFromQualityChanges()
        {
            if (_brain != null)
            {
                _brain.OnGraphicsQualityChanged -= OnGraphicsQualityChanged;
            }
        }

        private void OnGraphicsQualityChanged() => RefreshAll();

        private void BuildChains()
        {
            _chains.Clear();

            var settings = GameplayGeneralSettings.Instance;
            if (settings == null || !settings.ProceduralHairSimulation)
            {
                return;
            }

            var chainNames = _template?.ProceduralBoneChains;
            if (chainNames == null || chainNames.Length == 0)
            {
                return;
            }

            foreach (var chainName in chainNames)
            {
                if (string.IsNullOrWhiteSpace(chainName))
                {
                    continue;
                }

                var chainRoot = FindChainRoot(chainName);
                if (chainRoot == null)
                {
                    $"HairSimulationDriver: could not find chain root '{chainName}' on {_template.DisplayName}".LogWarning(
                        "UnitAppearanceBrain"
                    );
                    continue;
                }

                var chainGo = new GameObject($"VerletHairChain_{chainName}");
                chainGo.transform.SetParent(transform, worldPositionStays: false);
                var chain = chainGo.AddComponent<VerletHairChain>();
                chain.Initialize(chainRoot, gameObject);
                _chains.Add(chain);
            }
        }

        private Transform FindChainRoot(string chainName)
        {
            var root = transform;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == chainName)
                {
                    return child;
                }
            }
            return null;
        }

        private void RefreshAll()
        {
            var settings = GameplayGeneralSettings.Instance;
            var playerSettings = GameplayPlayerSettings.Instance;
            int qualityStep = playerSettings != null ? playerSettings.QualityStep : 0;
            bool globallyEnabled =
                settings != null && settings.ProceduralHairSimulation && qualityStep > 0;

            foreach (var chain in _chains)
            {
                chain.Enabled = globallyEnabled;
                if (chain is MonoBehaviour behaviour)
                {
                    behaviour.enabled = globallyEnabled;
                }
            }
        }
    }
}
