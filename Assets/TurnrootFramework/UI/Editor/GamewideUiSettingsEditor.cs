#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Turnroot.GameSettings.GamewideUiSettings))]
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
#endif
