using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class VerletHairChain : MonoBehaviour, IHairSimulation
    {
        private Transform _chainRoot;
        private GameObject _unitModel;
        private bool _initialized;

        public bool Enabled { get; set; }

        public void Initialize(Transform chainRoot, GameObject unitModel)
        {
            _chainRoot = chainRoot;
            _unitModel = unitModel;
            _initialized = chainRoot != null && unitModel != null;

            RefreshEnabledState();
        }

        private void Update()
        {
            if (!_initialized || !Enabled)
            {
                return;
            }

            UpdateSimulation();
        }

        public void UpdateSimulation()
        {
            if (!_initialized || !Enabled)
            {
                return;
            }

            // TODO: Verlet solver and collision response
        }

        public void RefreshEnabledState()
        {
            var settings = GameplayGeneralSettings.Instance;
            if (settings == null || !settings.ProceduralHairSimulation)
            {
                Enabled = false;
                return;
            }

            var playerSettings = GameplayPlayerSettings.Instance;
            int qualityStep = playerSettings != null ? playerSettings.QualityStep : 0;

            // Disable at the lowest quality setting
            Enabled = qualityStep > 0;
        }
    }
}
