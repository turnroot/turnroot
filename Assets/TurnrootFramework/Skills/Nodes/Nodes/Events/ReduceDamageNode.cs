using Turnroot.Characters;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Reduces incoming damage on the unit or adjacent allies by a flat amount or percentage.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Reduce Damage")]
    [NodeLabel("Reduce incoming damage")]
    public class ReduceDamageNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The amount to reduce damage by")]
        public FloatValue reductionAmount;

        [Input]
        [Tooltip("If true, affects adjacent allies; if false, only caster")]
        public BoolValue affectAdjacentAllies;

        [Tooltip("Is reduction a percentage (true) or flat value (false)?")]
        public bool isPercentage = false;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var redPort = GetInputPort("reductionAmount");
            if (redPort == null || !redPort.IsConnected)
            {
                Debug.LogWarning("ReduceDamageNode: 'reductionAmount' input not provided");
                return;
            }
            float reduction = GetInputFloat("reductionAmount", 0f);
            bool shouldAffectAdjacent = false;
            var adjPort = GetInputPort("affectAdjacentAllies");
            if (adjPort != null && adjPort.IsConnected)
            {
                shouldAffectAdjacent = GetInputBool("affectAdjacentAllies", false);
            }

            // Store in CustomData for combat system to apply during damage calculation
            // Key format: "DamageReduction_{CharacterInstanceId}"
            var reductionData = new { Amount = reduction, IsPercentage = isPercentage };

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.Participants.AdjacentUnits == null)
                {
                    "ReduceDamage: No adjacent units available in context".LogWarning();
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
                    // Apply damage reduction to this adjacent ally
                    context.SetCustomData($"DamageReduction_{adjacentUnit.Id}", reductionData);
                    affectedCount++;
                }

                string reductionType = isPercentage ? "%" : "flat";
                if (affectedCount > 0)
                {
                    $"ReduceDamage: Applied {reduction} {reductionType} damage reduction to {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}".LogInfo();
                }
                else
                {
                    Debug.LogWarning(
                        "ReduceDamage: No adjacent allies found to apply reduction to"
                    );
                }

                ListPool<CharacterInstance>.Return(adjacentAllies);
            }
            else
            {
                // Affect only the caster
                context.SetCustomData(
                    $"DamageReduction_{context.Unit.UnitInstance.Id}",
                    reductionData
                );
                string reductionType = isPercentage ? "%" : "flat";
                $"ReduceDamage: Will take {reduction} {reductionType} less damage".LogInfo();
            }
        }
    }
}
