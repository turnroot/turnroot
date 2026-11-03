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
    public partial class CharacterChecklistWindow
    {
        private CharacterAnalysis AnalyzeCharacter(CharacterData data)
        {
            var result = new CharacterAnalysis();
            result.Name = data.DisplayName ?? data.name;

            bool hasNonBattleOutfit = data.NonBattleOutfitPrefab != null;
            bool hasHeadAndHands = data.HeadAndHandsPrefab != null;
            bool hasHair = data.HairPrefab != null;

            // Separate note buckets so we can properly color-code severity.
            var criticalNotes = new List<string>();
            var warnNotes = new List<string>();
            var yellowNotes = new List<string>();
            var missingPrefabs = new List<Object>();

            // Validate NonBattleOutfit prefab presence/SMR when needed
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
                else if (smr.sharedMesh == null)
                {
                    criticalNotes.Add("NonBattleOutfitPrefab missing sharedMesh");
                    missingPrefabs.Add(data.NonBattleOutfitPrefab);
                }
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
            {
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
                    if (pairingCount > 0)
                        criticalNotes.Add(
                            "Generic characters must not have support pairings in the table"
                        );
                }
                else
                {
                    if (pairingCount == 0)
                        warnNotes.Add(
                            "No support relationships in SupportRelationshipTable (recommended)"
                        );
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
            if (StatsMatchDefaults(data))
            {
                yellowNotes.Add("Stats match project defaults (consider customizing)");
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

            if (data.PortraitCount == 0)
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

        private bool StatsMatchDefaults(CharacterData data)
        {
            var gs = GameSettings.GameplayGeneralSettings.Instance;
            if (gs == null)
                return false;

            var defBounded = gs.CreateDefaultBoundedStats();
            if (defBounded.Count != data.BoundedStats.Count)
                return false;
            foreach (var d in defBounded)
            {
                var found = data.BoundedStats.Find(s => s.StatType == d.StatType);
                if (found == null || found.CurrentInt != d.CurrentInt || found.MaxInt != d.MaxInt)
                    return false;
            }

            var defUnbounded = gs.CreateDefaultUnboundedStats();
            if (defUnbounded.Count != data.UnboundedStats.Count)
                return false;
            foreach (var d in defUnbounded)
            {
                var found = data.UnboundedStats.Find(s => s.StatType == d.StatType);
                if (found == null || Mathf.RoundToInt(found.Current) != Mathf.RoundToInt(d.Current))
                    return false;
            }

            return true;
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
                {
                    continue;
                }

                foreach (var placement in sup.GenericEnemyStartingPlacements.placements)
                {
                    if (placement.Enemy == null)
                    {
                        continue;
                    }

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
