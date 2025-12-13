using UnityEngine;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Unit/Unit Stat")]
    [NodeLabel("Gets the current (and if the stat has a max value, the max) stat value of a unit")]
    public class UnitStatNode : StatNodeBase
    {
        protected override ConditionHelpers.CharacterSource CharacterSource =>
            ConditionHelpers.CharacterSource.Unit;
    }
}
