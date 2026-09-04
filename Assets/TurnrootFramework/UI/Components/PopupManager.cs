using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class PopupManager : MonoBehaviour
    {
        [Serializable]
        public class PopupDefinition
        {
            public string id;
            public DismissablePopupNotification prefab;
        }

        public List<PopupDefinition> _popups = new();

        public Transform _container;

        private Brain _brain;
        private readonly Dictionary<string, DismissablePopupNotification> _prefabsById = new();
        private readonly HashSet<string> _activePopupIds = new();

        private void Awake()
        {
            _brain = GetAndCacheBrain.GetBrain();
            BuildPrefabLookup();

            if (_container == null)
            {
                _container = transform;
            }
        }

        private void BuildPrefabLookup()
        {
            _prefabsById.Clear();
            foreach (var popup in _popups)
            {
                if (string.IsNullOrWhiteSpace(popup.id) || popup.prefab == null)
                {
                    continue;
                }

                if (_prefabsById.ContainsKey(popup.id))
                {
                    $"PopupManager: Duplicate popup ID '{popup.id}' ignored.".LogWarning(
                        "PopupManager"
                    );
                    continue;
                }

                _prefabsById.Add(popup.id, popup.prefab);
            }
        }

        private void OnEnable()
        {
            if (_brain != null)
            {
                _brain.OnWaitForPlayerAcknowledgment += HandleWaitForPlayerAcknowledgment;
            }
        }

        private void OnDisable()
        {
            if (_brain != null)
            {
                _brain.OnWaitForPlayerAcknowledgment -= HandleWaitForPlayerAcknowledgment;
            }
        }

        private void HandleWaitForPlayerAcknowledgment(string id) => ShowPopup(id);

        public void ShowPopup(string id)
        {
            if (!_prefabsById.TryGetValue(id, out var prefab))
            {
                $"PopupManager: No popup prefab registered for ID '{id}'.".LogWarning(
                    "PopupManager"
                );
                return;
            }

            if (!_activePopupIds.Add(id))
            {
                $"PopupManager: Popup '{id}' is already active; ignoring duplicate request.".LogWarning(
                    "PopupManager"
                );
                return;
            }

            var instance = Instantiate(prefab, _container);
            instance.name = $"{prefab.name} ({id})";
            instance.OnDismissed += () => _activePopupIds.Remove(id);
            instance.Show();
        }
    }
}
