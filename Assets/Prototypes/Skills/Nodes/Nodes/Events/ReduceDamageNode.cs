using System.Linq;
using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
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

        [Tooltip("Test value for reduction in editor mode")]
        public float testReduction = 5.0f;

        [Tooltip("Test value for affectAdjacentAllies in editor mode")]
        public bool testAffectAdjacent = false;

        [Tooltip("Is reduction a percentage (true) or flat value (false)?")]
        public bool isPercentage = false;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("ReduceDamage: No context provided");
                return;
            }

            float reduction = GetInputFloat("reductionAmount", testReduction);
            bool shouldAffectAdjacent = GetInputBool("affectAdjacentAllies", testAffectAdjacent);

            // Store in CustomData for combat system to apply during damage calculation
            // Key format: "DamageReduction_{CharacterInstanceId}"
            var reductionData = new { Amount = reduction, IsPercentage = isPercentage };

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.AdjacentUnits == null)
                {
                    Debug.LogWarning("ReduceDamage: No adjacent units available in context");
                    return;
                }

                // Get all adjacent allies using helper method
                var adjacentAllies = context.AdjacentUnits.GetAdjacentAllies(context);

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
                    Debug.Log(
                        $"ReduceDamage: Applied {reduction} {reductionType} damage reduction to {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                    );
                }
                else
                {
                    Debug.LogWarning(
                        "ReduceDamage: No adjacent allies found to apply reduction to"
                    );
                }
            }
            else
            {
                // Affect only the caster
                context.SetCustomData($"DamageReduction_{context.UnitInstance.Id}", reductionData);
                string reductionType = isPercentage ? "%" : "flat";
                Debug.Log($"ReduceDamage: Will take {reduction} {reductionType} less damage");
            }
        }
    }
}
