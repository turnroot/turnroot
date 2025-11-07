using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Reduce Damage")]
    [NodeLabel("Reduce incoming damage")]
    public class ReduceDamage : SkillNode
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

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("ReduceDamage: No context provided");
                return;
            }

            // Get the reduction value
            float reduction = testReduction;
            var reductionPort = GetInputPort("reductionAmount");
            if (reductionPort != null && reductionPort.IsConnected)
            {
                var inputValue = reductionPort.GetInputValue();
                if (inputValue is FloatValue floatValue)
                {
                    reduction = floatValue.value;
                }
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

            // Store in CustomData for combat system to apply during damage calculation
            // Key format: "DamageReduction_{CharacterInstanceId}"
            var reductionData = new { Amount = reduction, IsPercentage = isPercentage };

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.AdjacentUnits == null || context.AdjacentUnits.Count == 0)
                {
                    Debug.LogWarning("ReduceDamage: No adjacent units available in context");
                    return;
                }

                int affectedCount = 0;

                // Iterate through adjacent units and affect allies
                foreach (var kvp in context.AdjacentUnits)
                {
                    var adjacentUnit = kvp.Value;
                    if (adjacentUnit == null)
                        continue;

                    // Skip if not an ally (compare with Allies list)
                    if (
                        context.Allies == null
                        || !context.Allies.Exists(ally => ally.Id == adjacentUnit.Id)
                    )
                    {
                        continue;
                    }

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
