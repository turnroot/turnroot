#if UNITY_EDITOR
using System.Collections.Generic;
using Turnroot.Characters.Stats;
using Turnroot.Characters.CharacterClass;
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

namespace Turnroot.Characters.Editor
{
    /// <summary>
    /// Popup that previews level-up growth using only CharacterData and class data.
    /// The preview never creates a CharacterInstance or reads/writes LTM.
    /// </summary>
    public class CharacterLevelUpTesterWindow : EditorWindow
    {
        private CharacterData _template;
        private readonly Dictionary<UnboundedStatType, float> _currentUnboundedStats = new();
        private readonly Dictionary<BoundedStatType, float> _currentBoundedStats = new();
        private readonly Dictionary<BoundedStatType, float> _boundedStatMaxima = new();
        private CharacterClassData _classOverride;
        private int _levelUpCount;
        private int _goodLevelUps;
        private int _badLevelUps;

        private CharacterClassData ActiveClass =>
            _classOverride ?? _template?.GetPreferredStartingClass();

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
            _currentUnboundedStats.Clear();
            _currentBoundedStats.Clear();
            _boundedStatMaxima.Clear();
            _levelUpCount = 0;
            _goodLevelUps = 0;
            _badLevelUps = 0;

            if (_template?.UnboundedStats != null)
            {
                foreach (var stat in _template.UnboundedStats)
                {
                    if (stat != null && Enum.IsDefined(typeof(UnboundedStatType), stat.StatType))
                    {
                        _currentUnboundedStats[stat.StatType] = stat.Current;
                    }
                }
            }

            if (_template?.BoundedStats != null)
            {
                foreach (var stat in _template.BoundedStats)
                {
                    if (stat != null)
                    {
                        _currentBoundedStats[stat.StatType] = stat.Current;
                        _boundedStatMaxima[stat.StatType] = stat.Max;
                    }
                }
            }
        }

        private void SimulateLevelUp()
        {
            if (_template == null)
            {
                return;
            }

            var classData = ActiveClass;
            bool extraGrowthEnabled =
                GameSettings.GameplayGeneralSettings.Instance?.LevelUpExtraGrowthChance ?? false;
            var increasedStats = new HashSet<UnboundedStatType>();

            if (_currentBoundedStats.TryGetValue(BoundedStatType.Level, out var level))
            {
                level += 1f;
                _currentBoundedStats[BoundedStatType.Level] = level;
                float currentMax = _boundedStatMaxima.TryGetValue(
                    BoundedStatType.Level,
                    out var max
                )
                    ? max
                    : level;
                _boundedStatMaxima[BoundedStatType.Level] = Mathf.Max(currentMax, level);
            }

            if (_currentBoundedStats.ContainsKey(BoundedStatType.Health))
            {
                float hpGrowth = GetPersonalGrowth(BoundedStatType.Health)
                    + GetClassGrowth(classData, BoundedStatType.Health);
                float hpRoll = UnityEngine.Random.Range(0f, 100f);

                if (hpGrowth > 100f && extraGrowthEnabled)
                {
                    IncreaseHealth();
                    hpGrowth -= 100f;
                }

                if (hpRoll < hpGrowth)
                {
                    IncreaseHealth();
                }
            }

            foreach (UnboundedStatType statType in Enum.GetValues(typeof(UnboundedStatType)))
            {
                if (statType == UnboundedStatType.Movement)
                {
                    continue;
                }

                if (!_currentUnboundedStats.TryGetValue(statType, out var current))
                {
                    continue;
                }

                float growth = GetPersonalGrowth(statType)
                    + GetClassGrowth(classData, statType);
                float clampedGrowth = Mathf.Clamp(growth, 0f, 100f);

                if (extraGrowthEnabled && growth > clampedGrowth)
                {
                    current += 1f;
                    increasedStats.Add(statType);
                }

                if (UnityEngine.Random.Range(0f, 100f) < clampedGrowth)
                {
                    float cap = GetClassCap(classData, statType);
                    if (cap <= 0f || current < cap)
                    {
                        current += 1f;
                        increasedStats.Add(statType);
                    }
                }

                _currentUnboundedStats[statType] = current;
            }

            if (
                Enum.GetValues(typeof(UnboundedStatType))
                    .Cast<UnboundedStatType>()
                    .Where(statType => statType != UnboundedStatType.Movement)
                    .All(increasedStats.Contains)
            )
            {
                IncreaseHealth();
            }

            _levelUpCount++;
            if (increasedStats.Count <= 2)
            {
                _badLevelUps++;
            }
            else
            {
                _goodLevelUps++;
            }

            void IncreaseHealth()
            {
                if (
                    !_currentBoundedStats.TryGetValue(BoundedStatType.Health, out var currentHealth)
                    || !_boundedStatMaxima.TryGetValue(
                        BoundedStatType.Health,
                        out var currentMaxHealth
                    )
                )
                {
                    return;
                }

                currentMaxHealth += 1f;
                currentHealth += 1f;
                _boundedStatMaxima[BoundedStatType.Health] = currentMaxHealth;
                _currentBoundedStats[BoundedStatType.Health] = currentHealth;
            }
        }

        private float GetPersonalGrowth(BoundedStatType statType)
        {
            var rate = _template?.PersonalGrowthRates?.Find(entry =>
                entry.isBounded && entry.boundedStatType == statType
            );
            return rate?.value ?? 0f;
        }

        private float GetPersonalGrowth(UnboundedStatType statType)
        {
            var rate = _template?.PersonalGrowthRates?.Find(entry =>
                !entry.isBounded && entry.unboundedStatType == statType
            );
            return rate?.value ?? 0f;
        }

        private static float GetClassGrowth(CharacterClassData classData, BoundedStatType statType)
        {
            var rate = classData?.Stats?.GrowthRateModifiers?.Find(entry =>
                entry.isBounded && entry.boundedStatType == statType
            );
            return rate?.value ?? 0f;
        }

        private static float GetClassGrowth(
            CharacterClassData classData,
            UnboundedStatType statType
        )
        {
            var rate = classData?.Stats?.GrowthRateModifiers?.Find(entry =>
                !entry.isBounded && entry.unboundedStatType == statType
            );
            return rate?.value ?? 0f;
        }

        private static float GetClassCap(CharacterClassData classData, UnboundedStatType statType)
        {
            var cap = classData?.Stats?.StatCaps?.Find(entry =>
                !entry.isBounded && entry.unboundedStatType == statType
            );
            return cap?.value ?? 0f;
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
            EditorGUILayout.LabelField($"Class: {ActiveClass?.Identity?.ClassName ?? "<none>"}");

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Level Up"))
            {
                SimulateLevelUp();
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
                if (
                    stat == null
                    || !Enum.IsDefined(typeof(UnboundedStatType), stat.StatType)
                )
                {
                    continue;
                }

                labels.Add(stat.StatType.ToString());
                float original = Mathf.RoundToInt(stat.Current);
                float current = Mathf.RoundToInt(
                    _currentUnboundedStats.TryGetValue(stat.StatType, out var value)
                        ? value
                        : original
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

            labels.Add("Health");
            originalValues.Add(health.CurrentInt * 0.5f);
            currentValues.Add(
                Mathf.RoundToInt(
                    _currentBoundedStats.TryGetValue(BoundedStatType.Health, out var value)
                        ? value
                        : health.CurrentInt
                ) * 0.5f
            );
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
            if (_template.BoundedStats != null)
            {
                foreach (var bounded in _template.BoundedStats)
                {
                    var type = bounded.StatType;
                    var orig = bounded.CurrentInt;
                    var cur = Mathf.RoundToInt(
                        _currentBoundedStats.TryGetValue(type, out var value) ? value : orig
                    );
                    var delta = cur - orig;
                    EditorGUILayout.LabelField($"{type}: {cur} (+{delta})");
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unbounded Stats", EditorStyles.boldLabel);
            if (_template.UnboundedStats != null)
            {
                foreach (var unbound in _template.UnboundedStats)
                {
                    if (!Enum.IsDefined(typeof(UnboundedStatType), unbound.StatType))
                    {
                        continue;
                    }

                    var type = unbound.StatType;
                    var orig = Mathf.RoundToInt(unbound.Current);
                    var cur = Mathf.RoundToInt(
                        _currentUnboundedStats.TryGetValue(type, out var value) ? value : orig
                    );
                    var delta = cur - orig;
                    EditorGUILayout.LabelField($"{type}: {cur} (+{delta})");
                }
            }
        }
    }
}
#endif
