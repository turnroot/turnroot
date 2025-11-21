// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

namespace UnityEditor
{
    // State for when we're dragging a knob.
    internal class KnobState
    {
        public float dragStartPos;
        public float dragStartValue;
        public bool isDragging;
        public bool isEditing;
    }

    // Provide knob layout helper without redefining Unity's EditorGUILayout
    public static class KnobGUILayout
    {
        public static float Knob(
            Vector2 knobSize,
            float value,
            float minValue,
            float maxValue,
            string unit,
            Color backgroundColor,
            Color activeColor,
            bool showValue,
            string valueFormat = "0.##",
            float snap = 0f,
            float neutralAngle = -90f,
            float sweep = 270f,
            params GUILayoutOption[] options
        )
        {
            Rect labelRect = Rect.zero;
            if (showValue)
            {
                labelRect = UnityEditor.EditorGUILayout.GetControlRect(false, 18f);
            }
            Rect r = UnityEditor.EditorGUILayout.GetControlRect(false, knobSize.y, options);
            return KnobGUI.Knob(
                r,
                labelRect,
                knobSize,
                value,
                minValue,
                maxValue,
                unit,
                backgroundColor,
                activeColor,
                showValue,
                valueFormat,
                snap,
                neutralAngle,
                sweep,
                GUIUtility.GetControlID("Knob".GetHashCode(), FocusType.Passive, r)
            );
        }
    }

    // Knob rendering implementation stored separately to avoid colliding with UnityEditor.EditorGUI
    public static class KnobGUI
    {
        internal struct KnobContext
        {
            readonly Rect position;
            readonly Rect labelPosition;
            readonly Vector2 knobSize;
            readonly float currentValue;
            readonly float start;
            readonly float end;
            readonly string unit;
            readonly Color activeColor;
            readonly Color backgroundColor;
            readonly bool showValue;
            readonly string valueFormat;
            readonly float snap;
            readonly float neutralAngle;
            readonly float sweepDegrees;
            readonly int id;

            private const int kPixelRange = 250;

            public KnobContext(
                Rect position,
                Rect labelPosition,
                Vector2 knobSize,
                float currentValue,
                float start,
                float end,
                string unit,
                Color backgroundColor,
                Color activeColor,
                bool showValue,
                string valueFormat,
                float snap,
                float neutralAngle,
                float sweepDegrees,
                int id
            )
            {
                this.position = position;
                this.labelPosition = labelPosition;
                this.knobSize = knobSize;
                this.currentValue = currentValue;
                this.start = start;
                this.end = end;
                this.unit = unit;
                this.activeColor = activeColor;
                this.backgroundColor = backgroundColor;
                this.showValue = showValue;
                this.valueFormat = valueFormat;
                this.snap = snap;
                this.neutralAngle = neutralAngle;
                this.sweepDegrees = sweepDegrees;
                this.id = id;
            }

            public float Handle()
            {
                if (KnobState().isEditing && CurrentEventType() != EventType.Repaint)
                    return DoKeyboardInput();

                switch (CurrentEventType())
                {
                    case EventType.MouseDown:
                        return OnMouseDown();

                    case EventType.MouseDrag:
                        return OnMouseDrag();

                    case EventType.MouseUp:
                        return OnMouseUp();

                    case EventType.Repaint:
                        return OnRepaint();
                }

                return currentValue;
            }

            private EventType CurrentEventType()
            {
                return CurrentEvent().type;
            }

            private bool IsEmptyKnob()
            {
                return start == end;
            }

            private Event CurrentEvent()
            {
                return Event.current;
            }

            private float Clamp(float value)
            {
                return Mathf.Clamp(value, MinValue(), MaxValue());
            }

            private float ClampedCurrentValue()
            {
                return Clamp(currentValue);
            }

            private float MaxValue()
            {
                return Mathf.Max(start, end);
            }

            private float MinValue()
            {
                return Mathf.Min(start, end);
            }

            private float GetCurrentValuePercent()
            {
                return (ClampedCurrentValue() - MinValue()) / (MaxValue() - MinValue());
            }

            private float MousePosition()
            {
                return CurrentEvent().mousePosition.y - position.y;
            }

            private bool WasDoubleClick()
            {
                return CurrentEventType() == EventType.MouseDown && CurrentEvent().clickCount == 2;
            }

            private float ValuesPerPixel()
            {
                return kPixelRange / (MaxValue() - MinValue());
            }

            private KnobState KnobState()
            {
                return (KnobState)GUIUtility.GetStateObject(typeof(KnobState), id);
            }

            private void StartDraggingWithValue(float dragStartValue)
            {
                var state = KnobState();
                state.dragStartPos = MousePosition();
                state.dragStartValue = dragStartValue;
                state.isDragging = true;
            }

            private float OnMouseDown()
            {
                // if the click is outside this control, just bail out...
                if (
                    !position.Contains(CurrentEvent().mousePosition)
                    || KnobState().isEditing
                    || IsEmptyKnob()
                )
                    return currentValue;

                GUIUtility.hotControl = id;

                if (WasDoubleClick())
                {
                    KnobState().isEditing = true;
                }
                else
                {
                    // Record where we're dragging from, so the user can get back.
                    StartDraggingWithValue(ClampedCurrentValue());
                }

                CurrentEvent().Use();

                return currentValue;
            }

            private float OnMouseDrag()
            {
                if (GUIUtility.hotControl != id)
                    return currentValue;

                var state = KnobState();
                if (!state.isDragging)
                    return currentValue;

                GUI.changed = true;
                CurrentEvent().Use();

                // Recalculate the value from the mouse position. this has the side effect that values are relative to the
                // click point - no matter where inside the trough the original value was. Also means user can get back original value
                // if he drags back to start position.
                float deltaPos = state.dragStartPos - MousePosition();
                var newValue = state.dragStartValue + (deltaPos / ValuesPerPixel());
                if (snap > 0f)
                    newValue = Mathf.Round(newValue / snap) * snap;
                return Clamp(newValue);
            }

            private float OnMouseUp()
            {
                if (GUIUtility.hotControl == id)
                {
                    CurrentEvent().Use();
                    GUIUtility.hotControl = 0;
                    KnobState().isDragging = false;
                }
                return currentValue;
            }

            private void PrintValue()
            {
                if (!showValue)
                    return;

                string value = currentValue.ToString(
                    valueFormat,
                    CultureInfo.InvariantCulture.NumberFormat
                );
                var style = EditorStyles.centeredGreyMiniLabel;

                // Draw the label into the reserved labelPosition rect during repaint
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.Label(labelPosition, value + " " + unit, style);
                }

                // Allow clicking the label rect to enter edit mode
                if (
                    Event.current.type == EventType.MouseDown
                    && labelPosition.Contains(Event.current.mousePosition)
                )
                {
                    KnobState().isEditing = true;
                    Event.current.Use();
                }
            }

            private float DoKeyboardInput()
            {
                GUI.SetNextControlName("KnobInput");
                GUI.FocusControl("KnobInput");

                EditorGUI.BeginChangeCheck();
                Rect inputRect = labelPosition;

                //FIXME: Hack
                GUIStyle style = GUIStyle.none;
                style.normal.textColor = new Color(.703f, .703f, .703f, 1.0f);
                style.fontStyle = FontStyle.Normal;

                string newStr = EditorGUI.DelayedTextField(
                    inputRect,
                    currentValue.ToString(valueFormat, CultureInfo.InvariantCulture.NumberFormat),
                    style
                );
                if (EditorGUI.EndChangeCheck() && !String.IsNullOrEmpty(newStr))
                {
                    KnobState().isEditing = false;
                    if (float.TryParse(newStr, out float newValue) && newValue != currentValue)
                    {
                        if (snap > 0f)
                            newValue = Mathf.Round(newValue / snap) * snap;
                        return Clamp(newValue);
                    }
                }

                return currentValue;
            }

            private void DrawValueArc(float angle)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                // Simple knob: draw a wire circle and a line indicator from center to edge
                Vector2 center2 = new Vector2(
                    position.x + knobSize.x / 2f,
                    position.y + knobSize.y / 2f
                );
                float radius = Mathf.Min(knobSize.x, knobSize.y) / 2f - 4f;

                Handles.BeginGUI();
                Color prev = Handles.color;

                // circle outline
                Handles.color = backgroundColor * (GUI.enabled ? 1f : 0.5f);
                Handles.DrawWireDisc(
                    new Vector3(center2.x, center2.y, 0f),
                    Vector3.forward,
                    radius
                );

                // indicator line
                float percent = GetCurrentValuePercent();
                // Map percent into the configured sweep centered on neutralAngle.
                // Sweep is neutralAngle - sweepDegrees/2 .. neutralAngle + sweepDegrees/2
                float angDeg = Mathf.Lerp(
                    neutralAngle - sweepDegrees / 2f,
                    neutralAngle + sweepDegrees / 2f,
                    percent
                );
                float angRad = angDeg * Mathf.Deg2Rad;
                Vector2 end = center2 + new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad)) * radius;
                Handles.color = activeColor * (GUI.enabled ? 1f : 0.5f);
                Handles.DrawAAPolyLine(3f, (Vector3)center2, (Vector3)end);

                // neutral tick mark (outside the circle) at configured neutral angle
                float neutralRad = neutralAngle * Mathf.Deg2Rad;
                Vector2 tickStart =
                    center2
                    + new Vector2(Mathf.Cos(neutralRad), Mathf.Sin(neutralRad)) * (radius + 4f);
                Vector2 tickEnd =
                    center2
                    + new Vector2(Mathf.Cos(neutralRad), Mathf.Sin(neutralRad)) * (radius + 10f);
                Handles.color = Color.grey * (GUI.enabled ? 1f : 0.5f);
                Handles.DrawLine(
                    new Vector3(tickStart.x, tickStart.y, 0f),
                    new Vector3(tickEnd.x, tickEnd.y, 0f)
                );

                Handles.color = prev;
                Handles.EndGUI();
            }

            private float OnRepaint()
            {
                DrawValueArc(GetCurrentValuePercent() * Mathf.PI * (3.0f / 2.0f));

                if (KnobState().isEditing)
                    return DoKeyboardInput();

                if (showValue)
                    PrintValue();

                return currentValue;
            }
        }

        // Show text, fixed Size input
        internal static float Knob(
            Rect position,
            Rect labelPosition,
            Vector2 knobSize,
            float currentValue,
            float start,
            float end,
            string unit,
            Color backgroundColor,
            Color activeColor,
            bool showValue,
            string valueFormat,
            float snap,
            float neutralAngle,
            float sweepDegrees,
            int id
        )
        {
            return new KnobContext(
                position,
                labelPosition,
                knobSize,
                currentValue,
                start,
                end,
                unit,
                backgroundColor,
                activeColor,
                showValue,
                valueFormat,
                snap,
                neutralAngle,
                sweepDegrees,
                id
            ).Handle();
        }

        internal static float OffsetKnob(
            Rect position,
            float currentValue,
            float start,
            float end,
            float median,
            string unit,
            Color backgroundColor,
            Color activeColor,
            GUIStyle knob,
            int id
        )
        {
            ///@TODO Implement
            return 0f;
        }
    }
}
