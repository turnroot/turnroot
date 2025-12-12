using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

/// <summary>
/// Condition to protect specific NPCs from being defeated.
/// </summary>
[Serializable]
public class ProtectNPCsBattleCondition : BattleCondition
{
    [SerializeField]
    public CharacterData[] NPCsToProtect;

    [SerializeField]
    public int MustSurviveCount = 0;

    private readonly SingleValueCache<List<CharacterInstance>> _npcsCache = new();

    public ProtectNPCsBattleCondition(
        string name,
        string description,
        int mustSurviveCount,
        CharacterData[] npcsToProtect
    )
        : base(name, description)
    {
        NPCsToProtect = npcsToProtect ?? Array.Empty<CharacterData>();
    }

    public ProtectNPCsBattleCondition()
        : base("Protect NPCs", "Prevent listed NPCs from being defeated")
    {
        NPCsToProtect = Array.Empty<CharacterData>();
    }

    public override void InvalidateCache() => _npcsCache.Invalidate();

    private List<CharacterInstance> GetTrackedNPCs()
    {
        return _npcsCache.GetOrCompute(() => GetMatchingAlliesAndThirdParty(NPCsToProtect));
    }

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(ProtectNPCsBattleCondition)))
        {
            return;
        }

        if (!ValidationHelper.ValidateNotNullOrEmpty(NPCsToProtect, nameof(NPCsToProtect)))
        {
            Debug.LogWarning("ProtectNPCsBattleCondition: No NPCs specified to protect.");
            return;
        }

        var allPotentialNPCs = GetTrackedNPCs();

        if (!ValidationHelper.ValidateNotNullOrEmpty(allPotentialNPCs, nameof(allPotentialNPCs)))
        {
            Debug.LogWarning("ProtectNPCsBattleCondition: No matching NPCs found in battle.");
            return;
        }

        int aliveCount = allPotentialNPCs.Count(npc => !npc.IsDefeatedInCurrentBattle);
        int requiredSurvivors = MustSurviveCount > 0 ? MustSurviveCount : allPotentialNPCs.Count;

        if (aliveCount < requiredSurvivors)
        {
            ConditionFailed();
        }
    }
}
