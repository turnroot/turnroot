#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Turnroot.GameSettings;
using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Characters.CharacterClass;

namespace Turnroot.Characters.Editor
{
    /// <summary>
    /// Editor helpers for CharacterData assets.  Currently provides a repair
    /// menu that synchronizes stat lists and personal growth rates with the
    /// current project defaults, removing stale entries (e.g. from when
    /// "Endurance" was removed) and ensuring the HP growth entry is present.
    /// </summary>
    public static class CharacterDataTools
    {
        [MenuItem("Tools/Turnroot/Characters/Repair Stats & Growth Rates")]
        public static void RepairDefaultStatsMenu()
        {
            var gs = GameplayGeneralSettings.Instance;
            if (gs == null)
            {
                EditorUtility.DisplayDialog(
                    "Repair CharacterData Stats",
                    "GameplayGeneralSettings asset not found in Resources/. Create one first.",
                    "OK"
                );
                return;
            }

            var boundedDefaults = gs.GetDefaultBoundedStatTypes();
            var unboundedDefaults = gs.GetDefaultUnboundedStatTypes();

            // gather targets
            var targets = new List<CharacterData>();
            var guids = AssetDatabase.FindAssets("t:CharacterData");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var asset = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (asset != null)
                {
                    targets.Add(asset);
                }
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Repair CharacterData Stats",
                    "No CharacterData assets found.",
                    "OK"
                );
                return;
            }

            int processed = 0;
            int added = 0;
            int removed = 0;

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    var cd = targets[i];
                    EditorUtility.DisplayProgressBar(
                        "Repairing CharacterData",
                        cd.name,
                        (float)i / targets.Count
                    );
                    Undo.RecordObject(cd, "Repair CharacterData Stats");

                    int a,
                        r;
                    RepairCharacterDataStats(cd, boundedDefaults, unboundedDefaults, out a, out r);
                    added += a;
                    removed += r;
                    if (a > 0 || r > 0)
                    {
                        Debug.Log($"CharacterDataTools: fixed '{cd.name}' (+{a}/-{r})");
                    }

                    if (a > 0 || r > 0)
                    {
                        EditorUtility.SetDirty(cd);
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
                "Repair CharacterData Stats",
                $"Processed {processed} character(s).\nAdded entries: {added}\nRemoved entries: {removed}",
                "OK"
            );
        }

        private static void RepairCharacterDataStats(
            CharacterData cd,
            BoundedStatType[] boundedDefaults,
            UnboundedStatType[] unboundedDefaults,
            out int added,
            out int removed
        )
        {
            // counters we can mutate inside lambdas
            int addedLocal = 0;
            int removedLocal = 0;

            // normalize growth modifiers first (same as sanitisaton logic)
            if (cd.PersonalGrowthRates != null && cd.PersonalGrowthRates.Count > 0)
            {
                var copy = cd.PersonalGrowthRates.ToArray();
                cd.PersonalGrowthRates.Clear();
                foreach (var g in copy)
                {
                    if (g.isBounded)
                    {
                        cd.PersonalGrowthRates.Add(
                            new UnboundedStatModifier(g.boundedStatType, g.value)
                        );
                    }
                    else
                    {
                        cd.PersonalGrowthRates.Add(
                            new UnboundedStatModifier(g.unboundedStatType, g.value)
                        );
                    }
                }
            }

            // helper to rebuild a bounded list preserving values
            System.Func<List<BoundedCharacterStat>, BoundedCharacterStat[]> rebuildBounded =
                existing =>
                {
                    var map = new Dictionary<BoundedStatType, BoundedCharacterStat>();
                    if (existing != null)
                    {
                        foreach (var e in existing)
                        {
                            if (e != null && !map.ContainsKey(e.StatType))
                            {
                                map[e.StatType] = e;
                            }
                        }
                    }

                    var result = new List<BoundedCharacterStat>();
                    foreach (var t in boundedDefaults)
                    {
                        if (map.TryGetValue(t, out var found))
                        {
                            result.Add(found);
                        }
                        else
                        {
                            result.Add(new BoundedCharacterStat(0, 0, 0, t));
                            addedLocal++;
                        }
                    }

                    if (existing != null)
                    {
                        removedLocal += existing.Count(e => !boundedDefaults.Contains(e.StatType));
                    }
                    return result.ToArray();
                };

            // rebuild unbounded stats
            System.Func<List<CharacterStat>, CharacterStat[]> rebuildUnbounded = existing =>
            {
                var map = new Dictionary<UnboundedStatType, CharacterStat>();
                if (existing != null)
                {
                    foreach (var e in existing)
                    {
                        if (e != null && !map.ContainsKey(e.StatType))
                        {
                            map[e.StatType] = e;
                        }
                    }
                }

                var result = new List<CharacterStat>();
                foreach (var t in unboundedDefaults)
                {
                    if (map.TryGetValue(t, out var found))
                    {
                        result.Add(found);
                    }
                    else
                    {
                        result.Add(new CharacterStat(0f, t));
                        addedLocal++;
                    }
                }

                if (existing != null)
                {
                    removedLocal += existing.Count(e => !unboundedDefaults.Contains(e.StatType));
                }
                return result.ToArray();
            };

            // remove any null entries just in case (repairs may fix later)
            cd.BoundedStats?.RemoveAll(s => s == null);
            cd.UnboundedStats?.RemoveAll(s => s == null);

            // modify lists in place because setters are private
            var newBounded = rebuildBounded(cd.BoundedStats);
            cd.BoundedStats.Clear();
            cd.BoundedStats.AddRange(newBounded);
            var newUnbounded = rebuildUnbounded(cd.UnboundedStats);
            cd.UnboundedStats.Clear();
            cd.UnboundedStats.AddRange(newUnbounded);

            // purge again after rebuilding
            cd.BoundedStats.RemoveAll(s => s == null);
            cd.UnboundedStats.RemoveAll(s => s == null);

            // repair personal growth rates in a single, straightforward pass.
            if (cd.PersonalGrowthRates == null)
            {
                var field = typeof(CharacterData).GetField(
                    "<PersonalGrowthRates>k__BackingField",
                    System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.NonPublic
                );
                field?.SetValue(cd, new List<UnboundedStatModifier>());
            }

            if (cd.PersonalGrowthRates != null)
            {
                var original = cd.PersonalGrowthRates.ToArray();
                cd.PersonalGrowthRates.Clear();
                var seenKeys = new HashSet<string>();
                float removedStrengthValue = -1f;

                foreach (var g in original)
                {
                    string key = g.isBounded
                        ? "B" + (int)g.boundedStatType
                        : "U" + (int)g.unboundedStatType;
                    if (seenKeys.Contains(key))
                    {
                        if (!g.isBounded && key == "U" + (int)UnboundedStatType.Strength)
                        {
                            removedStrengthValue = g.value;
                        }

                        removedLocal++;
                        Debug.Log(
                            $"CharacterDataTools: duplicate growth entry {key} (value={g.value}) removed from '{cd.name}'"
                        );
                        continue;
                    }
                    seenKeys.Add(key);
                    cd.PersonalGrowthRates.Add(g); // struct copy retains all flags and value
                }

                // strip out any growths for stats that no longer exist
                cd.PersonalGrowthRates.RemoveAll(g =>
                    !g.isBounded && !unboundedDefaults.Contains(g.unboundedStatType)
                );

                // ensure each unbounded default has an entry
                foreach (var t in unboundedDefaults)
                {
                    if (!cd.PersonalGrowthRates.Any(g => !g.isBounded && g.unboundedStatType == t))
                    {
                        cd.PersonalGrowthRates.Add(new UnboundedStatModifier(t, 0f));
                        addedLocal++;
                    }
                }

                // make sure there's always an HP (bounded) entry
                if (
                    !cd.PersonalGrowthRates.Any(g =>
                        g.isBounded && g.boundedStatType == BoundedStatType.Health
                    )
                )
                {
                    cd.PersonalGrowthRates.Add(
                        new UnboundedStatModifier(BoundedStatType.Health, 0f)
                    );
                    addedLocal++;
                }

                // movement entries are irrelevant for growth; drop them
                removedLocal += cd.PersonalGrowthRates.RemoveAll(g =>
                    !g.isBounded && g.unboundedStatType == UnboundedStatType.Movement
                );

                // if we tossed a duplicate Strength and HP was zero, assume that value belonged to HP
                if (removedStrengthValue >= 0f)
                {
                    var hp = cd.PersonalGrowthRates.Find(g =>
                        g.isBounded && g.boundedStatType == BoundedStatType.Health
                    );
                    if (hp.IsHpGrowth && Mathf.Approximately(hp.value, 0f))
                    {
                        hp.value = removedStrengthValue;
                        Debug.Log(
                            $"CharacterDataTools: restored HP growth value {removedStrengthValue} on '{cd.name}' from discarded duplicate strength."
                        );
                    }
                }
            }

            // assign out parameters
            added = addedLocal;
            removed = removedLocal;
        }
    }
}
#endif
