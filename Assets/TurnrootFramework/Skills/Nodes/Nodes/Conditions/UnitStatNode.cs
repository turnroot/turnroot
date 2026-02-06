namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Retrieves stat values (current, max, percentage, bonus) for the unit.
    /// </summary>
    [CreateNodeMenu("Conditions/Unit/Unit Stat")]
    [NodeLabel("Gets the current (and if the stat has a max value, the max) stat value of a unit")]
    public class UnitStatNode : StatNodeBase
    {
        protected override ConditionHelpers.CharacterSource CharacterSource =>
            ConditionHelpers.CharacterSource.Unit;
    }
}
