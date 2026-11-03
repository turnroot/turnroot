#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs;
using UnityEngine;
using UnityEditor;

namespace Turnroot.EditorTools
{
    /// <summary>
    /// Editor window for validating character data and displaying completion status.
    /// </summary>
    public partial class CharacterChecklistWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<CharacterData> _characters = new List<CharacterData>();
        private string _statusMessage = "";

        // UI filters
        private bool _filterUniqueOnly = true;
        private bool _filterOnlyWarnOrCritical = false;

        [MenuItem("Window/Turnroot/Checklists/Character Checklist")]
        public static void ShowWindow()
        {
            var w = GetWindow<CharacterChecklistWindow>("Character Checklist");
            w.Refresh();
            w.minSize = new Vector2(700, 300);
        }

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Character Checklist", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                Refresh();
            }

            // Filters
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            var newUnique = EditorGUILayout.ToggleLeft(
                "Only IsUnique",
                _filterUniqueOnly,
                GUILayout.Width(140)
            );
            var newSeverity = EditorGUILayout.ToggleLeft(
                "Only Orange/Red",
                _filterOnlyWarnOrCritical,
                GUILayout.Width(150)
            );
            if (newUnique != _filterUniqueOnly || newSeverity != _filterOnlyWarnOrCritical)
            {
                _filterUniqueOnly = newUnique;
                _filterOnlyWarnOrCritical = newSeverity;
                // Clear selection on filter change
                _selectedCheckIndex = -1;
                _selectedCharacterIndex = -1;
                _statusMessage = "Filters updated";
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            DrawLegend();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(_statusMessage, EditorStyles.helpBox);

            EditorGUILayout.Space();
            DrawCheckTable();

            // Selected cell details
            EditorGUILayout.Space();
            if (_selectedCheckIndex >= 0 && _selectedCheckIndex < _checks.Count)
            {
                var chk = _checks[_selectedCheckIndex];
                GUILayout.Label($"Selected check: {chk.Label}", EditorStyles.boldLabel);
                // Recompute visible characters so selection maps correctly when filters change
                var visibleCharacters = _characters
                    .Where(c => (!_filterUniqueOnly || c.IsUnique))
                    .ToList();
                if (_filterOnlyWarnOrCritical)
                {
                    visibleCharacters = visibleCharacters
                        .Where(c =>
                        {
                            var a = AnalyzeCharacter(c);
                            return a.StatusLabel is "CRITICAL" or "WARN";
                        })
                        .ToList();
                }
                if (
                    _selectedCharacterIndex >= 0
                    && _selectedCharacterIndex < visibleCharacters.Count
                )
                {
                    var character = visibleCharacters[_selectedCharacterIndex];
                    var res = chk.Evaluator(character);
                    EditorGUILayout.HelpBox(res.Note ?? "", MessageType.None);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Select Character"))
                    {
                        Selection.activeObject = character;
                        EditorGUIUtility.PingObject(character);
                    }
                    if (GUILayout.Button("Refresh"))
                    {
                        Refresh();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Ping Missing Assets"))
            {
                PingMissingAssets();
            }
        }

        private void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLegendItem(Color.red, "Red: Will break / error on run");
            DrawLegendItem(new Color(1f, 0.6f, 0f), "Orange: Missing optional but may still run");
            DrawLegendItem(Color.yellow, "Yellow: Mostly defaults / needs polish");
            DrawLegendItem(Color.green, "Green: Looks good");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegendItem(Color col, string text)
        {
            var rect = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20));
            EditorGUI.DrawRect(rect, col);
            GUILayout.Label(text);
        }

        private void DrawCheckTable()
        {
            if (_characters == null || _characters.Count == 0)
            {
                EditorGUILayout.LabelField("No characters found.");
                return;
            }

            if (_checks == null || _checks.Count == 0)
            {
                PopulateChecks();
            }

            // Horizontal scroll for many characters
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(420));
            EditorGUILayout.BeginHorizontal();

            // Left column: check labels
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            GUILayout.Space(24);
            foreach (var check in _checks)
            {
                GUILayout.Label(check.Label, GUILayout.Height(22));
            }
            EditorGUILayout.EndVertical();

            // Build filtered character list according to toggles
            var visibleCharacters = _characters
                .Where(c => (!_filterUniqueOnly || c.IsUnique))
                .ToList();
            if (_filterOnlyWarnOrCritical)
            {
                visibleCharacters = visibleCharacters
                    .Where(c =>
                    {
                        var a = AnalyzeCharacter(c);
                        return a.StatusLabel is "CRITICAL" or "WARN";
                    })
                    .ToList();
            }

            // Character columns
            EditorGUILayout.BeginVertical();
            // Header row with character names
            EditorGUILayout.BeginHorizontal();
            foreach (var c in visibleCharacters)
            {
                var analysis = AnalyzeCharacter(c);
                EditorGUILayout.BeginVertical(GUILayout.Width(110));
                var hdrRect = GUILayoutUtility.GetRect(100, 40, GUILayout.Width(100));
                EditorGUI.DrawRect(hdrRect, new Color(0.15f, 0.15f, 0.15f));
                GUI.Label(hdrRect, new GUIContent(c.DisplayName ?? c.name));
                var colorRect = new Rect(hdrRect.x + hdrRect.width - 14, hdrRect.y + 4, 10, 10);
                EditorGUI.DrawRect(colorRect, analysis.Color);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            // Rows for each check
            for (int i = 0; i < _checks.Count; i++)
            {
                var check = _checks[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(4);
                for (int j = 0; j < visibleCharacters.Count; j++)
                {
                    var ch = visibleCharacters[j];
                    var res = check.Evaluator(ch);

                    EditorGUILayout.BeginVertical(GUILayout.Width(110), GUILayout.Height(22));
                    var rect = GUILayoutUtility.GetRect(100, 20, GUILayout.Width(100));
                    EditorGUI.DrawRect(rect, res.Color);

                    var content = new GUIContent("", res.Note ?? "");
                    if (GUI.Button(rect, content, GUIStyle.none))
                    {
                        _selectedCheckIndex = i;
                        _selectedCharacterIndex = j;
                        _statusMessage =
                            $"{check.Label} for {ch.DisplayName ?? ch.name}: {res.Note}";
                        Selection.activeObject = ch;
                        EditorGUIUtility.PingObject(ch);
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }

            // Bottom row: per-column Notes buttons (show aggregated notes in popup)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(4);
            for (int j = 0; j < visibleCharacters.Count; j++)
            {
                var ch = visibleCharacters[j];
                EditorGUILayout.BeginVertical(GUILayout.Width(110), GUILayout.Height(22));
                if (
                    GUILayout.Button(
                        new GUIContent("Notes", "Show all notes (most critical → least)"),
                        GUILayout.Width(100),
                        GUILayout.Height(20)
                    )
                )
                {
                    ShowNotesPopup(ch);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

    }
}
#endif
