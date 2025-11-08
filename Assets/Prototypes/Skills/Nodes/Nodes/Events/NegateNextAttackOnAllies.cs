using System.Linq;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
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

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("NegateNextAttackOnAllies: No context provided");
                return;
            }

            bool shouldAffectAdjacent = GetInputBool("affectAdjacentAllies", testAffectAdjacent);

            // Determine number of attacks to negate: 1 for single attack, -1 for all this turn
            int attacksToNegate = allAttacksThisTurn ? -1 : 1;

            // Store in CustomData for combat system to check
            // Key format: "NegateAttacks_{CharacterInstanceId}"
            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.AdjacentUnits == null)
                {
                    Debug.LogWarning(
                        "NegateNextAttackOnAllies: No adjacent units available in context"
                    );
                    return;
                }

                // Get all adjacent allies using helper method
                var adjacentAllies = context.AdjacentUnits.GetAdjacentAllies(context);

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
