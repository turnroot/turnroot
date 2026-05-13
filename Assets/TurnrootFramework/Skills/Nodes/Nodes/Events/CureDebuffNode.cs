using Turnroot.Characters;
using Turnroot.Characters.StatusEffects;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Events
{
    /// <summary>
    /// Removes status debuffs from the caster or adjacent allied units.
    /// </summary>
    [CreateNodeMenu("Events/Defensive/Cure Debuff")]
    [NodeLabel("Remove status debuffs from ally")]
    public class CureDebuffNode : SkillNode
    {
        [Input]
        public ExecutionFlow executionIn;

        [Input]
        [Tooltip("If true, cures adjacent allies; if false, only caster or first target")]
        public BoolValue affectAdjacentAllies;

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

            bool shouldAffectAdjacent = GetInputBool("affectAdjacentAllies", false);

            if (shouldAffectAdjacent)
            {
                // Get adjacent allies from context
                if (context.Participants.AdjacentUnits == null)
                {
                    "CureDebuff: No adjacent units available in context".LogWarning();
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
                    int removed = CureDebuffsFromCharacter(context, adjacentUnit);
                    if (removed > 0)
                    {
                        affectedCount++;
                    }
                }

                string cureText = GetCureDescription();
                if (affectedCount > 0)
                {
                    $"CureDebuff: Cured {cureText} from {affectedCount} adjacent {(affectedCount == 1 ? "ally" : "allies")}".LogInfo();
                }
                else
                {
                    "CureDebuff: No adjacent allies found to cure".LogWarning();
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

                int removed = CureDebuffsFromCharacter(context, target);
                string cureText = GetCureDescription();
                $"CureDebuff: Cured {cureText} from target ({removed} effects removed)".LogInfo();
            }
        }

        private int CureDebuffsFromCharacter(BattleContext context, CharacterInstance character)
        {
            if (character == null)
            {
                return 0;
            }

            int removed;
            var battleBrain = context.Brain.GetComponent<Gameplay.Brain.BattleBrain>();
            if (cureMode == CureMode.AllDebuffs)
            {
                removed = battleBrain?.RemoveAllDebuffs(character) ?? 0;
            }
            else if (specificDebuffType != null)
            {
                removed =
                    battleBrain?.RemoveStatusEffectsByType(character, specificDebuffType) ?? 0;
            }
            else if (!string.IsNullOrEmpty(debuffName))
            {
                // Try to remove by name
                var effect = character.GetStatusEffectByName(debuffName);
                if (effect != null)
                {
                    removed = battleBrain?.RemoveStatusEffect(character, effect) == true ? 1 : 0;
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

    /// <summary>
    /// Specifies which debuffs should be removed by the CureDebuffNode.
    /// </summary>
    public enum CureMode
    {
        AllDebuffs, // Remove all status debuffs
        SpecificDebuff, // Remove only specific debuff type
    }
}
