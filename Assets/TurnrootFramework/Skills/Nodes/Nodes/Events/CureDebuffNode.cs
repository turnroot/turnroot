using Turnroot.Characters;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    [CreateNodeMenu("Events/Defensive/Cure Debuff")]
    [NodeLabel("Remove status debuffs from ally")]
    public class CureDebuffNode : SkillNode
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

        [Tooltip("If CureMode is SpecificDebuff, which debuff type to cure")]
        public StatusEffectType specificDebuffType;

        [Tooltip("Alternative: Cure by name string (used if specificDebuffType is not set)")]
        public string debuffName = "";

        public override void Execute(BattleContext context)
        {
            if (!ValidateContext(context))
            {
                return;
            }

            bool shouldAffectAdjacent = GetInputBool("affectAdjacentAllies", testAffectAdjacent);

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.Participants.AdjacentUnits == null)
                {
                    Debug.LogWarning("CureDebuff: No adjacent units available in context");
                    return;
                }

                // Get all adjacent allies using non-allocating method
                var adjacentAllies = ListPool<CharacterInstance>.Get();
                context.Participants.AdjacentUnits.GetAdjacentAlliesNonAlloc(
                    context,
                    adjacentAllies
                );

                int affectedCount = 0;
                foreach (var adjacentUnit in adjacentAllies)
                {
                    int removed = CureDebuffsFromCharacter(adjacentUnit);
                    if (removed > 0)
                    {
                        affectedCount++;
                    }
                }

                string cureText = GetCureDescription();
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

                ListPool<CharacterInstance>.Return(adjacentAllies);
            }
            else
            {
                // Cure caster or first target
                var target =
                    context.Participants.Targets != null && context.Participants.Targets.Count > 0
                        ? context.Participants.Targets[0]
                        : context.Unit.UnitInstance;

                int removed = CureDebuffsFromCharacter(target);
                string cureText = GetCureDescription();
                Debug.Log($"CureDebuff: Cured {cureText} from target ({removed} effects removed)");
            }
        }

        private int CureDebuffsFromCharacter(CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            int removed;
            if (cureMode == CureMode.AllDebuffs)
            {
                removed = character.RemoveAllDebuffs();
            }
            else if (specificDebuffType != null)
            {
                removed = character.RemoveStatusEffectsByType(specificDebuffType);
            }
            else if (!string.IsNullOrEmpty(debuffName))
            {
                // Try to remove by name
                var effect = character.GetStatusEffectByName(debuffName);
                if (effect != null)
                {
                    character.RemoveStatusEffect(effect);
                    removed = 1;
                }
                else
                {
                    removed = 0;
                }
            }
            else
            {
                removed = 0;
            }

            return removed;
        }

        private string GetCureDescription()
        {
            return cureMode == CureMode.AllDebuffs ? "all debuffs"
                : specificDebuffType != null ? specificDebuffType.DisplayName
                : !string.IsNullOrEmpty(debuffName) ? debuffName
                : "specific debuff";
        }
    }

    public enum CureMode
    {
        AllDebuffs, // Remove all status debuffs
        SpecificDebuff, // Remove only specific debuff type
    }
}
