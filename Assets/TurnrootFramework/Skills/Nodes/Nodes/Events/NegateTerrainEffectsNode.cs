using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
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

        [Output]
        public ExecutionFlow OutFlow;

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            var unit = context.Unit.UnitInstance;
            if (!ValidationHelper.ValidateNotNull(unit, nameof(unit)))
            {
                return;
            }

            // Per-unit key: attacker having this flag causes the defender's terrain avoid bonus
            // to be ignored when calculating hit chance for this attacker's attacks.
            // Key is read in DamageCalculator.CalculateHitChance.
            // TODO: the movement-cost side (e.g. marsh penalties) is handled by the
            // pathfinding system and should be wired to this flag there too.
            context.SetCustomData($"NegateTerrainEffects_{unit.Id}", true);

            $"NegateTerrainEffects: {unit.CharacterTemplate.DisplayName} will ignore target terrain avoid this exchange".LogInfo();
        }
    }
}
