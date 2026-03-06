using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Behavior;
using Turnroot.Characters.Stats;
using UnityEditor;
using UnityEngine;
using static Turnroot.Characters.Components.Behavior.CharacterBehavior;

namespace Turnroot.Characters.Editor
{
    [CustomEditor(typeof(CharacterData))]
    public class CharacterDataEditor : NaughtyAttributes.Editor.NaughtyInspector
    {
        private SerializedProperty _personalGrowthRates;

        private bool _behaviorFoldout = false;

        private bool _showBaseStats = false;
        private bool _showGrowthRates = false;
        private bool _showExpRanks = false;

        protected override void OnEnable()
        {
            base.OnEnable();
            _personalGrowthRates = serializedObject.FindProperty("PersonalGrowthRates");
            PopulateExperienceRanksIfEmpty();
        }

        private void PopulateExperienceRanksIfEmpty()
        {
            var expProp =
                serializedObject.FindProperty("ExperienceRanks")
                ?? serializedObject.FindProperty("<ExperienceRanks>k__BackingField");
            if (expProp != null && expProp.isArray && expProp.arraySize == 0)
            {
                var gs = GameSettings.GameplayGeneralSettings.Instance;
                if (gs != null)
                {
                    var types = gs.GetAllExperienceTypes();
                    foreach (var et in types)
                    {
                        expProp.InsertArrayElementAtIndex(expProp.arraySize);
                        var elem = expProp.GetArrayElementAtIndex(expProp.arraySize - 1);
                        if (elem != null)
                        {
                            var idProp = elem.FindPropertyRelative("_experienceTypeId");
                            var rankProp = elem.FindPropertyRelative("_rank");
                            if (idProp != null)
                            {
                                idProp.stringValue = et.Name;
                            }

                            if (rankProp != null)
                            {
                                var valueProp = rankProp.FindPropertyRelative("Value");
                                if (valueProp != null)
                                {
                                    valueProp.intValue = 0;
                                }
                            }
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "This is pre-runtime data. Use this editor to define the character's base stats, skills, inventory, and relationships - anything that should be in place before the game starts.",
                MessageType.Info
            );

            // remove null entries; we assume the asset is correct and don't attempt
            // to deduplicate automatically anymore.
            SanitizeNullStats(serializedObject);

            // refresh before validation/drawing since sanitize may modify array
            _personalGrowthRates = serializedObject.FindProperty("PersonalGrowthRates");
            ValidateGrowthProperty();

            // draw the default inspector first; we'll merge our custom sections below
            base.OnInspectorGUI();

            serializedObject.Update();
            var behaviorProp =
                serializedObject.FindProperty("BehaviorSettings")
                ?? serializedObject.FindProperty("<BehaviorSettings>k__BackingField");
            EditorGUILayout.Space();
            _behaviorFoldout = EditorGUILayout.Foldout(_behaviorFoldout, "Behavior", false);
            if (_behaviorFoldout && behaviorProp != null)
            {
                float h = GetBehaviorPropertyHeight(behaviorProp);
                Rect rect = EditorGUILayout.GetControlRect(false, h, GUILayout.ExpandWidth(true));
                DrawBehaviorProperty(rect, behaviorProp);
            }
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            _showBaseStats = EditorGUILayout.Foldout(_showBaseStats, "Base Stats", true);
            if (_showBaseStats)
            {
                DrawBaseStatsSection();
            }

            _showGrowthRates = EditorGUILayout.Foldout(_showGrowthRates, "Growth Rates", true);
            if (_showGrowthRates)
            {
                DrawGrowthRatesCustom();
            }

            // experience ranks (auto‑populated)
            EditorGUILayout.Space();
            _showExpRanks = EditorGUILayout.Foldout(_showExpRanks, "Experience Ranks", true);
            if (_showExpRanks)
            {
                var expProp =
                    serializedObject.FindProperty("ExperienceRanks")
                    ?? serializedObject.FindProperty("<ExperienceRanks>k__BackingField");
                // if list exists but is empty, build default entries now using gameplay settings
                if (expProp != null && expProp.isArray && expProp.arraySize == 0)
                {
                    var gs = GameSettings.GameplayGeneralSettings.Instance;
                    if (gs != null)
                    {
                        var types = gs.GetAllExperienceTypes();
                        foreach (var et in types)
                        {
                            expProp.InsertArrayElementAtIndex(expProp.arraySize);
                            var elem = expProp.GetArrayElementAtIndex(expProp.arraySize - 1);
                            if (elem != null)
                            {
                                var idProp = elem.FindPropertyRelative("_experienceTypeId");
                                var rankProp = elem.FindPropertyRelative("_rank");
                                if (idProp != null)
                                {
                                    idProp.stringValue = et.Name;
                                }

                                if (rankProp != null)
                                {
                                    rankProp.FindPropertyRelative("Value").intValue = 0;
                                }
                            }
                        }
                        serializedObject.ApplyModifiedProperties();
                    }
                }
                EditorGUILayout.LabelField("Experience Ranks", EditorStyles.boldLabel);
                if (expProp != null && expProp.isArray)
                {
                    if (expProp.arraySize == 0)
                    {
                        EditorGUILayout.LabelField("<none configured>", EditorStyles.miniLabel);
                    }
                    else
                    {
                        for (int i = 0; i < expProp.arraySize; i++)
                        {
                            var elem = expProp.GetArrayElementAtIndex(i);
                            if (elem == null)
                            {
                                continue;
                            }

                            var idProp = elem.FindPropertyRelative("_experienceTypeId");
                            var rankProp = elem.FindPropertyRelative("_rank");
                            EditorGUILayout.BeginHorizontal();
                            // show type as plain text, stripping any namespace suffix in parentheses
                            if (idProp != null)
                            {
                                string raw = idProp.stringValue;
                                string display = raw;
                                int idx = raw.IndexOf('(');
                                if (idx > 0)
                                {
                                    display = raw.Substring(0, idx).Trim();
                                }

                                EditorGUILayout.LabelField(display, GUILayout.Width(120));
                            }
                            if (rankProp != null)
                            {
                                EditorGUILayout.PropertyField(rankProp, GUIContent.none);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                // level‑up tester button
                EditorGUILayout.Space();
                if (GUILayout.Button("Open Level Up Tester"))
                {
                    var data = target as CharacterData;
                    if (data != null)
                    {
                        CharacterLevelUpTesterWindow.Show(data);
                    }
                }

                // don't mark dirty here; we'll do a final apply/dirtify after the foldouts
                serializedObject.ApplyModifiedProperties();
            }

            // final flush for any changes made to base stats or growth rates
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }

        private static void SanitizeNullStats(SerializedObject so)
        {
            if (so == null)
            {
                return;
            }

            string[] statProps = { "BoundedStats", "UnboundedStats" };
            foreach (var propName in statProps)
            {
                var arr = so.FindProperty(propName);
                if (arr != null && arr.isArray)
                {
                    for (int i = arr.arraySize - 1; i >= 0; i--)
                    {
                        var el = arr.GetArrayElementAtIndex(i);
                        if (el != null && el.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (el.objectReferenceValue == null)
                            {
                                arr.DeleteArrayElementAtIndex(i);
                            }
                        }
                    }
                }
            }

            // sanitize growth rates entries (struct migrations can leave invalid elements)
            var gr = so.FindProperty("PersonalGrowthRates");
            if (gr != null && gr.isArray)
            {
                // rebuild into temp list to ensure each element has all fields
                var temp = new List<UnboundedStatModifier>();
                var seenKeys = new HashSet<string>();
                for (int i = 0; i < gr.arraySize; i++)
                {
                    var el = gr.GetArrayElementAtIndex(i);
                    if (el == null)
                    {
                        continue;
                    }

                    var isB = el.FindPropertyRelative("isBounded");
                    var unb = el.FindPropertyRelative("unboundedStatType");
                    var bnd = el.FindPropertyRelative("boundedStatType");
                    var val = el.FindPropertyRelative("value");
                    if (isB == null || unb == null || bnd == null || val == null)
                    {
                        continue;
                    }

                    bool ib = isB.boolValue;
                    float v = val.floatValue;
                    string key = ib ? "B" + bnd.enumValueIndex : "U" + unb.enumValueIndex;
                    if (seenKeys.Contains(key))
                    {
                        continue; // skip duplicate entries
                    }

                    seenKeys.Add(key);

                    if (ib)
                    {
                        temp.Add(new UnboundedStatModifier((BoundedStatType)bnd.enumValueIndex, v));
                    }
                    else
                    {
                        temp.Add(
                            new UnboundedStatModifier((UnboundedStatType)unb.enumValueIndex, v)
                        );
                    }
                }

                // write back cleaned list
                gr.arraySize = temp.Count;
                for (int i = 0; i < temp.Count; i++)
                {
                    var el = gr.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("isBounded").boolValue = temp[i].isBounded;
                    el.FindPropertyRelative("unboundedStatType").enumValueIndex = (int)
                        temp[i].unboundedStatType;
                    el.FindPropertyRelative("boundedStatType").enumValueIndex = (int)
                        temp[i].boundedStatType;
                    el.FindPropertyRelative("value").floatValue = temp[i].value;
                }
            }
        }

        private void ValidateGrowthProperty()
        {
            if (_personalGrowthRates == null)
            {
                return;
            }

            // first pass: remove malformed entries (missing fields/null) and movement
            for (int i = _personalGrowthRates.arraySize - 1; i >= 0; i--)
            {
                var el = _personalGrowthRates.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    _personalGrowthRates.DeleteArrayElementAtIndex(i);
                    continue;
                }
                var isBProp = el.FindPropertyRelative("isBounded");
                var unbProp = el.FindPropertyRelative("unboundedStatType");
                var bndProp = el.FindPropertyRelative("boundedStatType");
                var valProp = el.FindPropertyRelative("value");
                if (isBProp == null || unbProp == null || bndProp == null || valProp == null)
                {
                    _personalGrowthRates.DeleteArrayElementAtIndex(i);
                    continue;
                }

                // automatically drop movement entries; they are managed by GameplayGeneralSettings
                if (!isBProp.boolValue)
                {
                    var stat = (UnboundedStatType)unbProp.enumValueIndex;
                    if (stat == UnboundedStatType.Movement)
                    {
                        _personalGrowthRates.DeleteArrayElementAtIndex(i);
                        continue;
                    }
                }
            }

            // second pass: deduplicate entries by stat type (keep first occurrence)
            var seen = new HashSet<string>();
            for (int i = _personalGrowthRates.arraySize - 1; i >= 0; i--)
            {
                var el = _personalGrowthRates.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    continue;
                }
            }
        }

        private void DrawBaseStatsSection()
        {
            // meter showing template's unbounded stat total (and editable list)
            var cd = serializedObject.targetObject as CharacterData;
            if (cd != null && cd.UnboundedStats != null)
            {
                float curTotal = 0f;
                foreach (var unb in cd.UnboundedStats)
                {
                    if (unb == null)
                    {
                        continue;
                    }

                    if (
                        unb.StatType == UnboundedStatType.Movement
                        || unb.StatType == UnboundedStatType.Charm
                    )
                    {
                        continue;
                    }

                    curTotal += unb.Current;
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
                Rect barRect2 = EditorGUILayout.GetControlRect(
                    false,
                    20,
                    GUILayout.ExpandWidth(true)
                );
                float width2 = barRect2.width;
                // use checklist thresholds: total 10‑50, green=30‑40, yellow=20‑30 & 40‑50, orange=10‑20
                const float minT = 10f,
                    maxT = 50f;
                float norm2 = (curTotal - minT) / (maxT - minT);
                norm2 = Mathf.Clamp01(norm2);
                void DrawSeg2Norm(float aNorm, float bNorm, Color col)
                {
                    if (bNorm <= aNorm)
                    {
                        return;
                    }

                    Rect seg = new Rect(
                        barRect2.x + aNorm * width2,
                        barRect2.y,
                        (bNorm - aNorm) * width2,
                        barRect2.height
                    );
                    EditorGUI.DrawRect(seg, col);
                }
                // segments correspond to normalized boundaries 0,0.25,0.5,0.75,1
                DrawSeg2Norm(0.5f, 0.75f, Color.green);
                DrawSeg2Norm(0.25f, 0.5f, Color.yellow);
                DrawSeg2Norm(0.75f, 1f, Color.yellow);
                DrawSeg2Norm(0f, 0.25f, new Color(1f, 0.5f, 0f));
                // red outside handled by background or left unfilled
                float mx2 = barRect2.x + norm2 * width2;
                Handles.BeginGUI();
                Handles.color = Color.black;
                Vector3 top2 = new Vector3(mx2, barRect2.y);
                Vector3 bot2 = new Vector3(mx2, barRect2.y + barRect2.height);
                Handles.DrawAAPolyLine(5f, top2, bot2);
                // always draw white outlines at both sides for readability
                Handles.color = Color.white;
                float o = 2f;
                Handles.DrawAAPolyLine(2f, top2 + Vector3.left * o, bot2 + Vector3.left * o);
                Handles.DrawAAPolyLine(2f, top2 + Vector3.right * o, bot2 + Vector3.right * o);
                Handles.EndGUI();
                EditorGUILayout.HelpBox("Base stat total vs thresholds.", MessageType.Info);

                // manual list so we can show names and values instead of
                // generic "Element 0" entries.  Use the backing field if needed.
                var unbProp =
                    serializedObject.FindProperty("UnboundedStats")
                    ?? serializedObject.FindProperty("<UnboundedStats>k__BackingField");
                if (unbProp != null)
                {
                    for (int j = 0; j < unbProp.arraySize; j++)
                    {
                        var elem = unbProp.GetArrayElementAtIndex(j);
                        if (elem == null)
                        {
                            continue;
                        }

                        var typeProp = elem.FindPropertyRelative("_statType");
                        var curProp = elem.FindPropertyRelative("_current");
                        string label =
                            typeProp != null
                                ? ((UnboundedStatType)typeProp.enumValueIndex).ToString()
                                : $"Element {j}";
                        EditorGUILayout.BeginHorizontal();
                        if (typeProp != null)
                        {
                            EditorGUILayout.PropertyField(typeProp, GUIContent.none);
                        }

                        if (curProp != null)
                        {
                            EditorGUILayout.PropertyField(curProp, new GUIContent(label));
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(
                        "You can edit each unbounded stat's type and value above.",
                        MessageType.None
                    );
                }
            }
        }

        private void DrawGrowthRatesCustom()
        {
            // separator between default properties and custom meters
            EditorGUILayout.Space();
            Rect sepRect = EditorGUILayout.GetControlRect(false, 4, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(sepRect, new Color(1f, 0.4f, 0.4f));
            EditorGUILayout.Space();

            // make sure serialized data is current before grabbing the property
            serializedObject.Update();
            _personalGrowthRates = serializedObject.FindProperty("PersonalGrowthRates");
            if (_personalGrowthRates == null)
            {
                // sometimes auto-property serialisation uses the backing field name
                _personalGrowthRates = serializedObject.FindProperty(
                    "<PersonalGrowthRates>k__BackingField"
                );
            }
            if (_personalGrowthRates == null || !_personalGrowthRates.isArray)
            {
                Debug.Log("CharacterDataEditor: PersonalGrowthRates property null or not array");
                return;
            }

            if (_personalGrowthRates.arraySize == 0)
            {
                Debug.Log(
                    "CharacterDataEditor: PersonalGrowthRates arraySize 0 - nothing to meter"
                );
            }

            // compute total growth percent (exclude movement and charm only)
            float total = 0f;
            for (int i = 0; i < _personalGrowthRates.arraySize; i++)
            {
                var el = _personalGrowthRates.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    continue;
                }

                bool isB = el.FindPropertyRelative("isBounded").boolValue;
                if (!isB)
                {
                    var idx = el.FindPropertyRelative("unboundedStatType").enumValueIndex;
                    if (
                        idx == (int)UnboundedStatType.Movement
                        || idx == (int)UnboundedStatType.Charm
                    )
                    {
                        continue;
                    }
                }
                // bounded HP is now counted
                var valProp = el.FindPropertyRelative("value");
                if (valProp != null)
                {
                    total += valProp.floatValue;
                }
            }

            // draw segmented meter showing ranges: green centre, yellow, orange, red edges
            Rect barRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.ExpandWidth(true));
            float width = barRect.width;
            // compute normalized value from 310..530 (clamped but marker may lie outside visually)
            float norm = (total - 310f) / (530f - 310f);
            // color segments relative positions (all in normalized coordinates)
            float redOuter = Mathf.InverseLerp(0f, 310f, total);
            // we will paint sequentially
            // segment definitions in value space (all shifted +30):
            // red: 0-310 and >530, orange: 310-350 & 490-530,
            // yellow: 350-390 & 450-490, green:390-450
            void DrawSegment(float a, float b, Color col)
            {
                float xa = Mathf.Clamp01((a - 310f) / 220f);
                float xb = Mathf.Clamp01((b - 310f) / 220f);
                if (xb > xa)
                {
                    Rect seg = new Rect(
                        barRect.x + (xa * width),
                        barRect.y,
                        (xb - xa) * width,
                        barRect.height
                    );
                    EditorGUI.DrawRect(seg, col);
                }
            }
            DrawSegment(390f, 450f, Color.green);
            DrawSegment(350f, 390f, Color.yellow);
            DrawSegment(450f, 490f, Color.yellow);
            DrawSegment(310f, 350f, new Color(1f, 0.5f, 0f));
            DrawSegment(490f, 530f, new Color(1f, 0.5f, 0f));
            // red outer blocks
            DrawSegment(float.MinValue, 310f, Color.red);
            DrawSegment(530f, float.MaxValue, Color.red);
            // draw marker line at norm position
            float mx = barRect.x + (Mathf.Clamp01(norm) * width);
            Handles.BeginGUI();
            Vector3 top = new Vector3(mx, barRect.y);
            Vector3 bottom = new Vector3(mx, barRect.y + barRect.height);
            Handles.color = Color.black;
            // thicker needle for visibility
            Handles.DrawAAPolyLine(5f, top, bottom);
            // white outline on both sides for contrast
            Handles.color = Color.white;
            float off = 2f;
            Handles.DrawAAPolyLine(2f, top + (Vector3.left * off), bottom + (Vector3.left * off));
            Handles.DrawAAPolyLine(2f, top + (Vector3.right * off), bottom + (Vector3.right * off));
            Handles.EndGUI();
            EditorGUILayout.HelpBox(
                "Green = optimal range for normal units, yellow = very weak or very strong units. Orange is unbalanced"
                    + "Needle shows current total. Charm excluded.",
                MessageType.Info
            );

            // now render the growth rate list
            EditorGUILayout.LabelField("Personal Growth Rates", EditorStyles.boldLabel);
            for (int i = 0; i < _personalGrowthRates.arraySize; i++)
            {
                var el = _personalGrowthRates.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    continue;
                }

                var isB = el.FindPropertyRelative("isBounded");
                var unb = el.FindPropertyRelative("unboundedStatType");
                var bnd = el.FindPropertyRelative("boundedStatType");
                var val = el.FindPropertyRelative("value");
                if (isB == null || unb == null || bnd == null || val == null)
                {
                    continue;
                }

                string label = "";
                if (isB.boolValue)
                {
                    var idx = bnd.enumValueIndex;
                    if (idx >= 0 && idx < bnd.enumDisplayNames.Length)
                    {
                        label = bnd.enumDisplayNames[idx];
                    }
                }
                else
                {
                    var idx = unb.enumValueIndex;
                    if (idx >= 0 && idx < unb.enumDisplayNames.Length)
                    {
                        label = unb.enumDisplayNames[idx];
                    }
                }

                if (val != null)
                {
                    float current = val.floatValue;
                    float updated = EditorGUILayout.FloatField(label, current);
                    if (!Mathf.Approximately(updated, current))
                    {
                        val.floatValue = updated;
                    }
                }
            }
        }

        private void DrawBehaviorProperty(Rect position, SerializedProperty property)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            float buttonWidth = 85f;
            float buttonSpacing = 4f;
            float totalButtonWidth = (buttonWidth * 2) + buttonSpacing;
            float buttonStartX = position.x + position.width - totalButtonWidth;
            Rect presetRect = new Rect(
                position.x,
                position.y,
                position.width - totalButtonWidth - 4f,
                lineHeight
            );
            float y = position.y + lineHeight + 2;

            var presetProp = property.FindPropertyRelative("preset");
            string tooltip = GetPresetInfoBoxText(
                (CharacterBehaviorPresetEnum)presetProp.enumValueIndex
            );
            EditorGUI.PropertyField(
                presetRect,
                presetProp,
                new GUIContent("Behavior Preset", tooltip)
            );

            Rect applyPresetRect = new Rect(buttonStartX, position.y, buttonWidth, lineHeight);
            Rect jiggleButtonRect = new Rect(
                buttonStartX + buttonWidth + buttonSpacing,
                position.y,
                buttonWidth,
                lineHeight
            );
            if (GUI.Button(applyPresetRect, "Apply Preset"))
            {
                ApplyBehaviorPreset(property, presetProp);
                property.serializedObject.ApplyModifiedProperties();
            }
            if (GUI.Button(jiggleButtonRect, "Jiggle"))
            {
                var sliderProps = new[]
                {
                    "SoldierLoneWolf",
                    "MindlessCunning",
                    "SelfishSelfless",
                    "BrashWary",
                    "BloodthirstGreed",
                };
                foreach (var sliderProp in sliderProps)
                {
                    var prop = property.FindPropertyRelative(sliderProp);
                    if (prop != null)
                    {
                        var value = UnityEngine.Random.value;
                        prop.floatValue += Mathf.Lerp(-0.05f, .05f, value);
                    }
                }
                property.serializedObject.ApplyModifiedProperties();
            }
            y = position.y + lineHeight + 2;

            DrawBehaviorSlider(property, ref y, position, "SoldierLoneWolf", "Soldier/Lone Wolf");
            DrawBehaviorSlider(property, ref y, position, "MindlessCunning", "Mindless/Cunning");
            DrawBehaviorSlider(property, ref y, position, "SelfishSelfless", "Selfish/Selfless");
            DrawBehaviorSlider(property, ref y, position, "BrashWary", "Brash/Wary");
            DrawBehaviorSlider(property, ref y, position, "BloodthirstGreed", "Bloodthirst/Greed");

            var movementDisabledProp = property.FindPropertyRelative("MovementDisabled");
            if (movementDisabledProp != null)
            {
                Rect moveRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.PropertyField(
                    moveRect,
                    movementDisabledProp,
                    new GUIContent("Movement Disabled")
                );
                y += EditorGUIUtility.singleLineHeight + 2;
            }
            var attackDisabledProp = property.FindPropertyRelative("AttackDisabled");
            if (attackDisabledProp != null)
            {
                Rect attackRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                EditorGUI.PropertyField(
                    attackRect,
                    attackDisabledProp,
                    new GUIContent("Attack Disabled (will counterattack)")
                );
                y += EditorGUIUtility.singleLineHeight + 2;
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private float GetBehaviorPropertyHeight(SerializedProperty property)
        {
            // no longer need extra space for info box; keep fixed rows only
            float height = EditorGUIUtility.singleLineHeight + 2; // preset
            height += (EditorGUIUtility.singleLineHeight + 2) * 5; // sliders
            height += (EditorGUIUtility.singleLineHeight + 2) * 2; // movement/attack fields
            return height;
        }

        private void DrawBehaviorSlider(
            SerializedProperty property,
            ref float y,
            Rect position,
            string field,
            string label
        )
        {
            var prop = property.FindPropertyRelative(field);
            if (prop != null)
            {
                Rect sliderRect = new Rect(
                    position.x,
                    y,
                    position.width,
                    EditorGUIUtility.singleLineHeight
                );
                // Color blend: #006D77 (0) to #7E9624 (1)
                Color color0 = new Color32(0x00, 0x6D, 0x77, 0xFF);
                Color color1 = new Color32(0x7E, 0x96, 0x24, 0xFF);
                float t = Mathf.Clamp01(prop.floatValue);
                Color blended = Color.Lerp(color0, color1, t);
                EditorGUI.DrawRect(sliderRect, blended);
                EditorGUI.Slider(sliderRect, prop, 0f, 1f, label);
                y += EditorGUIUtility.singleLineHeight + 2;
            }
        }

        private string GetPresetInfoBoxText(CharacterBehaviorPresetEnum presetEnum)
        {
            return presetEnum switch
            {
                CharacterBehaviorPresetEnum.MindlessBerserker =>
                    "Mindlessly attacks nearby enemies without regard for self-preservation",
                CharacterBehaviorPresetEnum.CunningAssassin =>
                    "Cautiously works alone to eliminate strategic enemies.",
                CharacterBehaviorPresetEnum.GreedyCoward =>
                    "Avoids combat, prefers looting and running away.",
                CharacterBehaviorPresetEnum.LoyalGuardian =>
                    "Stays near to allies and keeps them safe, even above their own safety",
                CharacterBehaviorPresetEnum.WaryProtector =>
                    "Prioritizes defense of allies while avoiding unnecessary risks",
                CharacterBehaviorPresetEnum.VengefulWarrior =>
                    "Generally strategic, flies into a murderous rampage when allies are killed",
                CharacterBehaviorPresetEnum.RecklessDuelist =>
                    "Seeks out enemies without considering themselves or allies",
                CharacterBehaviorPresetEnum.BalancedVeteran =>
                    "A well-rounded fighter who balances risks and support",
                _ => string.Empty,
            };
        }

        private void ApplyBehaviorPreset(SerializedProperty property, SerializedProperty presetProp)
        {
            var presetEnum = (CharacterBehaviorPresetEnum)presetProp.enumValueIndex;
            CharacterBehavior presetValues = presetEnum switch
            {
                CharacterBehaviorPresetEnum.MindlessBerserker =>
                    CharacterBehaviorPreset.MindlessBerserker,
                CharacterBehaviorPresetEnum.CunningAssassin =>
                    CharacterBehaviorPreset.CunningAssassin,
                CharacterBehaviorPresetEnum.GreedyCoward => CharacterBehaviorPreset.GreedyCoward,
                CharacterBehaviorPresetEnum.LoyalGuardian => CharacterBehaviorPreset.LoyalGuardian,
                CharacterBehaviorPresetEnum.WaryProtector => CharacterBehaviorPreset.WaryProtector,
                CharacterBehaviorPresetEnum.VengefulWarrior =>
                    CharacterBehaviorPreset.VengefulWarrior,
                CharacterBehaviorPresetEnum.RecklessDuelist =>
                    CharacterBehaviorPreset.RecklessDuelist,
                CharacterBehaviorPresetEnum.BalancedVeteran =>
                    CharacterBehaviorPreset.BalancedVeteran,
                _ => default,
            };
            property.FindPropertyRelative("SoldierLoneWolf").floatValue =
                presetValues.SoldierLoneWolf;
            property.FindPropertyRelative("MindlessCunning").floatValue =
                presetValues.MindlessCunning;
            property.FindPropertyRelative("SelfishSelfless").floatValue =
                presetValues.SelfishSelfless;
            property.FindPropertyRelative("BrashWary").floatValue = presetValues.BrashWary;
            property.FindPropertyRelative("BloodthirstGreed").floatValue =
                presetValues.BloodthirstGreed;
        }
    }
}
