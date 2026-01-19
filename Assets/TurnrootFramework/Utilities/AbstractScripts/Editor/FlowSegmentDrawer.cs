using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    [CustomPropertyDrawer(typeof(FlowSegment))]
    public class FlowSegmentDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the foldout
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label
            );

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                // Get the stateId and onSegmentReached properties
                var stateIdProperty = property.FindPropertyRelative("stateId");
                var onSegmentReachedProperty = property.FindPropertyRelative("onSegmentReached");

                // Draw stateId as a dropdown
                Rect stateIdRect = new Rect(
                    position.x,
                    position.y
                        + EditorGUIUtility.singleLineHeight
                        + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );

                string[] allStateIds = BrainStateNames.GetAllStateIds();
                int currentIndex = System.Array.IndexOf(allStateIds, stateIdProperty.stringValue);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                int newIndex = EditorGUI.Popup(stateIdRect, "State ID", currentIndex, allStateIds);
                stateIdProperty.stringValue = allStateIds[newIndex];

                // Draw onSegmentReached
                Rect eventRect = new Rect(
                    position.x,
                    position.y
                        + (
                            EditorGUIUtility.singleLineHeight
                            + EditorGUIUtility.standardVerticalSpacing
                        ) * 2,
                    position.width,
                    EditorGUI.GetPropertyHeight(onSegmentReachedProperty)
                );

                EditorGUI.PropertyField(
                    eventRect,
                    onSegmentReachedProperty,
                    new GUIContent("On Segment Reached"),
                    true
                );

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var onSegmentReachedProperty = property.FindPropertyRelative("onSegmentReached");
            float eventHeight = EditorGUI.GetPropertyHeight(onSegmentReachedProperty);

            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing)
                    * 2
                + eventHeight;
        }
    }
}
