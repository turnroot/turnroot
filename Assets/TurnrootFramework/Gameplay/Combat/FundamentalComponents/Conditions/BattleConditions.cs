using System;
using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
/// <summary>
/// Base class for battle conditions.
/// Provides common functionality for all battle conditions including caching, validation, and helper methods.
/// Specific condition implementations are in the Specific folder.
/// </summary>
public class BattleCondition
{
    public BattleContext battleContext;

    [HideInInspector]
    public string Name;

    [SerializeField]
    public string Description;

    private bool IsActive { get; set; } = false;

    public UnityEvent OnConditionMet;
    public UnityEvent OnConditionActive;
    public UnityEvent OnConditionInactive;
    public UnityEvent OnConditionFailed;

    // Optional list of names of other conditions that must be satisfied before this one can evaluate
    [SerializeField]
    public string[] RequiredConditionNames;

    // Runtime-resolved references (not serialized)
    [NonSerialized]
    public BattleCondition[] RequiredConditions;

    // Indicates whether this condition has been satisfied (ConditionMet was called)
    public bool IsSatisfied { get; private set; } = false;

    public BattleCondition(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void ActivateCondition()
    {
        IsActive = true;
        OnConditionActive?.Invoke();
    }

    public void DeactivateCondition()
    {
        IsActive = false;
        OnConditionInactive?.Invoke();
    }

    public void ConditionMet()
    {
        IsSatisfied = true;
        OnConditionMet?.Invoke();
        // Publish to Brain for centralized event handling
        battleContext.Brain.PublishBattleConditionMet(this);
    }

    public void ConditionFailed()
    {
        IsSatisfied = false;
        OnConditionFailed?.Invoke();
        // Publish to Brain for centralized event handling
        battleContext.Brain.PublishBattleConditionFailed(this);
    }

    /// <summary>
    /// Returns true if all required conditions are satisfied (or if none are required).
    /// </summary>
    public bool AreRequirementsMet()
    {
        if (RequiredConditions == null || RequiredConditions.Length == 0)
        {
            return true;
        }

        foreach (var rc in RequiredConditions)
        {
            if (rc == null || !rc.IsSatisfied)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Resolves required condition references by matching names from the provided list of all conditions.
    /// Should be called by the owning `BattleGameObject` during connect/initialization.
    /// </summary>
    public void ResolveRequiredConditions(BattleCondition[] allConditions)
    {
        if (RequiredConditionNames == null || RequiredConditionNames.Length == 0)
        {
            return;
        }

        var list = new List<BattleCondition>();
        foreach (var name in RequiredConditionNames)
        {
            var match = Array.Find(allConditions, c => c != null && c.Name == name);
            if (match != null)
            {
                list.Add(match);
            }
        }
        RequiredConditions = list.ToArray();
    }

    /// <summary>
    /// Invalidate all caches. Override in derived classes that use caching.
    /// Base implementation does nothing - conditions without caching don't need to override this.
    /// Call this when units are added/removed from battle or when condition state needs refresh.
    /// </summary>
    public virtual void InvalidateCache() { }

    /// <summary>
    /// Helper to validate battleContext availability.
    /// </summary>
    protected bool ValidateBattleContext(string conditionName)
    {
        if (battleContext == null)
        {
            Debug.LogWarning(
                $"{conditionName}: BattleContext not available. Ensure battle is active."
            );
            return false;
        }
        return true;
    }

    /// <summary>
    /// Get units matching specified templates from a list.
    /// </summary>
    protected List<CharacterInstance> GetMatchingUnits(
        IEnumerable<CharacterInstance> units,
        CharacterData[] templates
    ) => units.Where(u => templates.Contains(u.CharacterTemplate)).ToList();

    /// <summary>
    /// Get units matching specified templates from Allies and ThirdParty.
    /// </summary>
    protected List<CharacterInstance> GetMatchingAlliesAndThirdParty(CharacterData[] templates)
    {
        return battleContext
            .Participants.Allies.Concat(battleContext.Participants.ThirdParty)
            .Where(u => templates.Contains(u.CharacterTemplate))
            .ToList();
    }
}
