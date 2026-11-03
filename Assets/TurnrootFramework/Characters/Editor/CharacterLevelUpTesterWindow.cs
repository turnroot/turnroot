#if UNITY_EDITOR
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Characters.CharacterClass;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Characters.Editor
{
    /// <summary>
    /// Popup window that lets designers simulate leveling a CharacterData asset
    /// without mutating any real data.  It creates a temporary CharacterInstance
    /// for the template and runs <see cref="CharacterInstance.LevelUp" /> each
    /// time the user clicks the button.  Stats are shown alongside the base
    /// template values so deltas are visible.
    /// </summary>
    public class CharacterLevelUpTesterWindow : EditorWindow
    {
        private CharacterData _template;
        private CharacterInstance _instance;
        private CharacterClassData _classOverride; // optional class chosen in tester
        private int _levelUpCount;
        private int _goodLevelUps;
        private int _badLevelUps;

        public static void Show(CharacterData template)
        {
            var window = GetWindow<CharacterLevelUpTesterWindow>("Level Up Tester");
            window._template = template;
            window._classOverride = null;
            window.ResetSimulation();
            window.minSize = new Vector2(300, 300);
            window.Show();
        }

        private void ResetSimulation()
        {
            if (_template != null)
            {
                // if template is marked unique the registry would normally return the same
                // instance each time; clear it so our new simulation starts fresh.
                if (_template.IsUnique && _instance != null)
                {
                    UniqueInstanceRegistry.TryUnregister(_template, _instance);
                }

                // create a fresh instance (won't affect assets)
                _instance = CharacterInstance.Create(_template, useBattleModel: false);
                _levelUpCount = 0;
                _goodLevelUps = 0;
                _badLevelUps = 0;

                // apply class override if one is selected; this ensures the simulated
                // level ups will roll using growth rates coming from the chosen class.
                if (_classOverride != null && _instance != null)
                {
                    var res = _instance.ChangeClass(_classOverride, applyClassChangeBonuses: false);
                    if (!res.Success)
                    {
                        Debug.LogWarning(
                            $"LevelUp tester: failed to assign override class '{_classOverride.name}': {res.ErrorMessage}"
                        );
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_template == null)
            {
                EditorGUILayout.LabelField("No character template selected.");
                return;
            }

            EditorGUILayout.LabelField(
                _template.DisplayName ?? _template.name,
                EditorStyles.boldLabel
            );

            // class override picker
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Class (override)", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            var newClass = (CharacterClassData)
                EditorGUILayout.ObjectField(
                    _classOverride,
                    typeof(CharacterClassData),
                    allowSceneObjects: false
                );
            if (EditorGUI.EndChangeCheck())
            {
                _classOverride = newClass;
                ResetSimulation();
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                _classOverride = null;
                ResetSimulation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Simulations: {_levelUpCount}");
            EditorGUILayout.LabelField($"Good: {_goodLevelUps}   Bad: {_badLevelUps}");
            if (_instance != null)
            {
                var className = _instance.CurrentClassTemplate?.Identity.ClassName ?? "<none>";
                EditorGUILayout.LabelField($"Applied class: {className}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Level Up"))
            {
                if (_instance != null)
                {
                    try
                    {
                        // take snapshot of unbounded stats only (HP not counted)
                        var before = new Dictionary<UnboundedStatType, float>();
                        foreach (var stat in _instance.UnboundedStats)
                        {
                            if (stat.StatType == UnboundedStatType.Movement)
                            {
                                continue;
                            }

                            before[stat.StatType] = stat.Current;
                        }

                        _instance.LevelUp();
                        _levelUpCount++;

                        // compare after, tally only unbounded increases
                        int increased = 0;
                        foreach (var stat in _instance.UnboundedStats)
                        {
                            if (stat.StatType == UnboundedStatType.Movement)
                            {
                                continue;
                            }

                            if (
                                before.TryGetValue(stat.StatType, out var val)
                                && stat.Current > val
                            )
                            {
                                increased++;
                            }
                        }

                        if (increased <= 2)
                        {
                            _badLevelUps++;
                        }
                        else
                        {
                            _goodLevelUps++;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"LevelUp simulation failed: {ex}");
                    }
                }
            }
            if (GUILayout.Button("Reset"))
            {
                ResetSimulation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawRadarChart();
            EditorGUILayout.Space();
            DrawStatsComparison();
        }

        private void DrawRadarChart()
        {
            if (!TryBuildRadarChartData(out var labels, out var originalValues, out var currentValues))
            {
                return;
            }

            float radius = 80f;
            float scale = radius / GetRadarChartMaximum(originalValues, currentValues);

            Rect rect = GUILayoutUtility.GetRect(
                (radius * 2) + 20,
                (radius * 2) + 20,
                GUILayout.ExpandWidth(true)
            );
            Vector2 center = rect.center;

            Handles.BeginGUI();
            DrawRadarChartAxes(center, radius, labels.Count);
            DrawRadarChartValues(center, scale, originalValues, currentValues);
            DrawRadarChartLabels(center, radius, labels);
            Handles.EndGUI();
        }

        private bool TryBuildRadarChartData(
            out List<string> labels,
            out List<float> originalValues,
            out List<float> currentValues
        )
        {
            labels = new List<string>();
            originalValues = new List<float>();
            currentValues = new List<float>();
            if (
                _template == null
                || _instance == null
                || _template.UnboundedStats == null
                || _template.UnboundedStats.Count == 0
            )
            {
                return false;
            }

            AddUnboundedRadarStats(labels, originalValues, currentValues);
            AddHealthRadarStat(labels, originalValues, currentValues);
            return labels.Count > 0;
        }

        private void AddUnboundedRadarStats(
            List<string> labels,
            List<float> originalValues,
            List<float> currentValues
        )
        {
            foreach (var stat in _template.UnboundedStats)
            {
                if (stat == null || stat.StatType == UnboundedStatType.Movement)
                {
                    continue;
                }

                labels.Add(stat.StatType.ToString());
                float original = Mathf.RoundToInt(stat.Current);
                float current = Mathf.RoundToInt(
                    _instance.GetUnboundedStat(stat.StatType)?.Current ?? original
                );
                originalValues.Add(original);
                currentValues.Add(current);
            }
        }

        private void AddHealthRadarStat(
            List<string> labels,
            List<float> originalValues,
            List<float> currentValues
        )
        {
            if (_template.BoundedStats == null)
            {
                return;
            }

            var health = _template.GetBoundedStat(BoundedStatType.Health);
            if (health == null)
            {
                return;
            }

            var instanceHealth = _instance.GetBoundedStat(BoundedStatType.Health);
            labels.Add("Health");
            originalValues.Add(health.CurrentInt * 0.5f);
            currentValues.Add((instanceHealth != null ? instanceHealth.CurrentInt : health.CurrentInt) * 0.5f);
        }

        private static float GetRadarChartMaximum(
            List<float> originalValues,
            List<float> currentValues
        )
        {
            float maximum = 1f;
            for (int i = 0; i < originalValues.Count; i++)
            {
                maximum = Mathf.Max(maximum, originalValues[i], currentValues[i]);
            }

            return maximum;
        }

        private static void DrawRadarChartAxes(Vector2 center, float radius, int count)
        {
            Handles.color = Color.gray;
            Handles.DrawWireDisc(center, Vector3.forward, radius);
            for (int i = 0; i < count; i++)
            {
                float angle = GetRadarChartAngle(i, count);
                var axisEnd = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    center.y + Mathf.Sin(angle) * radius
                );
                Handles.DrawLine(center, axisEnd);
            }
        }

        private static void DrawRadarChartValues(
            Vector2 center,
            float scale,
            List<float> originalValues,
            List<float> currentValues
        )
        {
            var originalPoints = CreateRadarPoints(center, originalValues, scale);
            var currentPoints = CreateRadarPoints(center, currentValues, scale);
            Handles.color = new Color(0f, 0.5f, 1f, 0.6f);
            Handles.DrawAAPolyLine(3f, originalPoints);
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Handles.DrawAAPolyLine(3f, currentPoints);
        }

        private static Vector3[] CreateRadarPoints(
            Vector2 center,
            List<float> values,
            float scale
        )
        {
            var points = new Vector3[values.Count + 1];
            for (int i = 0; i < values.Count; i++)
            {
                float angle = GetRadarChartAngle(i, values.Count);
                float valueRadius = values[i] * scale;
                points[i] = new Vector3(
                    center.x + Mathf.Cos(angle) * valueRadius,
                    center.y + Mathf.Sin(angle) * valueRadius
                );
            }

            points[values.Count] = points[0];
            return points;
        }

        private static void DrawRadarChartLabels(Vector2 center, float radius, List<string> labels)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                float angle = GetRadarChartAngle(i, labels.Count);
                Vector2 position = new(
                    center.x + Mathf.Cos(angle) * (radius + 10),
                    center.y + Mathf.Sin(angle) * (radius + 10)
                );
                GUI.Label(
                    new Rect(position.x - 20, position.y - 8, 40, 16),
                    labels[i],
                    EditorStyles.miniLabel
                );
            }
        }

        private static float GetRadarChartAngle(int index, int count) =>
            ((Mathf.PI * 2f / count) * index) - (Mathf.PI / 2f);

        private void DrawStatsComparison()
        {
            // bounded stats
            EditorGUILayout.LabelField("Bounded Stats", EditorStyles.boldLabel);
            var gs = GameSettings.GameplayGeneralSettings.Instance;
            if (_template.BoundedStats != null && _instance != null)
            {
                foreach (var bounded in _template.BoundedStats)
                {
                    var type = bounded.StatType;
                    var orig = bounded.CurrentInt;
                    var cur = _instance.GetBoundedStat(type)?.CurrentInt ?? orig;
                    var delta = cur - orig;
                    EditorGUILayout.LabelField($"{type}: {cur} (+{delta})");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unbounded Stats", EditorStyles.boldLabel);
            if (_template.UnboundedStats != null && _instance != null)
            {
                foreach (var unbound in _template.UnboundedStats)
                {
                    if (unbound.StatType == UnboundedStatType.Movement)
                    {
                        continue;
                    }

                    var type = unbound.StatType;
                    var orig = Mathf.RoundToInt(unbound.Current);
                    var cur = Mathf.RoundToInt(_instance.GetUnboundedStat(type)?.Current ?? orig);
                    var delta = cur - orig;
                    EditorGUILayout.LabelField($"{type}: {cur} (+{delta})");
                }
            }
        }
    }
}
#endif
