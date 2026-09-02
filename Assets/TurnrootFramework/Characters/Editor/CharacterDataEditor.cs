using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components;
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
        private bool _showRecruitment = false;

        private bool _showBaseStats = false;
        private bool _showGrowthRates = false;
        private bool _showExpRanks = false;

        private bool _showIdentity = true;
        private bool _showDemographics = true;
        private bool _showPortraits = true;
        private bool _showVisualModel = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            _personalGrowthRates =
                serializedObject.FindProperty("PersonalGrowthRates")
                ?? serializedObject.FindProperty("<PersonalGrowthRates>k__BackingField");
            PopulateExperienceRanksIfEmpty();
            EnsureGrowthRatesHaveAllStats();
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

        private void EnsureGrowthRatesHaveAllStats()
        {
            var grProp =
                serializedObject.FindProperty("PersonalGrowthRates")
                ?? serializedObject.FindProperty("<PersonalGrowthRates>k__BackingField");
            if (grProp == null || !grProp.isArray)
            {
                return;
            }

            // collect keys already present so we don't duplicate
            var existingKeys = new HashSet<string>();
            for (int i = 0; i < grProp.arraySize; i++)
            {
                var el = grProp.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    continue;
                }

                var isB = el.FindPropertyRelative("isBounded");
                var unb = el.FindPropertyRelative("unboundedStatType");
                var bnd = el.FindPropertyRelative("boundedStatType");
                if (isB == null || unb == null || bnd == null)
                {
                    continue;
                }

                // use intValue (raw enum integer) as the key to handle non-contiguous enum values
                string key = isB.boolValue ? "B" + bnd.intValue : "U" + unb.intValue;
                existingKeys.Add(key);
            }

            bool modified = false;

            // Use GetDefaultUnboundedStatTypes() so that optional stats (Authority, Luck)
            // are only added when enabled in GameplayGeneralSettings.
            // Movement and CriticalAvoidance are calculated, not set directly — skip them.
            var gs = GameSettings.GameplayGeneralSettings.Instance;
            UnboundedStatType[] statTypes;
            if (gs != null)
            {
                statTypes = gs.GetDefaultUnboundedStatTypes();
            }
            else
            {
                statTypes = (UnboundedStatType[])System.Enum.GetValues(typeof(UnboundedStatType));
            }

            foreach (UnboundedStatType statType in statTypes)
            {
                if (
                    statType == UnboundedStatType.Movement
                    || statType == UnboundedStatType.CriticalAvoidance
                )
                {
                    continue;
                }

                string key = "U" + (int)statType;
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                grProp.InsertArrayElementAtIndex(grProp.arraySize);
                var el = grProp.GetArrayElementAtIndex(grProp.arraySize - 1);
                el.FindPropertyRelative("isBounded").boolValue = false;
                // use intValue to correctly handle non-contiguous enum values (e.g. Authority=11, CriticalAvoidance=12)
                el.FindPropertyRelative("unboundedStatType").intValue = (int)statType;
                el.FindPropertyRelative("boundedStatType").intValue = 0;
                el.FindPropertyRelative("value").floatValue = 0f;
                existingKeys.Add(key);
                modified = true;
            }

            // ensure Health (bounded) has an entry
            string healthKey = "B" + (int)BoundedStatType.Health;
            if (!existingKeys.Contains(healthKey))
            {
                grProp.InsertArrayElementAtIndex(grProp.arraySize);
                var el = grProp.GetArrayElementAtIndex(grProp.arraySize - 1);
                el.FindPropertyRelative("isBounded").boolValue = true;
                el.FindPropertyRelative("boundedStatType").intValue = (int)BoundedStatType.Health;
                el.FindPropertyRelative("unboundedStatType").intValue = 0;
                el.FindPropertyRelative("value").floatValue = 0f;
                modified = true;
            }

            if (modified)
            {
                serializedObject.ApplyModifiedProperties();
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
            _personalGrowthRates =
                serializedObject.FindProperty("PersonalGrowthRates")
                ?? serializedObject.FindProperty("<PersonalGrowthRates>k__BackingField");
            ValidateGrowthProperty();

            // Apply sanitize/validate changes NOW before base.OnInspectorGUI(), which calls
            // serializedObject.Update() internally and would discard any uncommitted changes.
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }

            // When the character is an NPC, only show identity + demographics fields.
            var whichProp = FindAutoProperty("Which");
            if (whichProp != null)
            {
                EditorGUILayout.PropertyField(whichProp);
            }
            else
            {
                EditorGUILayout.LabelField("Unable to find 'Which' property");
            }

            // CharacterWhich is a serializable class (string-backed), not an enum.
            // Use the internal value to determine if this is an NPC.
            var whichValueProp = whichProp?.FindPropertyRelative("_value");
            bool isNpc =
                whichValueProp != null
                && whichValueProp.stringValue == Turnroot.Characters.Components.CharacterWhich.NPC;
            if (isNpc)
            {
                _showIdentity = EditorGUILayout.Foldout(_showIdentity, "Identity");
                if (_showIdentity)
                {
                    DrawAutoPropertyField("DisplayName");
                    DrawAutoPropertyField("FullName");
                    DrawAutoPropertyField("Team");
                }

                _showDemographics = EditorGUILayout.Foldout(_showDemographics, "Demographics");
                if (_showDemographics)
                {
                    DrawAutoPropertyField("CharacterPronouns");
                    DrawAutoPropertyField("BirthdayDay");
                    DrawAutoPropertyField("BirthdayMonth");
                    DrawAutoPropertyField("Species");
                }

                _showPortraits = EditorGUILayout.Foldout(_showPortraits, "Portraits");
                if (_showPortraits)
                {
                    DrawAutoPropertyField("Portraits");
                }

                _showVisualModel = EditorGUILayout.Foldout(_showVisualModel, "Visual Model");
                if (_showVisualModel)
                {
                    DrawAutoPropertyField("BadgeText");
                    DrawAutoPropertyField("BadgeIcon");
                    DrawAutoPropertyField("Blendshapes");
                    DrawAutoPropertyField("SkinColor");
                    DrawAutoPropertyField("AccentColor1");
                    DrawAutoPropertyField("AccentColor2");
                    DrawAutoPropertyField("AccentColor3");
                    DrawAutoPropertyField("HeadAndHandsPrefab");
                    DrawAutoPropertyField("HairPrefab");
                    DrawAutoPropertyField("NonBattleOutfitPrefab");
                }

                serializedObject.ApplyModifiedProperties();
                return;
            }

            // draw the default inspector first; we'll merge our custom sections below
            base.OnInspectorGUI();

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

            // Recruitment section - only shown when IsRecruitable is true
            var isRecruitableProp = FindAutoProperty("IsRecruitable");
            if (isRecruitableProp != null && isRecruitableProp.boolValue)
            {
                EditorGUILayout.Space();
                _showRecruitment = EditorGUILayout.Foldout(_showRecruitment, "Recruitment", true);
                if (_showRecruitment)
                {
                    DrawRecruitmentSection();
                }
                if (serializedObject.hasModifiedProperties)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();
            _showBaseStats = EditorGUILayout.Foldout(_showBaseStats, "Base Stats", true);
            if (_showBaseStats)
            {
                DrawBaseStatsSection();
            }
            // Apply changes made to base stats immediately
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
            }

            _showGrowthRates = EditorGUILayout.Foldout(_showGrowthRates, "Growth Rates", true);
            if (_showGrowthRates)
            {
                DrawGrowthRatesCustom();
            }
            // Apply changes made to growth rates immediately
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
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

        private SerializedProperty FindAutoProperty(string name)
        {
            // Auto-properties with [field: SerializeField] are stored in the backing field.
            // Unity may serialize either the field name or the property name depending on the compiler.
            return serializedObject.FindProperty(name)
                ?? serializedObject.FindProperty($"<{name}>k__BackingField");
        }

        private void DrawAutoPropertyField(string name)
        {
            var prop = FindAutoProperty(name);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
            else
            {
                EditorGUILayout.LabelField($"Missing serialized property: {name}");
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
            var gr =
                so.FindProperty("PersonalGrowthRates")
                ?? so.FindProperty("<PersonalGrowthRates>k__BackingField");
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
                    string key = ib ? "B" + bnd.intValue : "U" + unb.intValue;
                    if (seenKeys.Contains(key))
                    {
                        continue; // skip duplicate entries
                    }

                    seenKeys.Add(key);

                    if (ib)
                    {
                        temp.Add(new UnboundedStatModifier((BoundedStatType)bnd.intValue, v));
                    }
                    else
                    {
                        temp.Add(new UnboundedStatModifier((UnboundedStatType)unb.intValue, v));
                    }
                }

                // write back cleaned list
                gr.arraySize = temp.Count;
                for (int i = 0; i < temp.Count; i++)
                {
                    var el = gr.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("isBounded").boolValue = temp[i].isBounded;
                    el.FindPropertyRelative("unboundedStatType").intValue = (int)
                        temp[i].unboundedStatType;
                    el.FindPropertyRelative("boundedStatType").intValue = (int)
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

            // first pass: remove malformed entries (missing fields/null)
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

                // movement and critical avoidance are calculated, not per-character; remove any stale entries
                if (!isBProp.boolValue)
                {
                    var stat = (UnboundedStatType)unbProp.intValue;
                    if (
                        stat == UnboundedStatType.Movement
                        || stat == UnboundedStatType.CriticalAvoidance
                    )
                    {
                        _personalGrowthRates.DeleteArrayElementAtIndex(i);
                        continue;
                    }
                }
            }

            // second pass: deduplicate entries by stat type (keep first occurrence, remove later duplicates)
            var seen = new HashSet<string>();
            for (int i = _personalGrowthRates.arraySize - 1; i >= 0; i--)
            {
                var el = _personalGrowthRates.GetArrayElementAtIndex(i);
                if (el == null)
                {
                    continue;
                }
                var isB2 = el.FindPropertyRelative("isBounded");
                var unb2 = el.FindPropertyRelative("unboundedStatType");
                var bnd2 = el.FindPropertyRelative("boundedStatType");
                if (isB2 == null || unb2 == null || bnd2 == null)
                {
                    continue;
                }
                string key = isB2.boolValue ? "B" + bnd2.intValue : "U" + unb2.intValue;
                if (seen.Contains(key))
                {
                    _personalGrowthRates.DeleteArrayElementAtIndex(i);
                }
                else
                {
                    seen.Add(key);
                }
            }
        }

        private void DrawBaseStatsSection()
        {
            var cd = serializedObject.targetObject as CharacterData;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);

            // --- Bounded Stats Section (Health, Stamina, etc.) ---
            var bndProp =
                serializedObject.FindProperty("BoundedStats")
                ?? serializedObject.FindProperty("<BoundedStats>k__BackingField");
            if (bndProp != null && bndProp.isArray)
            {
                EditorGUILayout.LabelField("Bounded Stats", EditorStyles.miniBoldLabel);
                if (bndProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No bounded stats configured.", MessageType.Info);
                }
                else
                {
                    for (int j = 0; j < bndProp.arraySize; j++)
                    {
                        var elem = bndProp.GetArrayElementAtIndex(j);
                        if (elem == null)
                        {
                            continue;
                        }

                        var typeProp = elem.FindPropertyRelative("_statType");
                        var curProp = elem.FindPropertyRelative("_current");
                        var maxProp = elem.FindPropertyRelative("_max");

                        string label =
                            typeProp != null
                                ? ((BoundedStatType)typeProp.enumValueIndex).ToString()
                                : $"Element {j}";

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(label, GUILayout.Width(100));

                        if (curProp != null)
                        {
                            EditorGUILayout.LabelField("Current:", GUILayout.Width(55));
                            EditorGUILayout.PropertyField(
                                curProp,
                                GUIContent.none,
                                GUILayout.Width(50)
                            );
                        }

                        if (maxProp != null)
                        {
                            EditorGUILayout.LabelField("Max:", GUILayout.Width(35));
                            EditorGUILayout.PropertyField(
                                maxProp,
                                GUIContent.none,
                                GUILayout.Width(50)
                            );
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.Space();
            }

            // --- Unbounded Stats Section with Meter ---
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

                EditorGUILayout.LabelField("Unbounded Stats", EditorStyles.miniBoldLabel);
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
                        if (typeProp != null)
                        {
                            var statType = (UnboundedStatType)typeProp.intValue;
                            // CriticalAvoidance is calculated, not set directly
                            if (statType == UnboundedStatType.CriticalAvoidance)
                            {
                                continue;
                            }
                        }

                        var curProp = elem.FindPropertyRelative("_current");
                        string label =
                            typeProp != null
                                ? ((UnboundedStatType)typeProp.intValue).ToString()
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

            // ensure property reference is current (OnInspectorGUI already called Update())
            if (_personalGrowthRates == null)
            {
                _personalGrowthRates =
                    serializedObject.FindProperty("PersonalGrowthRates")
                    ?? serializedObject.FindProperty("<PersonalGrowthRates>k__BackingField");
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
                    var idx = el.FindPropertyRelative("unboundedStatType").intValue;
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

        private void DrawRecruitmentSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recruitment", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Recruitment checks run in this order:\n"
                    + "1. If 'Will Join If Ally Is Already Recruited' is set, the character joins immediately when that ally is recruited — all other checks are bypassed.\n"
                    + "2. If 'Avatar Must Have Minimum Experience Levels To Recruit', the avatar's proficiency ranks are verified. If this check fails and 'Support Can Compensate For Missing Experience Levels' is enabled, a sufficiently high support rank with this character can substitute for the missing experience.\n"
                    + "3. Independently, if 'Recruit Requires Min Support Level', the avatar must have at least the specified support rank with this character.",
                MessageType.Info
            );

            ValidateRecruitmentSection();

            DrawAutoPropertyField("RecruitRequiresMinSupportLevel");

            var requiresMinSupportProp = FindAutoProperty("RecruitRequiresMinSupportLevel");
            if (requiresMinSupportProp != null && requiresMinSupportProp.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawAutoPropertyField("RecruitSupportRelationshipMinRank");
                EditorGUI.indentLevel--;
            }

            DrawAutoPropertyField("AvatarMustHaveMinimumExperienceLevelsToRecruit");

            var avatarMinLevelsProp = serializedObject.FindProperty(
                "AvatarMustHaveMinimumExperienceLevelsToRecruit"
            );
            if (avatarMinLevelsProp != null && avatarMinLevelsProp.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawExperienceRankListWithTypeDropdown("AvatarMinimumExperienceRanksToRecruit");
                EditorGUI.indentLevel--;
            }

            DrawAutoPropertyField("SupportCanCompensateForMissingExperienceLevels");

            var compensateProp = serializedObject.FindProperty(
                "SupportCanCompensateForMissingExperienceLevels"
            );
            if (compensateProp != null && compensateProp.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawAutoPropertyField("RecruitCompensationSupportLevel");
                EditorGUI.indentLevel--;

                // Validate: compensation level must be strictly higher than the base min support level
                var reqMinSupportProp = FindAutoProperty("RecruitRequiresMinSupportLevel");
                if (reqMinSupportProp != null && reqMinSupportProp.boolValue)
                {
                    var minRankProp = FindAutoProperty("RecruitSupportRelationshipMinRank");
                    var compLevelProp = FindAutoProperty("RecruitCompensationSupportLevel");
                    if (minRankProp != null && compLevelProp != null)
                    {
                        var minValueProp = minRankProp.FindPropertyRelative("_value");
                        var compValueProp = compLevelProp.FindPropertyRelative("_value");
                        if (minValueProp != null && compValueProp != null)
                        {
                            int minRank = GetRankNumericValue(minValueProp.stringValue);
                            int compRank = GetRankNumericValue(compValueProp.stringValue);
                            if (compRank <= minRank)
                            {
                                EditorGUILayout.HelpBox(
                                    "Compensation Support Level ("
                                        + compValueProp.stringValue
                                        + ") must be strictly higher than the Minimum Recruit Support Level ("
                                        + minValueProp.stringValue
                                        + "). Otherwise the avatar will already meet the base requirement before reaching the compensation threshold.",
                                    MessageType.Warning
                                );
                            }
                        }
                    }
                }
            }

            DrawAutoPropertyField("WillJoinIfAllyIsAlreadyRecruited");

            var willJoinProp = serializedObject.FindProperty("WillJoinIfAllyIsAlreadyRecruited");
            if (willJoinProp != null && willJoinProp.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawAutoPropertyField("SpecificAllyRequiredForRecruitment");
                EditorGUI.indentLevel--;
            }
        }

        private static int GetRankNumericValue(string rankLetter) =>
            rankLetter switch
            {
                "S" => 5,
                "A" => 4,
                "B" => 3,
                "C" => 2,
                "D" => 1,
                _ => 0,
            };

        private void ValidateRecruitmentSection()
        {
            var compensateProp = serializedObject.FindProperty(
                "SupportCanCompensateForMissingExperienceLevels"
            );
            var avatarMinLevelsProp = serializedObject.FindProperty(
                "AvatarMustHaveMinimumExperienceLevelsToRecruit"
            );
            var willJoinProp = serializedObject.FindProperty("WillJoinIfAllyIsAlreadyRecruited");
            var specificAllyProp = serializedObject.FindProperty(
                "SpecificAllyRequiredForRecruitment"
            );

            if (
                compensateProp != null
                && compensateProp.boolValue
                && avatarMinLevelsProp != null
                && !avatarMinLevelsProp.boolValue
            )
            {
                EditorGUILayout.HelpBox(
                    "'Support Can Compensate For Missing Experience Levels' is enabled, but 'Avatar Must Have Minimum Experience Levels To Recruit' is not set. The compensation will have no effect since there is nothing to compensate for.",
                    MessageType.Warning
                );
            }

            if (
                willJoinProp != null
                && willJoinProp.boolValue
                && specificAllyProp != null
                && specificAllyProp.objectReferenceValue == null
            )
            {
                EditorGUILayout.HelpBox(
                    "'Will Join If Ally Is Already Recruited' is enabled, but no Specific Ally Required For Recruitment is assigned. This may cause a null reference at runtime.",
                    MessageType.Error
                );
            }
        }

        private void DrawExperienceRankListWithTypeDropdown(string fieldName)
        {
            var listProp =
                serializedObject.FindProperty(fieldName)
                ?? serializedObject.FindProperty($"<{fieldName}>k__BackingField");
            if (listProp == null || !listProp.isArray)
            {
                EditorGUILayout.LabelField($"Missing property: {fieldName}");
                return;
            }

            var gs = GameSettings.GameplayGeneralSettings.Instance;
            var allTypes = gs != null ? gs.GetAllExperienceTypes() : null;
            string[] typeNames =
                allTypes != null ? System.Array.ConvertAll(allTypes, t => t.Name) : new string[0];

            EditorGUILayout.LabelField("Minimum Experience Ranks", EditorStyles.miniBoldLabel);

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var elem = listProp.GetArrayElementAtIndex(i);
                if (elem == null)
                {
                    continue;
                }

                var idProp = elem.FindPropertyRelative("_experienceTypeId");
                var rankProp = elem.FindPropertyRelative("_rank");

                EditorGUILayout.BeginHorizontal();

                if (idProp != null && typeNames.Length > 0)
                {
                    int currentIndex = System.Array.IndexOf(typeNames, idProp.stringValue);
                    if (currentIndex < 0)
                    {
                        currentIndex = 0;
                    }

                    int newIndex = EditorGUILayout.Popup(
                        currentIndex,
                        typeNames,
                        GUILayout.Width(140)
                    );
                    if (newIndex != currentIndex)
                    {
                        idProp.stringValue = typeNames[newIndex];
                    }
                }
                else if (idProp != null)
                {
                    EditorGUILayout.PropertyField(idProp, GUIContent.none, GUILayout.Width(140));
                }

                if (rankProp != null)
                {
                    EditorGUILayout.PropertyField(rankProp, GUIContent.none);
                }

                if (GUILayout.Button("-", GUILayout.Width(22)))
                {
                    listProp.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Rank Requirement", GUILayout.Width(160)))
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                var newElem = listProp.GetArrayElementAtIndex(listProp.arraySize - 1);
                if (newElem != null)
                {
                    var idProp = newElem.FindPropertyRelative("_experienceTypeId");
                    if (idProp != null && typeNames.Length > 0)
                    {
                        idProp.stringValue = typeNames[0];
                    }
                }
            }
        }
    }
}
