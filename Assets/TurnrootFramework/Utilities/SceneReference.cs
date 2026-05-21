using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Turnroot.Utilities
{
    /// <summary>
    /// A serializable scene reference that can be assigned in the Inspector.
    /// Stores the scene path internally and exposes the loaded Scene at runtime.
    /// </summary>
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
        [SerializeField]
        private string _scenePath = string.Empty;

        // Editor-only field — stripped from builds automatically via #if UNITY_EDITOR
#if UNITY_EDITOR
        [SerializeField]
        private UnityEditor.SceneAsset _sceneAsset;
#endif

        public string ScenePath => _scenePath;

        public string SceneName
        {
            get
            {
                if (string.IsNullOrEmpty(_scenePath))
                    return string.Empty;
                int slash = _scenePath.LastIndexOf('/');
                string name = slash >= 0 ? _scenePath.Substring(slash + 1) : _scenePath;
                // Strip .unity extension
                if (name.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 6);
                return name;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(_scenePath);

        /// <summary>Returns the loaded Scene. Only valid at runtime after the scene has been loaded.</summary>
        public Scene LoadedScene => SceneManager.GetSceneByPath(_scenePath);

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            _scenePath =
                _sceneAsset != null
                    ? UnityEditor.AssetDatabase.GetAssetPath(_sceneAsset)
                    : string.Empty;
#endif
        }

        public void OnAfterDeserialize() { }
    }
}
