using System.Linq;

/// <summary>
/// Condition to defeat all enemies.
/// </summary>
public class DefeatAllEnemiesBattleCondition : BattleCondition
{
    public DefeatAllEnemiesBattleCondition()
        : base("Defeat All Enemies", "Defeat all enemy units on the battlefield") { }

    public void CheckCondition()
    {
        if (!ValidateBattleContext(nameof(DefeatAllEnemiesBattleCondition)))
        {
            return;
        }

        if (battleContext.Targets.All(enemy => enemy.IsDefeatedInCurrentBattle))
        {
            ConditionMet();
        }
    }
}
