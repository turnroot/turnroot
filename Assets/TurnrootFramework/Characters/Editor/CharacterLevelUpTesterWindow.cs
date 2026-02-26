#if UNITY_EDITOR
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
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
        private int _levelUpCount;
        private int _goodLevelUps;
        private int _badLevelUps;

        public static void Show(CharacterData template)
        {
            var window = GetWindow<CharacterLevelUpTesterWindow>("Level Up Tester");
            window._template = template;
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
            EditorGUILayout.LabelField($"Simulations: {_levelUpCount}");
            EditorGUILayout.LabelField($"Good: {_goodLevelUps}   Bad: {_badLevelUps}");

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
                                continue;
                            before[stat.StatType] = stat.Current;
                        }

                        _instance.LevelUp();
                        _levelUpCount++;

                        // compare after, tally only unbounded increases
                        int increased = 0;
                        foreach (var stat in _instance.UnboundedStats)
                        {
                            if (stat.StatType == UnboundedStatType.Movement)
                                continue;
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
            if (_template == null || _instance == null)
                return;

            var raw = _template.UnboundedStats;
            if (raw == null || raw.Count == 0)
                return;

            // build parallel arrays for orig/current values and labels
            var labels = new List<string>();
            var origVals = new List<float>();
            var curVals = new List<float>();

            // unbounded stats except movement
            foreach (var s in raw)
            {
                if (s == null || s.StatType == UnboundedStatType.Movement)
                    continue;
                labels.Add(s.StatType.ToString());
                float o = Mathf.RoundToInt(s.Current);
                float c = Mathf.RoundToInt(_instance.GetUnboundedStat(s.StatType)?.Current ?? o);
                origVals.Add(o);
                curVals.Add(c);
            }

            // add bounded health stat as extra slice (divide by 2 for chart)
            if (_template.BoundedStats != null)
            {
                var hp = _template.GetBoundedStat(BoundedStatType.Health);
                var hpInst = _instance.GetBoundedStat(BoundedStatType.Health);
                if (hp != null)
                {
                    labels.Add("Health");
                    float o = hp.CurrentInt;
                    float c = hpInst != null ? hpInst.CurrentInt : o;
                    // scale down for display
                    origVals.Add(o * 0.5f);
                    curVals.Add(c * 0.5f);
                }
            }

            if (labels.Count == 0)
                return;

            // compute max for scaling including health above
            float maxVal = 1f;
            for (int i = 0; i < labels.Count; i++)
            {
                maxVal = Mathf.Max(maxVal, origVals[i], curVals[i]);
            }
            float radius = 80f;
            float scale = maxVal > 0 ? radius / maxVal : 1f;

            Rect rect = GUILayoutUtility.GetRect(
                (radius * 2) + 20,
                (radius * 2) + 20,
                GUILayout.ExpandWidth(true)
            );
            Vector2 center = rect.center;

            Handles.BeginGUI();
            Handles.color = Color.gray;
            Handles.DrawWireDisc(center, Vector3.forward, radius);

            int count = labels.Count;
            var pointsOrig = new Vector3[count + 1];
            var pointsCur = new Vector3[count + 1];

            for (int i = 0; i < count; i++)
            {
                float angle = ((Mathf.PI * 2f / count) * i) - (Mathf.PI / 2f);
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float orig = origVals[i] * scale;
                float cur = curVals[i] * scale;

                pointsOrig[i] = new Vector3(center.x + (cos * orig), center.y + (sin * orig));
                pointsCur[i] = new Vector3(center.x + (cos * cur), center.y + (sin * cur));

                Handles.color = Color.gray;
                Handles.DrawLine(
                    center,
                    new Vector3(center.x + (cos * radius), center.y + (sin * radius))
                );
            }
            pointsOrig[count] = pointsOrig[0];
            pointsCur[count] = pointsCur[0];

            Handles.color = new Color(0f, 0.5f, 1f, 0.6f);
            Handles.DrawAAPolyLine(3f, pointsOrig);
            Handles.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Handles.DrawAAPolyLine(3f, pointsCur);

            // labels
            for (int i = 0; i < count; i++)
            {
                float angle = ((Mathf.PI * 2f / count) * i) - (Mathf.PI / 2f);
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                Vector2 labelPos = new Vector2(
                    center.x + (cos * (radius + 10)),
                    center.y + (sin * (radius + 10))
                );
                GUI.Label(
                    new Rect(labelPos.x - 20, labelPos.y - 8, 40, 16),
                    labels[i],
                    EditorStyles.miniLabel
                );
            }

            Handles.EndGUI();
        }

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
                        continue;
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
