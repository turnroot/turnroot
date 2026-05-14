using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Adjusts the weapon advantage percentage in combat calculations.
    /// </summary>
    [CreateNodeMenu("Events/Neutral/Adjust Advantage Percents")]
    [NodeLabel("Adjust Advantage Percents")]
    public class AdjustAdvantagePercentsNode : SkillNode
    {
        [Input]
        public ExecutionFlow In;

        [Output]
        public ExecutionFlow OutFlow;

        [Tooltip("The percent to increase advantage by")]
        [Range(0, 100)]
        public float AddAdvantagePercent;

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

            // Stored on CharacterBattleStats so the static DamageCalculator can read it without
            // needing BattleContext threaded through the full call chain. Cleared by
            // ClearCombatBonuses() at the start of each new combat exchange.
            unit.AddCombatWeaponAdvantageBonus(AddAdvantagePercent);

            $"AdjustAdvantagePercents: Added {AddAdvantagePercent}% weapon-triangle bonus for {unit.CharacterTemplate.DisplayName}".LogInfo();
        }
    }
}
