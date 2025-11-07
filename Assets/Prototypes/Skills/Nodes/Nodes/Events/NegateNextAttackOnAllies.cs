using System.Linq;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Negate Next Attack On Allies")]
    [NodeLabel("Negate incoming attack damage on allies")]
    public class NegateNextAttackOnAllies : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, affects adjacent allies; if false, only caster")]
        public BoolValue affectAdjacentAllies;

        [Tooltip("Test value for affectAdjacentAllies in editor mode")]
        public bool testAffectAdjacent = false;

        [Tooltip(
            "If true, negates all attacks this combat turn; if false, only next single attack"
        )]
        public bool allAttacksThisTurn = false;

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("NegateNextAttackOnAllies: No context provided");
                return;
            }

            // Get the affectAdjacentAllies value
            bool shouldAffectAdjacent = testAffectAdjacent;
            var affectAdjacentPort = GetInputPort("affectAdjacentAllies");
            if (affectAdjacentPort != null && affectAdjacentPort.IsConnected)
            {
                var inputValue = affectAdjacentPort.GetInputValue();
                if (inputValue is BoolValue boolValue)
                {
                    shouldAffectAdjacent = boolValue.value;
                }
            }

            // Determine number of attacks to negate: 1 for single attack, -1 for all this turn
            int attacksToNegate = allAttacksThisTurn ? -1 : 1;

            // Store in CustomData for combat system to check
            // Key format: "NegateAttacks_{CharacterInstanceId}"
            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.AdjacentUnits == null || context.AdjacentUnits.Count == 0)
                {
                    Debug.LogWarning(
                        "NegateNextAttackOnAllies: No adjacent units available in context"
                    );
                    return;
                }

                // Iterate through adjacent units and affect allies
                var adjacentAllies = context.AdjacentUnits
                    .Select(kvp => kvp.Value)
                    .Where(adjacentUnit => adjacentUnit != null && 
                           context.Allies != null && 
                           context.Allies.Exists(ally => ally.Id == adjacentUnit.Id));

                int affectedCount = 0;
                foreach (var adjacentUnit in adjacentAllies)
                {
                    // Apply attack negation to this adjacent ally
                    context.SetCustomData($"NegateAttacks_{adjacentUnit.Id}", attacksToNegate);
                    affectedCount++;
                }

                if (affectedCount > 0)
                {
                    if (allAttacksThisTurn)
                    {
                        Debug.Log(
                            $"NegateNextAttackOnAllies: Will negate all attacks this turn for {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                        );
                    }
                    else
                    {
                        Debug.Log(
                            $"NegateNextAttackOnAllies: Will negate next attack for {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        "NegateNextAttackOnAllies: No adjacent allies found to apply negation to"
                    );
                }
            }
            else
            {
                // Affect only the caster
                context.SetCustomData($"NegateAttacks_{context.UnitInstance.Id}", attacksToNegate);
                if (allAttacksThisTurn)
                {
                    Debug.Log(
                        "NegateNextAttackOnAllies: All attacks this turn will be negated for caster"
                    );
                }
                else
                {
                    Debug.Log("NegateNextAttackOnAllies: Next attack will be negated for caster");
                }
            }
        }
    }
}
