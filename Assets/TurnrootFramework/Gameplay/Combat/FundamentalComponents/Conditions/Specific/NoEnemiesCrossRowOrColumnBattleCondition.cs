using System;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific
{
    /// <summary>
    /// Condition to have no enemies cross a specific row or column.
    /// </summary>
    [Serializable]
    public class NoEnemiesCrossRowOrColumnBattleCondition : BattleCondition
    {
        [SerializeField]
        public int RowOrColumnIndex;

        [SerializeField]
        public bool IsRow;

        public NoEnemiesCrossRowOrColumnBattleCondition(
            string name,
            string description,
            int rowOrColumnIndex,
            bool isRow = true
        )
            : base(name, description)
        {
            RowOrColumnIndex = rowOrColumnIndex;
            IsRow = isRow;
        }

        public NoEnemiesCrossRowOrColumnBattleCondition()
            : base(
                "No Enemies Cross Row/Column",
                "Ensure no enemies cross the specified row or column"
            )
        {
            RowOrColumnIndex = 0;
            IsRow = true;
        }

        public void CheckCondition()
        {
            if (!ValidateBattleContext(nameof(NoEnemiesCrossRowOrColumnBattleCondition)))
            {
                return;
            }

            bool noEnemiesCrossed = true;
            foreach (var enemy in battleContext.Targets)
            {
                if (enemy == null)
                {
                    continue;
                }

                var position = enemy.MapGridPosition;
                bool hasCrossed = IsRow
                    ? position.y >= RowOrColumnIndex
                    : position.x >= RowOrColumnIndex;

                if (hasCrossed)
                {
                    noEnemiesCrossed = false;
                    break;
                }
            }

            if (noEnemiesCrossed)
            {
                ConditionMet();
            }
        }
    }
}
