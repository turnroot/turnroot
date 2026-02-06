#if UNITY_EDITOR
using Turnroot.UI;
using UnityEditor;
using UnityEngine;

namespace Turnroot.GameSettings
{
    /// <summary>
    /// Custom editor for GamewideUiSettings that provides a button to apply menu button spacing.
    /// </summary>
    [CustomEditor(typeof(GamewideUiSettings))]
    public class GamewideUiSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(8);
            if (GUILayout.Button("Apply Menu Button Spacing"))
            {
                UiTools.ApplyMenuButtonSpacing();
            }
        }
    }
}
#endif
