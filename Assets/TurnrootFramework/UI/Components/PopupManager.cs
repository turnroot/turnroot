using System;
using System.Collections.Generic;
using NaughtyAttributes;
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
            [InfoBox(
                "Unique ID per popup. When triggered from a Conversation, skip the PART prefix: i.e. \"Action_GainSupport_Aubrey_P\", so you don't have to make dozens of duplicate prefabs."
            )]
            public string id;
            public DismissablePopupNotification prefab;
        }

        [InfoBox(
            "Put every popup that could occur in this scene here, have a PopupManager per scene."
        )]
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

                // strip 'PART*_' if it's there
                if (popup.id.StartsWith("PART"))
                {
                    var index = popup.id.IndexOf('_', 4);
                    if (index != -1)
                    {
                        popup.id = popup.id[(index + 1)..];
                    }
                    "You don't need to include the PART prefix in popup IDs; it will be stripped automatically. Life will be much easier if you don't! That way, you can reuse something like \"Action_GainSupport_Aubrey_P\" repeatedly.".LogInfo(
                        "PopupManager"
                    );
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
                _brain.OnConversationActionNotificationRequested +=
                    HandleActionNotificationRequested;
            }
        }

        private void OnDisable()
        {
            if (_brain != null)
            {
                _brain.OnConversationActionNotificationRequested -=
                    HandleActionNotificationRequested;
            }
        }

        private void HandleActionNotificationRequested(string id) => ShowPopup(id);

        public void ShowPopup(string id)
        {
            var strippedId = id;
            if (strippedId.StartsWith("PART"))
            {
                var index = strippedId.IndexOf('_', 4);
                if (index != -1)
                {
                    strippedId = strippedId[(index + 1)..];
                }
            }
            if (!_prefabsById.TryGetValue(strippedId, out var prefab))
            {
                $"PopupManager: No popup prefab registered for '{strippedId}'.".LogWarning(
                    "PopupManager"
                );
                return;
            }

            $"PopupManager: Showing popup '{strippedId}'.".LogInfo("PopupManager");

            if (!_activePopupIds.Add(strippedId))
            {
                $"PopupManager: Popup '{strippedId}' is already active; ignoring duplicate request.".LogWarning(
                    "PopupManager"
                );
                return;
            }

            var instance = Instantiate(prefab, _container);
            instance.name = $"{prefab.name} ({strippedId})";
            instance.OnDismissed += () => _activePopupIds.Remove(strippedId);
            instance.Show();
        }
    }
}
