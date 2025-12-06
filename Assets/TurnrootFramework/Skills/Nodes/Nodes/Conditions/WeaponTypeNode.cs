using Turnroot.Skills.Nodes;
using UnityEngine;
using XNode;

namespace Turnroot.Skills.Nodes.Conditions
{
    [CreateNodeMenu("Conditions/Weapon/Weapon Type")]
    [NodeLabel("Gets the weapon type information")]
    public class WeaponTypeNode : SkillNode
    {
        [Output]
        public StringValue TypeName;

        [Output]
        public BoolValue IsSword;

        [Output]
        public BoolValue IsLance;

        [Output]
        public BoolValue IsAxe;

        [Output]
        public BoolValue IsBow;

        [Output]
        public BoolValue IsTome;

        [Output]
        public BoolValue IsStaff;

        [Output]
        public BoolValue IsDagger;

        [Output]
        public BoolValue IsDragonstone;

        [Output]
        public BoolValue IsBeaststone;

        public override object GetValue(NodePort port)
        {
            var skillGraph = graph as SkillGraph;
            if (skillGraph == null || !Application.isPlaying)
            {
                // In editor / preview mode there is no runtime context to query —
                // return neutral defaults. When an item/weapon system exists the
                // runtime branch should fetch the equipped weapon and compare its
                // WeaponType (ScriptableObject) using WeaponTypeHelpers.Equals.
                return port.fieldName switch
                {
                    "TypeName" => new StringValue { value = string.Empty },
                    "IsSword" => new BoolValue { value = false },
                    "IsLance" => new BoolValue { value = false },
                    "IsAxe" => new BoolValue { value = false },
                    "IsBow" => new BoolValue { value = false },
                    "IsTome" => new BoolValue { value = false },
                    "IsStaff" => new BoolValue { value = false },
                    "IsDagger" => new BoolValue { value = false },
                    "IsDragonstone" => new BoolValue { value = false },
                    "IsBeaststone" => new BoolValue { value = false },
                    _ => null,
                };
            }

            var context = GetContextFromGraph(skillGraph);
            if (context == null || context.UnitInstance == null)
            {
                Debug.LogWarning("WeaponType: Could not retrieve context or unit from graph");
                return port.fieldName switch
                {
                    "TypeName" => new StringValue { value = "" },
                    _ => new BoolValue { value = false },
                };
            }

            // TODO: Implement weapon type retrieval from equipped weapon when the
            // item/weapon system is added. Example implementation would obtain the
            // equipped weapon and compare its WeaponType ScriptableObject to a
            // chosen WeaponType via WeaponTypeHelpers.Equals(...) or by comparing
            // IDs.

            return port.fieldName switch
            {
                "TypeName" => new StringValue { value = "" },
                _ => new BoolValue { value = false },
            };
        }
    }
}
