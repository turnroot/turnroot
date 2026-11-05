using System;
using System.Linq;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Groups multiple battle conditions with AND/OR logic for complex victory requirements.
    /// </summary>
    [Serializable]
    public class ConditionalGroupBattleCondition : BattleCondition
    {
        public enum GroupMode
        {
            AllMustPass,
            AnyCanPass,
        }

        [SerializeField]
        public string[] ChildConditionNames;

        [NonSerialized]
        public BattleCondition[] ChildConditions;

        [SerializeField]
        public GroupMode Mode = GroupMode.AllMustPass;

        public ConditionalGroupBattleCondition()
            : base("Conditional Group", "Combine multiple conditions with AND/OR logic")
        {
            ChildConditionNames = Array.Empty<string>();
            ChildConditions = Array.Empty<BattleCondition>();
        }

        public OperationResult ResolveChildConditions(BattleCondition[] allConditions)
        {
            if (ChildConditionNames == null || ChildConditionNames.Length == 0)
            {
                ChildConditions = Array.Empty<BattleCondition>();
                return OperationResult.Successful();
            }

            if (allConditions == null)
            {
                return OperationResult.Failure("Cannot resolve child conditions from a null condition list");
            }

            ChildConditions = ChildConditionNames
                .Select(name => Array.Find(allConditions, condition =>
                    condition != null && condition.Name == name))
                .Where(condition => condition != null)
                .ToArray();
            return OperationResult.Successful();
        }

        public void CheckCondition()
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            if (ChildConditions == null || ChildConditions.Length == 0)
            {
                return;
            }

            if (Mode == GroupMode.AllMustPass)
            {
                foreach (var c in ChildConditions)
                {
                    if (c == null || !c.IsSatisfied)
                    {
                        return;
                    }
                }
                ConditionMet();
            }
            else // AnyCanPass
            {
                foreach (var c in ChildConditions)
                {
                    if (c != null && c.IsSatisfied)
                    {
                        ConditionMet();
                        return;
                    }
                }
            }
        }
    }
}
