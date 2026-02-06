using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Negates the next incoming attack on the caster and/or adjacent allies, preventing all damage.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Negate Next Attack On Allies")]
    [NodeLabel("Negate incoming attack damage on allies")]
    public class NegateNextAttackOnAlliesNode : SkillNode
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
            if (!ValidateContext(context))
            {
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
                if (context.Participants.AdjacentUnits == null)
                {
                    Debug.LogWarning(
                        "NegateNextAttackOnAllies: No adjacent units available in context"
                    );
                    return;
                }

                // Get all adjacent allies using non-allocating method
                var adjacentAllies = ListPool<CharacterInstance>.Get();
                context.Participants.AdjacentUnits.GetAdjacentAlliesNonAlloc(
                    context,
                    adjacentAllies
                );

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
                        TurnrootLogger.Log(
                            $"NegateNextAttackOnAllies: Will negate all attacks this turn for {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                        );
                    }
                    else
                    {
                        TurnrootLogger.Log(
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

                ListPool<CharacterInstance>.Return(adjacentAllies);
            }
            else
            {
                // Affect only the caster
                context.SetCustomData(
                    $"NegateAttacks_{context.Unit.UnitInstance.Id}",
                    attacksToNegate
                );
                if (allAttacksThisTurn)
                {
                    TurnrootLogger.Log(
                        "NegateNextAttackOnAllies: All attacks this turn will be negated for caster"
                    );
                }
                else
                {
#if UNITY_EDITOR
                    TurnrootLogger.Log(
                        "NegateNextAttackOnAllies: Next attack will be negated for caster"
                    );
#endif
                }
            }
        }
    }
}
