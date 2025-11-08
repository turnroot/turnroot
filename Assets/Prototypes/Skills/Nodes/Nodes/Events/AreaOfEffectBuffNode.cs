using Assets.Prototypes.Gameplay.Combat.FundamentalComponents.Battles;
using Assets.Prototypes.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Assets.Prototypes.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Area Of Effect Buff")]
    [NodeLabel("Buff adjacent allies in radius")]
    public class AreaOfEffectBuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("The stat change amount (positive for buff, negative for debuff)")]
        public FloatValue changeAmount;

        [Tooltip("Test value for change in editor mode")]
        public float testChange = 5f;

        [Tooltip("Effect radius in tiles")]
        [Range(1, 10)]
        public float radius = 2f;

        [Tooltip("Which stat to buff")]
        public string buffStatPlaceholder = "Strength";

        [Tooltip("Buff duration in turns (0 = permanent)")]
        [Range(0, 10)]
        public int durationTurns = 3;

        public override void Execute(BattleContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("AreaOfEffectBuff: No context provided");
                return;
            }

            if (context.Allies == null || context.Allies.Count == 0)
            {
                Debug.LogWarning("AreaOfEffectBuff: No allies in context");
                return;
            }

            float change = GetInputFloat("changeAmount", testChange);

            // Store buff command in CustomData
            // Combat system will need to:
            // 1. Check distance of each ally from caster
            // 2. Apply buff to allies within radius
            // 3. Track buff duration
            var buffData = new
            {
                CasterId = context.UnitInstance.Id,
                Radius = radius,
                StatName = buffStatPlaceholder,
                ChangeAmount = change,
                Duration = durationTurns,
                AffectedAllies = new System.Collections.Generic.List<string>(), // Will be populated by combat system with ally Ids
            };

            context.SetCustomData("AreaOfEffectBuff", buffData);

            string durationType = durationTurns > 0 ? $"{durationTurns} turns" : "permanent";
            Debug.Log(
                $"AreaOfEffectBuff: Will buff {buffStatPlaceholder} by {change} for allies within {radius} tiles ({durationType})"
            );
        }
    }
}
