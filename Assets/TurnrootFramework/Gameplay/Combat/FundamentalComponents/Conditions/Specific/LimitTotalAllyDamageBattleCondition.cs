using System;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to take less than N damage total between all allies.
    /// </summary>
    [Serializable]
    public class LimitTotalAllyDamageBattleCondition : BattleCondition
    {
        [SerializeField]
        public int MaxTotalDamage;

        private int currentTotalDamage = 0;

        public LimitTotalAllyDamageBattleCondition(
            string name,
            string description,
            int maxTotalDamage
        )
            : base(name, description)
        {
            MaxTotalDamage = maxTotalDamage;
        }

        public LimitTotalAllyDamageBattleCondition()
            : base(
                "Limit Total Ally Damage",
                "Take less than the specified total damage across all allies"
            )
        {
            MaxTotalDamage = 0;
        }

        public void OnAllyDamaged(int damageAmount)
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            currentTotalDamage += damageAmount;
            CheckCondition();
        }

        public void CheckCondition()
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            if (currentTotalDamage > MaxTotalDamage)
            {
                ConditionFailed();
            }
        }
    }
}
