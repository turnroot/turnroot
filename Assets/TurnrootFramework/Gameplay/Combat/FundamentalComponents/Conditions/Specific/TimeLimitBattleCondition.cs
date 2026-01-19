using System;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Condition to limit the battle duration by a number of turns.
    /// </summary>
    [Serializable]
    public class TimeLimitBattleCondition : BattleCondition
    {
        [SerializeField]
        public int TurnLimit;
        private int currentTurn = 0;

        public TimeLimitBattleCondition(string name, string description, int turnLimit)
            : base(name, description)
        {
            TurnLimit = turnLimit;
        }

        public TimeLimitBattleCondition()
            : base("Time Limit", "Limit the battle duration")
        {
            TurnLimit = 1;
        }

        public void OnTurnEnd()
        {
            currentTurn++;
            CheckCondition();
        }

        public void CheckCondition()
        {
            if (currentTurn >= TurnLimit)
            {
                ConditionFailed();
            }
        }
    }
}
