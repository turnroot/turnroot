#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;
using UnityEngine;
using UnityEditor;

namespace Turnroot.EditorTools
{
    public class CharacterChecklistWindow : EditorWindow
    {
        private Vector2 _scroll;
        private List<CharacterData> _characters = new List<CharacterData>();
        private string _statusMessage = "";

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
                if (_selectedCharacterIndex >= 0 && _selectedCharacterIndex < _characters.Count)
                {
                    var character = _characters[_selectedCharacterIndex];
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

            // Character columns
            EditorGUILayout.BeginVertical();
            // Header row with character names
            EditorGUILayout.BeginHorizontal();
            foreach (var c in _characters)
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
                for (int j = 0; j < _characters.Count; j++)
                {
                    var ch = _characters[j];
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

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

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
            var orange = new Color(1f, 0.6f, 0f);
            var yellow = Color.yellow;
            var green = Color.green;

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
                        var smr =
                            data.NonBattleOutfitPrefab.GetComponentInChildren<SkinnedMeshRenderer>(
                                true
                            );
                        if (smr == null)
                        {
                            r.Color = red;
                            r.Note = "NonBattleOutfitPrefab missing SkinnedMeshRenderer";
                            return r;
                        }
                        var mesh = smr.sharedMesh;
                        if (mesh == null)
                        {
                            r.Color = red;
                            r.Note = "NonBattleOutfitPrefab missing sharedMesh";
                            return r;
                        }
                        // If character defines blendshapes, ensure mesh contains them
                        var sb = data.Blendshapes.BlendshapeNames;
                        if (sb != null && sb.Length > 0)
                        {
                            var missing = new System.Collections.Generic.List<string>();
                            foreach (var b in sb)
                            {
                                if (mesh.GetBlendShapeIndex(b) < 0)
                                    missing.Add(b);
                            }
                            if (missing.Count > 0)
                            {
                                r.Color = red;
                                r.Note =
                                    $"Outfit missing blendshapes: {string.Join(", ", missing)}";
                                return r;
                            }
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
                            r.Color = green;
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
                            r.Color = green;
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
                            r.Color = green;
                        return r;
                    }
                )
            );

            // Support relationships (warn if zero)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Support Relationships",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        if (
                            data.SupportRelationships == null
                            || data.SupportRelationships.Count == 0
                        )
                        {
                            r.Color = orange;
                            r.Note = "No support relationships";
                        }
                        else
                            r.Color = green;
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
                            r.Color = green;
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
                        var defaultStats = CharacterSettings.DefaultStats;
                        if (defaultStats == null)
                        {
                            r.Color = green; // cannot evaluate without defaults
                            return r;
                        }
                        var defBounded = defaultStats.CreateBoundedStats();
                        var defUnbounded = defaultStats.CreateUnboundedStats();
                        bool boundedMatch = true;
                        bool unboundedMatch = true;
                        if (defBounded.Count != data.BoundedStats.Count)
                            boundedMatch = false;
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
                            unboundedMatch = false;
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
                            r.Color = green;
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
                            r.Color = green;
                        return r;
                    }
                )
            );

            // Colors (yellow if black or white)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Accent/Skin Colors",
                    data =>
                    {
                        var r = new CharacterCheckResult();
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
                                r.Color = yellow;
                                r.Note = "Accent or skin color is black/white";
                                return r;
                            }
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

            // Support relationships (0 -> orange)
            if (data.SupportRelationships == null || data.SupportRelationships.Count == 0)
            {
                warnNotes.Add("No support relationships (recommended)");
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
            var defaultStats = CharacterSettings.DefaultStats;
            if (defaultStats != null)
            {
                var defBounded = defaultStats.CreateBoundedStats();
                var defUnbounded = defaultStats.CreateUnboundedStats();
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
                    yellowNotes.Add("Stats match DefaultCharacterStats (consider customizing)");
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

            if (data.Sprites == null || data.Sprites.Length == 0)
            {
                defaultScore++;
            }

            if (data.Portraits == null || data.Portraits.Count == 0)
            {
                defaultScore++;
            }

            if (defaultScore >= 2)
            {
                yellowNotes.Add("Many fields are default/empty (name/sprites/portraits)");
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

        private class CharacterAnalysis
        {
            public string Name;
            public Color Color = Color.gray;
            public string StatusLabel = "";
            public List<string> Notes = new List<string>();
            public bool IsCritical = false;
            public List<Object> MissingPrefabs = new List<Object>();
        }
    }
}
#endif
