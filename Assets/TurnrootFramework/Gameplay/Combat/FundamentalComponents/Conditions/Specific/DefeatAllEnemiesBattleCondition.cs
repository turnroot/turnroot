using System.Linq;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Conditions.Specific
{
    /// <summary>
    /// Condition to defeat all enemies.
    /// </summary>
    public class DefeatAllEnemiesBattleCondition : BattleCondition
    {
        public DefeatAllEnemiesBattleCondition()
            : base("Defeat All Enemies", "Defeat all enemy units on the battlefield") { }

        public void CheckCondition()
        {
            if (!AreRequirementsMet())
            {
                return;
            }

            if (!ValidateBattleContext(nameof(DefeatAllEnemiesBattleCondition)))
            {
                return;
            }

            if (battleContext.Participants.Targets.All(enemy => enemy.IsDefeatedInCurrentBattle))
            {
                ConditionMet();
            }
        }
    }
}
