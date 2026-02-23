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
    public class CharacterChecklistWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<CharacterData> _characters = new List<CharacterData>();
        private string _statusMessage = "";

        // UI filters
        private bool _filterUniqueOnly = true;
        private bool _filterOnlyWarnOrCritical = false;

        [MenuItem("Turnroot/Checklists/Character Checklist")]
        public static void ShowWindow()
        {
            var w = GetWindow<CharacterChecklistWindow>("Character Checklist");
            w.Refresh();
            w.minSize = new Vector2(700, 300);
        }

        private void OnEnable()
        {
            Refresh();
        }

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
                            return a.StatusLabel == "CRITICAL" || a.StatusLabel == "WARN";
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
                        return a.StatusLabel == "CRITICAL" || a.StatusLabel == "WARN";
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

        // Roster indices cached for checks
        private HashSet<CharacterData> _allRosterCharacters = new();
        private HashSet<CharacterData> _nonPersistentRosterCharacters = new();
        private HashSet<CharacterData> _persistentPlayerRosterCharacters = new();
        private Dictionary<CharacterData, List<string>> _rosterLocations =
            new Dictionary<CharacterData, List<string>>();

        // Characters referenced by EnemySupervisor.GenericEnemyStartingPlacement (generic enemies)
        private HashSet<CharacterData> _genericEnemyPlacementCharacters = new();

        private void Refresh()
        {
            _statusMessage = "Loading CharacterData from Resources...";
            _characters.Clear();

            // Prefer Resources.LoadAll(CharacterData)
            var fromResources = Resources.LoadAll<CharacterData>("");
            if (fromResources != null && fromResources.Length > 0)
            {
                _characters.AddRange(fromResources);
                _statusMessage = $"Loaded {_characters.Count} CharacterData assets from Resources.";
            }
            else
            {
                // Fallback to AssetDatabase (in case assets are not in Resources)
                var guids = AssetDatabase.FindAssets("t:CharacterData");
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var asset = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                    if (asset != null)
                    {
                        _characters.Add(asset);
                    }
                }
                _statusMessage =
                    $"Loaded {_characters.Count} CharacterData assets via AssetDatabase fallback.";
            }

            // Sort alphabetically
            _characters = _characters.OrderBy(c => c.DisplayName ?? c.name).ToList();

            // Build roster indices used by checks (scan project rosters)
            BuildRosterIndices();
        }

        private void PingMissingAssets()
        {
            foreach (var c in _characters)
            {
                var analysis = AnalyzeCharacter(c);
                if (analysis.IsCritical)
                {
                    // Ping the first missing asset if available
                    if (analysis.MissingPrefabs != null && analysis.MissingPrefabs.Count > 0)
                    {
                        var obj = analysis.MissingPrefabs[0];
                        if (obj != null)
                        {
                            Selection.activeObject = obj;
                            EditorGUIUtility.PingObject(obj);
                            return;
                        }
                    }
                }
            }
            Debug.Log("No critical missing assets found to ping.");
        }

        // --- Checks infrastructure ---
        private List<CharacterCheckDefinition> _checks = new List<CharacterCheckDefinition>();
        private int _selectedCheckIndex = -1;
        private int _selectedCharacterIndex = -1;

        /// <summary>
        /// Defines a single validation check for character data.
        /// </summary>
        private class CharacterCheckDefinition
        {
            public string Label;
            public System.Func<CharacterData, CharacterCheckResult> Evaluator;

            public CharacterCheckDefinition(
                string label,
                System.Func<CharacterData, CharacterCheckResult> eval
            )
            {
                Label = label;
                Evaluator = eval;
            }
        }

        /// <summary>
        /// Represents the result of a character validation check.
        /// </summary>
        private class CharacterCheckResult
        {
            public Color Color = Color.gray;
            public string Note = "";
        }

        private void PopulateChecks()
        {
            _checks.Clear();
            // Helper color constants
            var red = Color.red;
            var orange = new Color(1f, 0.5f, 0f);
            var yellow = Color.yellow;
            var green = Color.green;

            // Roster presence check
            // - Unique characters: expected in a roster (green); missing -> yellow
            // - Generic (non-unique) characters: presence in an EnemySupervisor.GenericEnemyStartingPlacement is the desired marker (green); missing -> yellow
            _checks.Add(
                new CharacterCheckDefinition(
                    "Roster Presence",
                    data =>
                    {
                        var r = new CharacterCheckResult();

                        // Look up precomputed sets from Refresh()
                        bool inAny = _allRosterCharacters.Contains(data);
                        bool inNonPersistent = _nonPersistentRosterCharacters.Contains(data);
                        bool inPersistentPlayer = _persistentPlayerRosterCharacters.Contains(data);

                        if (data.IsUnique)
                        {
                            if (inAny)
                            {
                                r.Color = green;
                            }
                            else
                            {
                                r.Color = yellow;
                                r.Note = "Unique character not present in any roster";
                            }
                        }
                        else
                        {
                            // Generic characters: check EnemySupervisor placements instead of rosters
                            if (_genericEnemyPlacementCharacters.Contains(data))
                            {
                                r.Color = green;
                                r.Note = "Found in EnemySupervisor GenericEnemyStartingPlacement";
                            }
                            else
                            {
                                r.Color = yellow;
                                r.Note =
                                    "Non-unique character not present in any EnemySupervisor placements";
                            }
                        }

                        // Also attach a brief note listing roster/placement locations if available
                        if (
                            _rosterLocations.TryGetValue(data, out var locations)
                            && locations.Count > 0
                        )
                        {
                            var locs = string.Join(", ", locations);
                            r.Note = string.IsNullOrEmpty(r.Note)
                                ? $"In rosters/placements: {locs}"
                                : r.Note + $"; In rosters/placements: {locs}";
                        }

                        return r;
                    }
                )
            );

            // Full name (critical)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Full Name",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (
                            string.IsNullOrWhiteSpace(data.FullName)
                            || data.FullName == "Newly Created Unit"
                        )
                        {
                            r.Color = red;
                            r.Note = "Missing full name";
                        }
                        else
                        {
                            r.Color = green;
                        }
                        return r;
                    }
                )
            );

            // Display name (warn)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Display Name",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (
                            string.IsNullOrWhiteSpace(data.DisplayName)
                            || data.DisplayName == "New Unit"
                        )
                        {
                            r.Color = orange;
                            r.Note = "Missing display name";
                        }
                        else
                        {
                            r.Color = green;
                        }
                        return r;
                    }
                )
            );

            // Species (critical)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Species",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.Species == null)
                        {
                            r.Color = red;
                            r.Note = "Missing species";
                        }
                        else
                        {
                            r.Color = green;
                        }
                        return r;
                    }
                )
            );

            // Non-battle outfit (critical if missing or invalid)
            _checks.Add(
                new CharacterCheckDefinition(
                    "NonBattle Outfit",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.NonBattleOutfitPrefab == null)
                        {
                            r.Color = red;
                            r.Note = "Missing NonBattleOutfitPrefab";
                            return r;
                        }
                        var smrs =
                            data.NonBattleOutfitPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(
                                true
                            );
                        if (smrs == null || smrs.Length == 0)
                        {
                            r.Color = red;
                            r.Note = "NonBattleOutfitPrefab contains no SkinnedMeshRenderer";
                            return r;
                        }

                        // If character defines blendshapes, ensure all submeshes contain them and have sharedMesh
                        var sb = data.Blendshapes.BlendshapeNames;
                        if (sb != null && sb.Length > 0)
                        {
                            var missingAny = new List<string>();
                            foreach (var smr in smrs)
                            {
                                var mesh = smr.sharedMesh;
                                if (mesh == null)
                                {
                                    missingAny.Add($"{smr.gameObject.name}:no_mesh");
                                    continue;
                                }
                                foreach (var b in sb)
                                {
                                    if (mesh.GetBlendShapeIndex(b) < 0)
                                    {
                                        missingAny.Add($"{smr.gameObject.name}:{b}");
                                    }
                                }
                            }
                            if (missingAny.Count > 0)
                            {
                                r.Color = red;
                                r.Note =
                                    $"Outfit missing blendshapes on submeshes: {string.Join(", ", missingAny)}";
                                return r;
                            }
                        }

                        // Recommendation: prefer a dedicated child renderer named 'Hair' for hair meshes
                        // so runtime material-preservation is deterministic.
                        try
                        {
                            var prefab = data.NonBattleOutfitPrefab;
                            if (prefab != null)
                            {
                                if (prefab.transform.Find("Hair") != null)
                                {
                                    r.Color = yellow;
                                    r.Note =
                                        "Outfit prefab contains a 'Hair' renderer — move hair into the character HairPrefab instead.";
                                    return r;
                                }

                                var childSmrs = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(
                                    true
                                );
                                foreach (var smr in childSmrs)
                                {
                                    if (smr == null)
                                    {
                                        continue;
                                    }

                                    var n = smr.gameObject.name ?? string.Empty;
                                    if (
                                        n.IndexOf("hair", System.StringComparison.OrdinalIgnoreCase)
                                        >= 0
                                    )
                                    {
                                        r.Color = yellow;
                                        r.Note =
                                            "Outfit prefab contains a 'Hair' renderer — move hair into the character HairPrefab instead.";
                                        return r;
                                    }
                                }
                            }
                        }
                        catch
                        { /* non-fatal */
                        }

                        r.Color = green;
                        return r;
                    }
                )
            );

            // Head & hands (warn)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Head & Hands",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.HeadAndHandsPrefab == null)
                        {
                            r.Color = orange;
                            r.Note = "Missing head & hands prefab";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Hair (warn)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Hair",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.HairPrefab == null)
                        {
                            r.Color = orange;
                            r.Note = "Missing hair prefab";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Default portrait (warn)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Default Portrait",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.DefaultPortrait == null)
                        {
                            r.Color = orange;
                            r.Note = "No default portrait";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Support relationships
            // - Unique characters: recommended to have support relationships (0 -> orange)
            // - Generic (non-unique) characters: must NOT have support relationships (0 -> green, any -> red)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Support Relationships",
                    data =>
                    {
                        var r = new CharacterCheckResult();

                        if (!data.IsUnique)
                        {
                            // Generic characters should have *no* support relationships
                            if (
                                data.SupportRelationships == null
                                || data.SupportRelationships.Count == 0
                            )
                            {
                                r.Color = green;
                            }
                            else
                            {
                                r.Color = red;
                                r.Note = "Generic characters must not define support relationships";
                            }
                        }
                        else
                        {
                            // Unique characters: warn if zero
                            if (
                                data.SupportRelationships == null
                                || data.SupportRelationships.Count == 0
                            )
                            {
                                r.Color = orange;
                                r.Note = "No support relationships";
                            }
                            else
                            {
                                r.Color = green;
                            }
                        }

                        return r;
                    }
                )
            );

            // Progression ladder sanity
            _checks.Add(
                new CharacterCheckDefinition(
                    "Progression Ladder",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (data.UseClassProgressionLadder)
                        {
                            bool any =
                                data.ProgressionLadder.Starter.Class != null
                                || data.ProgressionLadder.Base.Class != null
                                || data.ProgressionLadder.Advanced.Class != null
                                || data.ProgressionLadder.Master.Class != null
                                || data.ProgressionLadder.Expert.Class != null;
                            if (!any)
                            {
                                r.Color = orange;
                                r.Note = "Ladder enabled but no classes set";
                            }
                            else
                            {
                                r.Color = green;
                            }
                        }
                        else
                        {
                            r.Color = green;
                        }
                        return r;
                    }
                )
            );

            // Badge text/icon (yellow)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Badge",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (string.IsNullOrWhiteSpace(data.BadgeText) || data.BadgeIcon == null)
                        {
                            r.Color = yellow;
                            r.Note = "Badge text or icon missing";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Stats default (yellow)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Stats vs Default",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        var gs = Turnroot.GameSettings.GameplayGeneralSettings.Instance;
                        var defBounded =
                            gs != null
                                ? gs.CreateDefaultBoundedStats()
                                : new List<BoundedCharacterStat>();
                        var defUnbounded =
                            gs != null
                                ? gs.CreateDefaultUnboundedStats()
                                : new List<CharacterStat>();
                        bool boundedMatch = true;
                        bool unboundedMatch = true;
                        if (defBounded.Count != data.BoundedStats.Count)
                        {
                            boundedMatch = false;
                        }
                        else
                        {
                            foreach (var d in defBounded)
                            {
                                var found = data.BoundedStats.Find(s => s.StatType == d.StatType);
                                if (
                                    found == null
                                    || found.CurrentInt != d.CurrentInt
                                    || found.MaxInt != d.MaxInt
                                )
                                {
                                    boundedMatch = false;
                                    break;
                                }
                            }
                        }
                        if (defUnbounded.Count != data.UnboundedStats.Count)
                        {
                            unboundedMatch = false;
                        }
                        else
                        {
                            foreach (var d in defUnbounded)
                            {
                                var found = data.UnboundedStats.Find(s => s.StatType == d.StatType);
                                if (
                                    found == null
                                    || Mathf.RoundToInt(found.Current)
                                        != Mathf.RoundToInt(d.Current)
                                )
                                {
                                    unboundedMatch = false;
                                    break;
                                }
                            }
                        }
                        if (boundedMatch && unboundedMatch)
                        {
                            r.Color = yellow;
                            r.Note = "Stats equal defaults";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Growth rates (yellow if all zero)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Growth Rates",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        bool growthAllZero = true;
                        if (data.PersonalGrowthRates != null && data.PersonalGrowthRates.Count > 0)
                        {
                            foreach (var g in data.PersonalGrowthRates)
                            {
                                if (!Mathf.Approximately(g.value, 0f))
                                {
                                    growthAllZero = false;
                                    break;
                                }
                            }
                        }
                        if (growthAllZero)
                        {
                            r.Color = yellow;
                            r.Note = "Personal growth rates are zero";
                        }
                        else
                        {
                            r.Color = green;
                        }

                        return r;
                    }
                )
            );

            // Colors (orange if black/white or accents identical)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Accent/Skin Colors",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        var ac1 = data.AccentColor1;
                        var ac2 = data.AccentColor2;
                        var ac3 = data.AccentColor3;
                        var skin = data.SkinColor;

                        bool IsBlackOrWhite(Color c)
                        {
                            const float eps = 0.01f;
                            bool isBlack =
                                Mathf.Abs(c.r) <= eps
                                && Mathf.Abs(c.g) <= eps
                                && Mathf.Abs(c.b) <= eps;
                            bool isWhite =
                                Mathf.Abs(c.r - 1f) <= eps
                                && Mathf.Abs(c.g - 1f) <= eps
                                && Mathf.Abs(c.b - 1f) <= eps;
                            return isBlack || isWhite;
                        }

                        bool AreClose(Color a, Color b)
                        {
                            const float eps = 0.01f;
                            return Mathf.Abs(a.r - b.r) <= eps
                                && Mathf.Abs(a.g - b.g) <= eps
                                && Mathf.Abs(a.b - b.b) <= eps;
                        }

                        // If any are black/white -> warn (orange)
                        if (
                            IsBlackOrWhite(ac1)
                            || IsBlackOrWhite(ac2)
                            || IsBlackOrWhite(ac3)
                            || IsBlackOrWhite(skin)
                        )
                        {
                            r.Color = orange;
                            r.Note = "Accent or skin color is black/white";
                            return r;
                        }

                        // If the three accent colors are effectively identical -> warn (orange)
                        if (AreClose(ac1, ac2) && AreClose(ac2, ac3))
                        {
                            r.Color = orange;
                            r.Note = "Accent colors are identical (consider variety)";
                            return r;
                        }

                        r.Color = green;
                        return r;
                    }
                )
            );
        }

        private CharacterAnalysis AnalyzeCharacter(CharacterData data)
        {
            var result = new CharacterAnalysis();
            result.Name = data.DisplayName ?? data.name;

            var requiredBlendshapes = data.Blendshapes.BlendshapeNames;
            bool hasBlendshapes = requiredBlendshapes != null && requiredBlendshapes.Length > 0;
            bool hasNonBattleOutfit = data.NonBattleOutfitPrefab != null;
            bool hasHeadAndHands = data.HeadAndHandsPrefab != null;
            bool hasHair = data.HairPrefab != null;

            // Separate note buckets so we can properly color-code severity.
            var criticalNotes = new List<string>();
            var warnNotes = new List<string>();
            var yellowNotes = new List<string>();
            var missingPrefabs = new List<Object>();

            // Validate NonBattleOutfit prefab presence/SMR/blendshapes when needed
            List<string> nonBattleMissingBlendshapes = new List<string>();
            if (hasNonBattleOutfit)
            {
                var smr = data.NonBattleOutfitPrefab.GetComponentInChildren<SkinnedMeshRenderer>(
                    true
                );
                if (smr == null)
                {
                    criticalNotes.Add("NonBattleOutfitPrefab missing SkinnedMeshRenderer");
                    missingPrefabs.Add(data.NonBattleOutfitPrefab);
                }
                else
                {
                    var mesh = smr.sharedMesh;
                    if (mesh == null)
                    {
                        criticalNotes.Add("NonBattleOutfitPrefab missing sharedMesh");
                        missingPrefabs.Add(data.NonBattleOutfitPrefab);
                    }
                    else if (hasBlendshapes)
                    {
                        foreach (var b in requiredBlendshapes)
                        {
                            if (mesh.GetBlendShapeIndex(b) < 0)
                            {
                                nonBattleMissingBlendshapes.Add(b);
                            }
                        }
                        if (nonBattleMissingBlendshapes.Count > 0)
                        {
                            criticalNotes.Add(
                                $"NonBattleOutfitPrefab missing blendshapes: {string.Join(", ", nonBattleMissingBlendshapes)}"
                            );
                            missingPrefabs.Add(data.NonBattleOutfitPrefab);
                        }
                    }
                }
            }

            // If blendshapes are defined but no non-battle outfit -> critical
            if (hasBlendshapes && !hasNonBattleOutfit)
            {
                criticalNotes.Add("Has blendshapes but no NonBattleOutfitPrefab (required)");
            }

            // Optional but recommended (warn-level)
            if (!hasHeadAndHands)
            {
                warnNotes.Add("Missing HeadAndHandsPrefab (recommended)");
            }
            if (!hasHair)
            {
                warnNotes.Add("Missing HairPrefab (recommended)");
            }

            // Portrait check (warn/orange)
            if (data.DefaultPortrait == null)
            {
                warnNotes.Add("No default portrait assigned (recommended)");
            }

            // Support relationships
            if (!data.IsUnique)
            {
                // Generic characters must NOT have support relationships
                if (data.SupportRelationships != null && data.SupportRelationships.Count > 0)
                {
                    criticalNotes.Add("Generic characters must not have support relationships");
                }
            }
            else
            {
                // Unique characters: warn if zero
                if (data.SupportRelationships == null || data.SupportRelationships.Count == 0)
                {
                    warnNotes.Add("No support relationships (recommended)");
                }
            }

            // Progression ladder summary
            if (data.UseClassProgressionLadder)
            {
                bool any =
                    data.ProgressionLadder.Starter.Class != null
                    || data.ProgressionLadder.Base.Class != null
                    || data.ProgressionLadder.Advanced.Class != null
                    || data.ProgressionLadder.Master.Class != null
                    || data.ProgressionLadder.Expert.Class != null;
                if (!any)
                {
                    warnNotes.Add("Progression ladder enabled but no classes assigned");
                }
            }

            // Badge checks (yellow)
            if (string.IsNullOrWhiteSpace(data.BadgeText) || data.BadgeIcon == null)
            {
                yellowNotes.Add("Missing badge text or icon (recommended)");
            }

            // Species check (critical if not assigned)
            if (data.Species == null)
            {
                criticalNotes.Add("Missing Species (required)");
            }

            // Stats: compare against defaults (yellow if they match default exactly)
            bool statsAreDefault = false;
            var gs = Turnroot.GameSettings.GameplayGeneralSettings.Instance;
            if (gs != null)
            {
                var defBounded = gs.CreateDefaultBoundedStats();
                var defUnbounded = gs.CreateDefaultUnboundedStats();
                bool boundedMatch = true;
                bool unboundedMatch = true;

                if (defBounded.Count != data.BoundedStats.Count)
                {
                    boundedMatch = false;
                }
                else
                {
                    foreach (var d in defBounded)
                    {
                        var found = data.BoundedStats.Find(s => s.StatType == d.StatType);
                        if (
                            found == null
                            || found.CurrentInt != d.CurrentInt
                            || found.MaxInt != d.MaxInt
                        )
                        {
                            boundedMatch = false;
                            break;
                        }
                    }
                }

                if (defUnbounded.Count != data.UnboundedStats.Count)
                {
                    unboundedMatch = false;
                }
                else
                {
                    foreach (var d in defUnbounded)
                    {
                        var found = data.UnboundedStats.Find(s => s.StatType == d.StatType);
                        if (
                            found == null
                            || Mathf.RoundToInt(found.Current) != Mathf.RoundToInt(d.Current)
                        )
                        {
                            unboundedMatch = false;
                            break;
                        }
                    }
                }

                statsAreDefault = boundedMatch && unboundedMatch;
                if (statsAreDefault)
                {
                    yellowNotes.Add("Stats match project defaults (consider customizing)");
                }
            }

            // Personal growth rates shouldn't all be zero (yellow)
            bool growthAllZero = true;
            if (data.PersonalGrowthRates != null && data.PersonalGrowthRates.Count > 0)
            {
                foreach (var g in data.PersonalGrowthRates)
                {
                    if (!Mathf.Approximately(g.value, 0f))
                    {
                        growthAllZero = false;
                        break;
                    }
                }
            }
            if (growthAllZero)
            {
                yellowNotes.Add("Personal growth rates are all zero (consider non-zero growth)");
            }

            // Accent/skin color checks (yellow if black or white)
            bool badColors = false;
            Color[] checkColors = new Color[]
            {
                data.AccentColor1,
                data.AccentColor2,
                data.AccentColor3,
                data.SkinColor,
            };
            foreach (var c in checkColors)
            {
                if (c == Color.black || c == Color.white)
                {
                    badColors = true;
                    break;
                }
            }
            if (badColors)
            {
                yellowNotes.Add(
                    "Accent or skin color uses black or white (consider setting a proper tint)"
                );
            }

            // Default-ish checks (name/sprites/portraits) - factor into yellow if many defaults
            int defaultScore = 0;
            if (string.IsNullOrWhiteSpace(data.DisplayName) || data.DisplayName == "New Unit")
            {
                defaultScore++;
            }

            if (data.DisplayName == data.FullName)
            {
                defaultScore++;
                yellowNotes.Add("Display name matches full name (consider differentiating)");
            }

            if (data.Portraits == null || data.Portraits.Count == 0)
            {
                defaultScore++;
                yellowNotes.Add("No portraits assigned (consider adding portraits)");
            }

            if (defaultScore >= 1)
            {
                yellowNotes.Add("Many fields are default/empty (name/portraits)");
            }

            // Aggregate notes (preserve severity)
            var notes = new List<string>();
            notes.AddRange(criticalNotes);
            notes.AddRange(warnNotes);
            notes.AddRange(yellowNotes);

            // Set critical flag if any critical notes exist
            if (criticalNotes.Count > 0)
            {
                result.IsCritical = true;
            }

            // Decide color/status (priority: critical -> warn -> yellow -> ok)
            if (result.IsCritical)
            {
                result.Color = Color.red;
                result.StatusLabel = "CRITICAL";
            }
            else if (warnNotes.Count > 0)
            {
                result.Color = new Color(1f, 0.6f, 0f);
                result.StatusLabel = "WARN";
            }
            else if (yellowNotes.Count > 0)
            {
                result.Color = Color.yellow;
                result.StatusLabel = "NEEDS WORK";
            }
            else
            {
                result.Color = Color.green;
                result.StatusLabel = "OK";
            }

            result.Notes = notes;
            result.MissingPrefabs = missingPrefabs;
            return result;
        }

        /// <summary>
        /// Contains analysis results for a character including validation status.
        /// </summary>
        private class CharacterAnalysis
        {
            public string Name;
            public Color Color = Color.gray;
            public string StatusLabel = "";
            public List<string> Notes = new List<string>();
            public bool IsCritical = false;
            public List<Object> MissingPrefabs = new List<Object>();
        }

        private void BuildRosterIndices()
        {
            _allRosterCharacters.Clear();
            _nonPersistentRosterCharacters.Clear();
            _persistentPlayerRosterCharacters.Clear();
            _rosterLocations.Clear();

            // Search for PlayerTeamRoster assets (treated as persistent player rosters)
            var playerGuids = AssetDatabase.FindAssets("t:PlayerTeamRoster");
            foreach (var g in playerGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<PlayerTeamRoster>(path);
                if (asset == null)
                {
                    continue;
                }

                var placements = asset.characters;
                if (placements == null)
                {
                    continue;
                }

                foreach (var up in placements)
                {
                    if (up == null || up.CharacterData == null)
                    {
                        continue;
                    }

                    _allRosterCharacters.Add(up.CharacterData);
                    _persistentPlayerRosterCharacters.Add(up.CharacterData);
                    if (!_rosterLocations.TryGetValue(up.CharacterData, out var list))
                    {
                        list = new List<string>();
                        _rosterLocations[up.CharacterData] = list;
                    }
                    list.Add($"PlayerTeamRoster:{asset.name}");
                }
            }

            // Search GenericRosters (non-persistent rosters)
            var genericGuids = AssetDatabase.FindAssets("t:GenericRoster");
            foreach (var g in genericGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<GenericRoster>(path);
                if (asset == null)
                {
                    continue;
                }

                var placements = asset.characters;
                if (placements == null)
                {
                    continue;
                }

                foreach (var up in placements)
                {
                    if (up == null || up.CharacterData == null)
                    {
                        continue;
                    }

                    _allRosterCharacters.Add(up.CharacterData);
                    _nonPersistentRosterCharacters.Add(up.CharacterData);
                    if (!_rosterLocations.TryGetValue(up.CharacterData, out var list))
                    {
                        list = new List<string>();
                        _rosterLocations[up.CharacterData] = list;
                    }
                    list.Add($"GenericRoster:{asset.name}");
                }
            }

            // --- NEW: scan EnemySupervisor components (prefabs/assets) for GenericEnemyStartingPlacement references ---
            _genericEnemyPlacementCharacters.Clear();
            var supervisors = Resources.FindObjectsOfTypeAll<EnemySupervisor>();
            foreach (var sup in supervisors)
            {
                if (sup == null || sup.GenericEnemyStartingPlacements.placements == null)
                    continue;

                foreach (var placement in sup.GenericEnemyStartingPlacements.placements)
                {
                    if (placement.Enemy == null)
                        continue;

                    _genericEnemyPlacementCharacters.Add(placement.Enemy);

                    if (!_rosterLocations.TryGetValue(placement.Enemy, out var list))
                    {
                        list = new List<string>();
                        _rosterLocations[placement.Enemy] = list;
                    }
                    list.Add($"EnemySupervisor:{sup.name}");
                }
            }
        }

        private void ShowNotesPopup(CharacterData character)
        {
            var analysis = AnalyzeCharacter(character);
            var title = $"{character.DisplayName ?? character.name} — {analysis.StatusLabel}";
            if (analysis.Notes == null || analysis.Notes.Count == 0)
            {
                EditorUtility.DisplayDialog(title, "No issues found. Everything looks good.", "OK");
                return;
            }

            // Notes are aggregated critical -> warn -> yellow in AnalyzeCharacter, show them in that order.
            var msg = string.Join("\n", analysis.Notes.Select(n => $"- {n}"));
            if (analysis.MissingPrefabs != null && analysis.MissingPrefabs.Count > 0)
            {
                msg =
                    msg
                    + "\n\nMissing prefabs: "
                    + string.Join(
                        ", ",
                        analysis.MissingPrefabs.Select(o => o.name ?? o.ToString())
                    );
            }

            EditorUtility.DisplayDialog(title, msg, "OK");
        }
    }
}
#endif
