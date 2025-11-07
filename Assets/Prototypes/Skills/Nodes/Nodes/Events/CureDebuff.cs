using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Cure Debuff")]
    [NodeLabel("Remove status debuffs from ally")]
    public class CureDebuff : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, cures adjacent allies; if false, only caster or first target")]
        public BoolValue affectAdjacentAllies;

        [Tooltip("Test value for affectAdjacentAllies in editor mode")]
        public bool testAffectAdjacent = false;

        [Tooltip("Cure all debuffs or specific type?")]
        public CureMode cureMode = CureMode.AllDebuffs;

        [Tooltip("If CureMode is SpecificDebuff, which debuff type to cure (placeholder)")]
        public string debuffTypePlaceholder = "Poison";

        public override void Execute(SkillExecutionContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("CureDebuff: No context provided");
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

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.AdjacentUnits == null || context.AdjacentUnits.Count == 0)
                {
                    Debug.LogWarning("CureDebuff: No adjacent units available in context");
                    return;
                }

                int affectedCount = 0;
                var cureData = new { Mode = cureMode, DebuffType = debuffTypePlaceholder };

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

                    // Apply cure to this adjacent ally
                    context.SetCustomData($"CureDebuff_{adjacentUnit.Id}", cureData);
                    affectedCount++;
                }

                string cureText =
                    cureMode == CureMode.AllDebuffs ? "all debuffs" : debuffTypePlaceholder;
                if (affectedCount > 0)
                {
                    Debug.Log(
                        $"CureDebuff: Cured {cureText} from {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}"
                    );
                }
                else
                {
                    Debug.LogWarning("CureDebuff: No adjacent allies found to cure");
                }
            }
            else
            {
                // Cure caster or first target
                var target =
                    context.Targets != null && context.Targets.Count > 0
                        ? context.Targets[0]
                        : context.UnitInstance;

                var cureData = new { Mode = cureMode, DebuffType = debuffTypePlaceholder };
                context.SetCustomData($"CureDebuff_{target.Id}", cureData);

                string cureText =
                    cureMode == CureMode.AllDebuffs ? "all debuffs" : debuffTypePlaceholder;
                Debug.Log($"CureDebuff: Would cure {cureText} from target");
            }
        }
    }

    public enum CureMode
    {
        AllDebuffs, // Remove all status debuffs
        SpecificDebuff, // Remove only specific debuff type
    }
}
