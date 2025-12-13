using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills.Nodes;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Negate Next Attack")]
    [NodeLabel("Negate incoming attack damage on unit")]
    public class NegateNextAttackNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Tooltip(
            "If true, negates all attacks this combat turn; if false, only next single attack"
        )]
        public bool allAttacksThisTurn = false;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            int attacksToNegate = allAttacksThisTurn ? -1 : 1;
            context.SetCustomData($"NegateAttacks_{context.UnitInstance.Id}", attacksToNegate);

            if (allAttacksThisTurn)
            {
                Debug.Log("NegateNextAttack: All attacks this turn will be negated for unit");
            }
            else
            {
                Debug.Log("NegateNextAttack: Next attack will be negated for unit");
            }
        }
    }
}
