using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Combat/Enemy Would Kill Unit")]
    [NodeLabel("Check if incoming damage would be lethal")]
    public class EnemyWouldKillUnitNode : SkillNode
    {
        [Input]
        [Tooltip("The amount of incoming damage")]
        public FloatValue incomingDamage;

        [Output]
        public BoolValue result;

        [Tooltip("Test value for damage in editor mode")]
        public float testDamage = 50f;

        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "result")
            {
                BoolValue wouldKill = new();

                // In editor mode, use test value
                if (!Application.isPlaying)
                {
                    wouldKill.value = testDamage >= 100f; // Arbitrary test
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
            var damagePort = GetInputPort("incomingDamage");
            if (damagePort == null || !damagePort.IsConnected)
            {
                return testDamage;
            }

            var inputValue = damagePort.GetInputValue();
            if (inputValue is FloatValue damageValue)
            {
                return damageValue.value;
            }

            return testDamage;
        }

        private float GetUnitCurrentHealth(BattleContext context)
        {
            var healthStat = context.Unit.UnitInstance.GetBoundedStat(BoundedStatType.Health);

            if (healthStat == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("EnemyWouldKillUnit: Unit has no Health stat");
#endif
                return -1f; // Sentinel value
            }

            return healthStat.Current;
        }
    }
}
