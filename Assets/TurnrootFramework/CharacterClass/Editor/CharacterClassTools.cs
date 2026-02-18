#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Turnroot.GameSettings;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;

namespace Turnroot.CharacterClass.Editor
{
    /// <summary>
    /// Project tools for CharacterClass assets (editor-only).
    /// - Repair Default Stats: ensures all ClassStats lists match the project's default stat types
    ///   (adds missing entries, preserves existing values, removes extras).
    /// </summary>
    public static class CharacterClassTools
    {
        [MenuItem("Tools/Turnroot/Character Classes/Repair Default Stats")]
        public static void RepairDefaultStatsMenu()
        {
            var gs = GameplayGeneralSettings.Instance;
            if (gs == null)
            {
                EditorUtility.DisplayDialog(
                    "Repair Default Stats",
                    "GameplayGeneralSettings asset not found in Resources/GameSettings/. Create one first.",
                    "OK"
                );
                return;
            }

            // Determine target assets: selected CharacterClassData assets, otherwise all in project
            var targets = Selection
                .objects.Where(o => o is CharacterClassData)
                .Select(o => o as CharacterClassData)
                .ToList();

            if (targets.Count == 0)
            {
                var guids = AssetDatabase.FindAssets("t:CharacterClassData");
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    var asset = AssetDatabase.LoadAssetAtPath<CharacterClassData>(path);
                    if (asset != null)
                    {
                        targets.Add(asset);
                    }
                }
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Repair Default Stats",
                    "No CharacterClassData assets found.",
                    "OK"
                );
                return;
            }

            int processed = 0;
            int totalAdded = 0;
            int totalRemoved = 0;

            var boundedDefaults = gs.GetDefaultBoundedStatTypes();
            var unboundedDefaults = gs.GetDefaultUnboundedStatTypes();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var cc = targets[i];
                    EditorUtility.DisplayProgressBar(
                        "Repairing Class Stats",
                        cc.name,
                        (float)i / targets.Count
                    );
                    Undo.RecordObject(cc, "Repair Default Stats");

                    int added,
                        removed;
                    RepairClassStats(
                        cc,
                        boundedDefaults,
                        unboundedDefaults,
                        out added,
                        out removed
                    );

                    if (added > 0 || removed > 0)
                    {
                        EditorUtility.SetDirty(cc);
                        totalAdded += added;
                        totalRemoved += removed;
                    }

                    processed++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "Repair Default Stats",
                $"Processed {processed} class(es).\nAdded entries: {totalAdded}\nRemoved entries: {totalRemoved}",
                "OK"
            );
        }

        private static void RepairClassStats(
            CharacterClassData cc,
            BoundedStatType[] boundedDefaults,
            UnboundedStatType[] unboundedDefaults,
            out int added,
            out int removed
        )
        {
            // Use local counters — lambdas/local helpers are not allowed to capture out/ref params.
            int addedLocal = 0;
            int removedLocal = 0;

            var stats = cc.Stats ?? new ClassStats();

            // Helper: rebuild bounded list of StatModifier preserving existing values where possible
            System.Func<List<StatModifier>, StatModifier[]> rebuildBounded = (existing) =>
            {
                var map = new Dictionary<BoundedStatType, StatModifier>();
                if (existing != null)
                {
                    foreach (var e in existing)
                    {
                        if (!map.ContainsKey(e.boundedStatType))
                        {
                            map[e.boundedStatType] = e;
                        }
                    }
                }

                var result = new List<StatModifier>();
                foreach (var t in boundedDefaults)
                {
                    if (map.TryGetValue(t, out var found))
                    {
                        result.Add(found);
                    }
                    else
                    {
                        result.Add(new StatModifier(t, 0f));
                        addedLocal++;
                    }
                }

                // count removed = entries present in existing but not in defaults
                if (existing != null)
                {
                    removedLocal += existing.Count(e =>
                        !boundedDefaults.Contains(e.boundedStatType)
                    );
                }

                return result.ToArray();
            };

            // Helper: rebuild unbounded list
            System.Func<List<UnboundedStatModifier>, UnboundedStatModifier[]> rebuildUnbounded = (
                existing
            ) =>
            {
                var map = new Dictionary<UnboundedStatType, UnboundedStatModifier>();
                if (existing != null)
                {
                    foreach (var e in existing)
                    {
                        if (!map.ContainsKey(e.unboundedStatType))
                        {
                            map[e.unboundedStatType] = e;
                        }
                    }
                }

                var result = new List<UnboundedStatModifier>();
                foreach (var t in unboundedDefaults)
                {
                    if (map.TryGetValue(t, out var found))
                    {
                        result.Add(found);
                    }
                    else
                    {
                        result.Add(new UnboundedStatModifier(t, 0f));
                        addedLocal++;
                    }
                }

                if (existing != null)
                {
                    removedLocal += existing.Count(e =>
                        !unboundedDefaults.Contains(e.unboundedStatType)
                    );
                }

                return result.ToArray();
            };

            // Rebuild all relevant lists
            stats.StatMinimums = new List<StatModifier>(rebuildBounded(stats.StatMinimums));
            stats.StatCaps = new List<StatModifier>(rebuildBounded(stats.StatCaps));
            stats.StatBonuses = new List<StatModifier>(rebuildBounded(stats.StatBonuses));
            stats.ClassChangeBonuses = new List<StatModifier>(
                rebuildBounded(stats.ClassChangeBonuses)
            );

            stats.UnboundedStatMinimums = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.UnboundedStatMinimums)
            );
            stats.UnboundedStatCaps = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.UnboundedStatCaps)
            );
            stats.UnboundedStatBonuses = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.UnboundedStatBonuses)
            );
            stats.GrowthRateModifiers = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.GrowthRateModifiers)
            );
            stats.UnboundedClassChangeBonuses = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.UnboundedClassChangeBonuses)
            );

            // Assign back (in case Stats was null)
            cc.Stats = stats;

            // Return accumulated counts via out params
            added = addedLocal;
            removed = removedLocal;
        }
    }
}
#endif
