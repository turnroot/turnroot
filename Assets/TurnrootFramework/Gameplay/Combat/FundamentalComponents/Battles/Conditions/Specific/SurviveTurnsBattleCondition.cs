using System;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

/// <summary>
/// Condition to survive a certain number of turns.
/// </summary>
[Serializable]
public class SurviveTurnsBattleCondition : BattleCondition
{
    [SerializeField]
    public int TurnsToSurvive;
    private int turnsSurvived = 0;

    public SurviveTurnsBattleCondition(string name, string description, int turnsToSurvive)
        : base(name, description)
    {
        TurnsToSurvive = turnsToSurvive;
    }

    public SurviveTurnsBattleCondition()
        : base("Survive Turns", "Survive the specified number of turns")
    {
        TurnsToSurvive = 1;
    }

    public void OnTurnEnd()
    {
        turnsSurvived++;
        CheckCondition();
    }

    public void CheckCondition()
    {
        if (turnsSurvived >= TurnsToSurvive)
        {
            ConditionMet();
        }
    }
}
