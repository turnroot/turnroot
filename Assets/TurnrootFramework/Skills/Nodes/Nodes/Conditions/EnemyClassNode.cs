using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    /// <summary>
    /// Condition node that checks the enemy's class type (Infantry, Cavalry, Flying, Armored).
    /// </summary>
    [CreateNodeMenu("Conditions/Enemy/Enemy Class")]
    [NodeLabel("Gets the enemy's class type")]
    public class EnemyClassNode : SkillNode
    {
        [Output]
        public StringValue ClassName;

        [Output]
        public BoolValue IsInfantry;

        [Output]
        public BoolValue IsCavalry;

        [Output]
        public BoolValue IsFlying;

        [Output]
        public BoolValue IsArmored;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            var context = GetContextFromGraph(skillGraph);
            var enemy = ConditionHelpers.GetCharacterFromContext(
                context,
                ConditionHelpers.CharacterSource.Enemy
            );

            if (enemy == null)
            {
                "EnemyClass: Could not retrieve enemy from context".LogWarning();
                return port.fieldName switch
                {
                    "ClassName" => new StringValue { value = "" },
                    _ => new BoolValue { value = false },
                };
            }

            // Get class data from the character's current class
            var classData = enemy.CurrentClass?.ClassData;
            if (classData == null)
            {
                "EnemyClass: Enemy has no class data assigned".LogWarning();
                return port.fieldName switch
                {
                    "ClassName" => new StringValue { value = "" },
                    _ => new BoolValue { value = false },
                };
            }

            var identity = classData.Identity;
            var movementType = identity.MovementType;

            return port.fieldName switch
            {
                "ClassName" => new StringValue { value = identity.ClassName ?? "" },
                "IsInfantry" => new BoolValue { value = movementType == MovementType.Infantry },
                "IsCavalry" => new BoolValue { value = movementType == MovementType.Riding },
                "IsFlying" => new BoolValue { value = movementType == MovementType.Flying },
                "IsArmored" => new BoolValue { value = movementType == MovementType.Armored },
                _ => new BoolValue { value = false },
            };
        }
    }
}
