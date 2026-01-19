using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
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

        public void ResolveChildConditions(BattleCondition[] allConditions)
        {
            if (ChildConditionNames == null || ChildConditionNames.Length == 0)
            {
                return;
            }

            var list = new List<BattleCondition>();
            foreach (var name in ChildConditionNames)
            {
                var match = Array.Find(allConditions, c => c != null && c.Name == name);
                if (match != null)
                {
                    list.Add(match);
                }
            }
            ChildConditions = list.ToArray();
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
