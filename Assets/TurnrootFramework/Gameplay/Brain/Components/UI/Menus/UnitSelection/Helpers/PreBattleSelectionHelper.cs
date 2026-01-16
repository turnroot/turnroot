using System.Collections.Generic;
using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Helper for computing and applying default pre-battle unit selections.
    /// Centralizes auto-selection rules so UI and pre-battle systems stay consistent.
    /// </summary>
    public static class PreBattleSelectionHelper
    {
        public static HashSet<CharacterData> EnsureDefaultPreBattleSelections(
            Brain brain,
            PlayerTeamRoster persistentRoster,
            PlayerTeamRosterInstance runtimeInstance,
            int maxPlayerTeamUnits,
            List<CharacterData> requiredPlayerUnits
        )
        {
            var result = new HashSet<CharacterData>();
            if (brain == null || persistentRoster == null)
            {
                return result;
            }

            var gw = brain.gamewideContextBrain;
            var ltm = brain.ltm;

            // Determine placements to iterate (runtime placements preferred)
            var placements =
                runtimeInstance != null
                    ? runtimeInstance.GetPlacements()
                    : persistentRoster.characters
                        ?? new Turnroot.Characters.Roster.UnitPlacement[0];

            // 1) Required units (always selected for this battle). Do NOT persist required units to LTM.
            if (requiredPlayerUnits != null)
            {
                foreach (var r in requiredPlayerUnits)
                {
                    if (r != null)
                    {
                        result.Add(r);
                    }
                }
            }

            // 2) Units marked in LTM (player-chosen previously)
            if (ltm != null)
            {
                foreach (var p in placements)
                {
                    if (p == null || p.CharacterData == null)
                    {
                        continue;
                    }

                    var key = LtmKeys.UnitSelectedForBattlePrefix + p.CharacterData.name;
                    bool selected = ltm.RecallBool(key);
                    if (selected)
                    {
                        result.Add(p.CharacterData);
                    }
                }
            }

            // 3) Fill to max using roster order — ONLY on the first initialization. Subsequent runs respect player choices.
            var autoFillAlreadyDone =
                ltm != null && ltm.RecallBool(LtmKeys.UnitSelectionsAutoFilled);

            if (!autoFillAlreadyDone)
            {
                foreach (var p in placements)
                {
                    if (result.Count >= maxPlayerTeamUnits)
                    {
                        break;
                    }

                    if (p == null || p.CharacterData == null)
                    {
                        continue;
                    }

                    if (!result.Contains(p.CharacterData))
                    {
                        result.Add(p.CharacterData);
                    }
                }
            }

            // Apply selections: persist non-required choices into LTM and set runtime instance flags
            foreach (var p in placements)
            {
                if (p == null || p.CharacterData == null)
                {
                    continue;
                }

                var template = p.CharacterData;
                var desired = result.Contains(template);

                // Persist desired selection for non-required units
                if (ltm != null)
                {
                    // Persist using asset name (stable identifier)
                    var key = LtmKeys.UnitSelectedForBattlePrefix + template.name;
                    if (requiredPlayerUnits == null || !requiredPlayerUnits.Contains(template))
                    {
                        ltm.RememberBool(key, desired);
                    }
                }

                // Set runtime instance selection state if instance exists
                CharacterInstance inst = null;
                if (runtimeInstance != null)
                {
                    inst = runtimeInstance.GetInstanceFor(template);
                }

                // fallback: try to find via GamewideContextBrain
                inst ??= gw?.FindInstanceByTemplate(template);

                if (inst != null)
                {
                    if (inst.IsSelectedForBattle != desired)
                    {
                        inst.IsSelectedForBattle = desired;
                        brain.PublishUnitSelectionChanged(inst, desired);
                    }
                }
            }

            // If we performed the one-time auto-fill, mark it in LTM so future inits respect player choices.
            if (!autoFillAlreadyDone && ltm != null)
            {
                ltm.RememberBool(LtmKeys.UnitSelectionsAutoFilled, true);
            }

#if UNITY_EDITOR
            Debug.Log(
                $"PreBattleSelectionHelper: Ensured {result.Count} selected units (required={requiredPlayerUnits?.Count ?? 0}, autoFilled={!autoFillAlreadyDone})."
            );
#endif

            return result;
        }
    }
}
