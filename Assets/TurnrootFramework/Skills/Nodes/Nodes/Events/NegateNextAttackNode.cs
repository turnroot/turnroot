using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Negates the next incoming attack on the unit, preventing all damage.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Negate Next Attack")]
    [NodeLabel("Negate incoming attack damage on unit")]
    public class NegateNextAttackNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Output]
        public ExecutionFlow OutFlow;

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
            context.SetCustomData($"NegateAttacks_{context.Unit.UnitInstance.Id}", attacksToNegate);

            if (allAttacksThisTurn)
            {
                "NegateNextAttack: All attacks this turn will be negated for unit".LogInfo();
            }
            else
            {
                "NegateNextAttack: Next attack will be negated for unit".LogInfo();
            }
        }
    }
}
