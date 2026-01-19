using System;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to deal at least N damage total between all enemies.
    /// </summary>
    [Serializable]
    public class DealMinimumTotalEnemyDamageBattleCondition : BattleCondition
    {
        [SerializeField]
        public int MinTotalDamage;

        private int currentTotalDamage = 0;

        public DealMinimumTotalEnemyDamageBattleCondition(
            string name,
            string description,
            int minTotalDamage
        )
            : base(name, description)
        {
            MinTotalDamage = minTotalDamage;
        }

        public DealMinimumTotalEnemyDamageBattleCondition()
            : base(
                "Deal Minimum Total Enemy Damage",
                "Deal at least the specified total damage across all enemies"
            )
        {
            MinTotalDamage = 0;
        }

        public void OnEnemyDamaged(int damageAmount)
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

            if (currentTotalDamage >= MinTotalDamage)
            {
                ConditionMet();
            }
        }
    }
}
