namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Enemy/Enemy Stat")]
    [NodeLabel(
        "Gets the current (and if the stat has a max value, the max) stat value of the enemy (if skill applies to multiple enemies, evaluates on the first targeted)"
    )]
    public class EnemyStatNode : StatNodeBase
    {
        protected override ConditionHelpers.CharacterSource CharacterSource =>
            ConditionHelpers.CharacterSource.Enemy;
    }
}
