#if UNITY_EDITOR
using Turnroot.UI;
using UnityEditor;
using UnityEngine;

namespace Turnroot.GameSettings
{
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
