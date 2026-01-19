using System;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to have all allies cross a specific row or column.
    /// </summary>
    [Serializable]
    public class AllAlliesCrossRowOrColumnBattleCondition : BattleCondition
    {
        [SerializeField]
        public int RowOrColumnIndex;

        [SerializeField]
        public bool IsRow;

        public AllAlliesCrossRowOrColumnBattleCondition(
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

        public AllAlliesCrossRowOrColumnBattleCondition()
            : base(
                "All Allies Cross Row/Column",
                "Have all allies cross the specified row or column"
            )
        {
            RowOrColumnIndex = 0;
            IsRow = true;
        }

        public void CheckCondition()
        {
            if (!ValidateBattleContext(nameof(AllAlliesCrossRowOrColumnBattleCondition)))
            {
                return;
            }

            bool allCrossed = true;
            foreach (var ally in battleContext.Participants.Allies)
            {
                if (ally == null)
                {
                    continue;
                }

                var position = ally.MapGridPosition;
                bool hasCrossed = IsRow
                    ? position.y >= RowOrColumnIndex
                    : position.x >= RowOrColumnIndex;

                if (!hasCrossed)
                {
                    allCrossed = false;
                    break;
                }
            }

            if (allCrossed && battleContext.Participants.Allies.Count > 0)
            {
                ConditionMet();
            }
        }
    }
}
