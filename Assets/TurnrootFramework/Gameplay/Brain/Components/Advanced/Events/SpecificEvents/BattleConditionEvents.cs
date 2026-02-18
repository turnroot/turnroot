using System;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;

namespace Turnroot.Gameplay.Brain
{
    public partial class Brain
    {
        #region Battle Condition Events

        public event Action<BattleCondition> OnBattleConditionMet;
        public event Action<BattleCondition> OnBattleConditionFailed;

        public void PublishBattleConditionMet(BattleCondition condition) =>
            OnBattleConditionMet?.Invoke(condition);

        public void PublishBattleConditionFailed(BattleCondition condition) =>
            OnBattleConditionFailed?.Invoke(condition);

        #endregion
    }
}
