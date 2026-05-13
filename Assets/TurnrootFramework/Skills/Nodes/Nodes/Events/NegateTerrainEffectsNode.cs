using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Removes movement penalties or other negative terrain effects from the unit
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Negate Terrain Effects")]
    [NodeLabel("Remove movement or stat penalties from terrain")]
    public class NegateTerrainEffectsNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            context.SetCustomData("NegateTerrainEffects", true);
            "NegateTerrainEffects: flagged unit to ignore terrain penalties (consumed by movement system)".LogInfo();
        }
    }
}
