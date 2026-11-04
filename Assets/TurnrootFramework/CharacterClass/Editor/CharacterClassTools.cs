#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Stats;
using System;

namespace Turnroot.CharacterClass.Editor
{
    /// <summary>
    /// Project tools for CharacterClass assets (editor-only).
    /// - Repair Default Stats: ensures all ClassStats lists match the fixed stat set
    ///   (adds missing entries, preserves existing values, removes extras).
    /// </summary>
    public static class CharacterClassTools
    {
        [MenuItem("Tools/Turnroot/Character Classes/Repair Default Stats")]
        public static void RepairDefaultStatsMenu()
        {
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

            var classStatTypes = Enum
                .GetValues(typeof(UnboundedStatType))
                .Cast<UnboundedStatType>()
                .Where(statType => statType != UnboundedStatType.Movement);

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
                    RepairClassStats(cc, classStatTypes, out added, out removed);

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
            IEnumerable<UnboundedStatType> classStatTypes,
            out int added,
            out int removed
        )
        {
            // Use local counters — lambdas/local helpers are not allowed to capture out/ref params.
            int addedLocal = 0;
            int removedLocal = 0;

            var stats = cc.Stats ?? new ClassStats();

            // Helper: rebuild a unified UnboundedStatModifier list preserving existing values where possible
            System.Func<
                List<UnboundedStatModifier>,
                IEnumerable<UnboundedStatType>,
                UnboundedStatModifier[]
            > rebuildUnbounded = (existing, allowedTypes) =>
            {
                var map = new Dictionary<UnboundedStatType, UnboundedStatModifier>();
                if (existing != null)
                {
                    foreach (var e in existing)
                    {
                        if (
                            !e.isBounded
                            && allowedTypes.Contains(e.unboundedStatType)
                            && !map.ContainsKey(e.unboundedStatType)
                        )
                        {
                            map[e.unboundedStatType] = e;
                        }
                    }
                }

                var result = new List<UnboundedStatModifier>();
                foreach (var t in allowedTypes)
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
                        !e.isBounded && !allowedTypes.Contains(e.unboundedStatType)
                    );
                }

                // preserve any bounded entries (HP etc.) that were in the existing list
                if (existing != null)
                {
                    foreach (var e in existing)
                    {
                        if (e.isBounded)
                        {
                            result.Add(e);
                        }
                    }
                }

                return result.ToArray();
            };

            // Rebuild all lists using the unified helper
            stats.StatMinimums = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.StatMinimums, classStatTypes)
            );
            stats.StatCaps = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.StatCaps, classStatTypes)
            );
            stats.StatBonuses = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.StatBonuses, classStatTypes)
            );
            stats.GrowthRateModifiers = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.GrowthRateModifiers, classStatTypes)
            );
            // ensure HP growth entry (bounded) is present
            if (
                !stats.GrowthRateModifiers.Exists(g =>
                    g.isBounded && g.boundedStatType == BoundedStatType.Health
                )
            )
            {
                stats.GrowthRateModifiers.Add(
                    new UnboundedStatModifier(BoundedStatType.Health, 0f)
                );
                addedLocal++;
            }
            stats.ClassChangeBonuses = new List<UnboundedStatModifier>(
                rebuildUnbounded(stats.ClassChangeBonuses, classStatTypes)
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
