using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks if incoming damage would reduce the unit's health to zero or below.
    /// </summary>
    [CreateNodeMenu("Conditions/Combat/Enemy Would Kill Unit")]
    [NodeLabel("Check if incoming damage would be lethal")]
    public class EnemyWouldKillUnitNode : SkillNode
    {
        [Output]
        public BoolValue result;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "result")
            {
                BoolValue wouldKill = new();

                // runtime check only; return false outside play mode
                if (!Application.isPlaying)
                {
                    wouldKill.value = false;
                    return wouldKill;
                }

                // At runtime, check if damage would kill unit
                if (graph is SkillGraph skillGraph)
                {
                    wouldKill.value = CheckIfDamageWouldKillUnit(skillGraph);
                }

                return wouldKill;
            }

            return null;
        }

        private bool CheckIfDamageWouldKillUnit(SkillGraph skillGraph)
        {
            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.Unit.UnitInstance == null)
            {
                return false;
            }

            float damage = GetIncomingDamage();
            float currentHealth = GetUnitCurrentHealth(context);

            if (currentHealth < 0)
            {
                return false; // No health stat found
            }

            // Would this damage reduce HP to 0 or below?
            return (currentHealth - damage) <= 0;
        }

        private float GetIncomingDamage()
        {
            // TODO: Connect to context
            return 1;
        }

        private float GetUnitCurrentHealth(BattleContext context)
        {
            var healthStat = context.Unit.UnitInstance.GetBoundedStat(BoundedStatType.Health);

            if (healthStat == null)
            {
                LoggerExtensions.LogWarning("EnemyWouldKillUnit: Unit has no Health stat");
                return -1f; // Sentinel value
            }

            return healthStat.Current;
        }
    }
}
