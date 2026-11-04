#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.NPCs;
using UnityEngine;
using UnityEditor;
using System;

namespace Turnroot.EditorTools
{
    public partial class CharacterChecklistWindow
    {
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
            // - Unique characters: recommended to have pairings in the table (0 -> orange)
            // - Generic (non-unique) characters: must NOT have pairings in the table (0 -> green, any -> red)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Support Relationships",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        var table = SupportRelationshipTable.Instance;
                        int pairingCount = 0;
                        if (table?.Pairings != null)
                        {
                            foreach (var p in table.Pairings)
                            {
                                if (p.CharacterA == data || p.CharacterB == data)
                                    pairingCount++;
                            }
                        }

                        if (!data.IsUnique)
                        {
                            if (pairingCount == 0)
                            {
                                r.Color = green;
                            }
                            else
                            {
                                r.Color = red;
                                r.Note =
                                    "Generic characters must not have support pairings in the table";
                            }
                        }
                        else
                        {
                            if (pairingCount == 0)
                            {
                                r.Color = orange;
                                r.Note = "No support relationships in SupportRelationshipTable";
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
                        if (StatsMatchDefaults(data))
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

            // Unbounded stat total sanity check (green 30-40, yellow 20-50, orange 10-50, red outside)
            _checks.Add(
                new CharacterCheckDefinition(
                    "Unbounded Stat Total",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        float total = 0f;
                        if (data.UnboundedStats != null)
                        {
                            foreach (var stat in data.UnboundedStats)
                            {
                                if (
                                    stat == null
                                    || !Enum.IsDefined(
                                        typeof(UnboundedStatType),
                                        stat.StatType
                                    )
                                    || stat.StatType
                                        is UnboundedStatType.Movement
                                            or UnboundedStatType.Charm
                                )
                                {
                                    continue;
                                }
                                total += stat.Current;
                            }
                        }
                        if (total is >= 30f and <= 40f)
                        {
                            r.Color = green;
                        }
                        else if (total is >= 20f and <= 50f)
                        {
                            r.Color = yellow;
                            r.Note = $"Total = {total:0.##}";
                        }
                        else if (total is >= 10f and <= 50f)
                        {
                            r.Color = orange;
                            r.Note = $"Total = {total:0.##}";
                        }
                        else
                        {
                            r.Color = red;
                            r.Note = $"Total = {total:0.##} (outside expected range)";
                        }
                        return r;
                    }
                )
            );

            // Growth rate total check (percentages). Exclude movement and charm.
            _checks.Add(
                new CharacterCheckDefinition(
                    "Growth Rates",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        float total = 0f;
                        if (data.PersonalGrowthRates != null && data.PersonalGrowthRates.Count > 0)
                        {
                            var seen = new HashSet<string>();
                            foreach (var g in data.PersonalGrowthRates)
                            {
                                // Skip stale non-growth entries and charm from this quality heuristic.
                                if (
                                    !g.isBounded
                                    && (
                                        !Enum.IsDefined(
                                            typeof(UnboundedStatType),
                                            g.unboundedStatType
                                        )
                                        || g.unboundedStatType == UnboundedStatType.Movement
                                        || g.unboundedStatType == UnboundedStatType.Charm
                                    )
                                )
                                {
                                    continue;
                                }
                                // bounded HP is now counted in the total; do not skip it
                                // dedupe by stat type in case asset has duplicates
                                string key = g.isBounded
                                    ? "B" + (int)g.boundedStatType
                                    : "U" + (int)g.unboundedStatType;
                                if (seen.Contains(key))
                                {
                                    continue;
                                }

                                seen.Add(key);

                                total += g.value;
                            }
                        }
                        // thresholds in percentage (all shifted +30)
                        if (total is >= 390f and <= 450f)
                        {
                            r.Color = green;
                        }
                        else if (total is >= 350f and <= 490f)
                        {
                            r.Color = yellow;
                            r.Note = $"Total = {total:0.##}%";
                        }
                        else if (total is >= 310f and <= 530f)
                        {
                            r.Color = orange;
                            r.Note = $"Total = {total:0.##}%";
                        }
                        else
                        {
                            r.Color = red;
                            r.Note = $"Total = {total:0.##}%";
                        }
                        return r;
                    }
                )
            );

            // Level cap sanity - ensure bounded Level max isn't absurdly low
            _checks.Add(
                new CharacterCheckDefinition(
                    "Level Cap",
                    data =>
                    {
                        var r = new CharacterCheckResult();
                        var lvl = data.BoundedStats?.Find(s => s.StatType == BoundedStatType.Level);
                        if (lvl == null)
                        {
                            r.Color = red;
                            r.Note = "No level stat";
                        }
                        else if (lvl.Max < 30)
                        {
                            r.Color = red;
                            r.Note = $"Max={lvl.Max}";
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

    }
}
#endif
