using Turnroot.Characters;
using Turnroot.Characters.Components.Support;
using Turnroot.CommonAncestors;
using Turnroot.GameSettings;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    /// <summary>
    /// Partial class containing support bonus calculation methods for adjacent ally combat bonuses.
    /// </summary>
    public static partial class DamageCalculator
    {
        #region Support Bonuses
        private static (
            float attackerHit,
            float attackerCrit,
            float targetAvoid,
            float targetDodge
        ) CalculateSupportBonuses(
            BattleContext context,
            CharacterInstance attacker,
            CharacterInstance target
        )
        {
            var settings = LoadSettings();
            if (settings == null)
            {
                return (0f, 0f, 0f, 0f);
            }

            var attackerBonus = AccumulateAdjacentSupport(context, attacker, settings);
            var targetBonus = AccumulateAdjacentSupport(context, target, settings);

            return (attackerBonus.Hit, attackerBonus.Crit, targetBonus.Avoid, targetBonus.Dodge);
        }

        private static GameplayGeneralSettings.SupportBonus AccumulateAdjacentSupport(
            BattleContext context,
            CharacterInstance unit,
            GameplayGeneralSettings settings
        )
        {
            var total = new GameplayGeneralSettings.SupportBonus();
            if (context == null || unit == null || settings == null)
            {
                return total;
            }

            var adjacency =
                (context.Participants.AdjacentUnits?.Center == unit)
                    ? context.Participants.AdjacentUnits
                    : new Locations.Adjacency(unit);

            using var allyIds = PooledHashSet<string>.Get();
            if (context.Participants.Allies != null)
            {
                foreach (var ally in context.Participants.Allies)
                {
                    if (ally != null)
                    {
                        allyIds.HashSet.Add(ally.Id);
                    }
                }
            }

            var adjacentList = ListPool<CharacterInstance>.Get();
            adjacency.GetAllAdjacentNonAlloc(adjacentList);

            foreach (var adjacent in adjacentList)
            {
                if (adjacent == null || adjacent == unit || !allyIds.HashSet.Contains(adjacent.Id))
                {
                    continue;
                }

                var bonus = GetSupportBonusForPair(unit, adjacent, settings);
                total.Hit += bonus.Hit;
                total.Avoid += bonus.Avoid;
                total.Crit += bonus.Crit;
                total.Dodge += bonus.Dodge;
            }

            ListPool<CharacterInstance>.Return(adjacentList);
            return total;
        }

        private static GameplayGeneralSettings.SupportBonus GetSupportBonusForPair(
            CharacterInstance unit,
            CharacterInstance adjacent,
            GameplayGeneralSettings settings
        )
        {
            var rel1 = unit.GetSupportRelationship(adjacent.CharacterTemplate);
            var rel2 = adjacent.GetSupportRelationship(unit.CharacterTemplate);

            int val1 = rel1 != null ? RankValue(rel1.CurrentLevel) : 0;
            int val2 = rel2 != null ? RankValue(rel2.CurrentLevel) : 0;

            int chosenValue = System.Math.Max(val1, val2);
            string rankLetter = RankLetter(chosenValue);

            SupportRelationshipInstance chosenRel = (rel1 != null && val1 >= val2) ? rel1 : rel2;

            return settings.GetSupportBonusForRank(rankLetter);
        }

        private static int RankValue(string rankLetter) =>
            rankLetter switch
            {
                LeveledLetteredField.S => 5,
                LeveledLetteredField.A => 4,
                LeveledLetteredField.B => 3,
                LeveledLetteredField.C => 2,
                LeveledLetteredField.D => 1,
                _ => 0,
            };

        private static string RankLetter(int rankValue) =>
            rankValue switch
            {
                5 => LeveledLetteredField.S,
                4 => LeveledLetteredField.A,
                3 => LeveledLetteredField.B,
                2 => LeveledLetteredField.C,
                1 => LeveledLetteredField.D,
                _ => LeveledLetteredField.E,
            };
        #endregion
    }
}
