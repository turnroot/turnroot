using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Gameplay.Audio
{
    public partial class AudioController : MonoBehaviour
    {
        #region Initialization & Help

        public void Initialize(Brain.Brain brain) => _brain = brain;

        private void Awake() => InitializeAudioGroups();

        private void InitializeAudioGroups()
        {
            _groupedSources = new Dictionary<AudioGroup, List<AudioSource>>();

            // Flatten all sources by group
            AddGroupSources(AudioGroup.Music, musicSources);
            AddGroupSources(AudioGroup.SFX, sfxSources);
            AddGroupSources(AudioGroup.Voices, voiceSources);
        }

        private void AddGroupSources(AudioGroup group, List<AudioSourceGroup> sourceGroups)
        {
            if (!_groupedSources.ContainsKey(group))
            {
                _groupedSources[group] = new List<AudioSource>();
            }

            foreach (var sourceGroup in sourceGroups)
            {
                if (sourceGroup.sources != null)
                {
                    _groupedSources[group].AddRange(sourceGroup.sources);
                }
            }
        }

#if UNITY_EDITOR
        [Button("📖 Show Audio System Help", EButtonEnableMode.Always)]
        private void ShowHelp()
        {
            // Use reflection to call the editor window since it's in a separate Editor assembly
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Type windowType = null;

            foreach (var assembly in assemblies)
            {
                windowType = assembly.GetType(
                    "Turnroot.Gameplay.Audio.Editor.AudioControllerHelpWindow"
                );
                if (windowType != null)
                {
                    break;
                }
            }

            if (windowType != null)
            {
                var showMethod = windowType.GetMethod(
                    "ShowWindowFromButton",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                showMethod?.Invoke(null, null);
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog(
                    "Help",
                    "Could not find AudioControllerHelpWindow editor script",
                    "OK"
                );
            }
        }
#endif

        #endregion
    }
}
