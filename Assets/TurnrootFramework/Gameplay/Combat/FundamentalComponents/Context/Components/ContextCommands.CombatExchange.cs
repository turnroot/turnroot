using Turnroot.Characters;
using Turnroot.Characters.Stats;
using Turnroot.Gameplay.Brain.Commands;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.Objects;
using Turnroot.GameSettings;
using Turnroot.Skills.Nodes.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext : MonoBehaviour
    {        public OperationResult ExecuteCombatExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon = null
        )
        {
            if (attacker == null || defender == null)
            {
                return OperationResult.Failure(
                    "ExecuteCombatExchange: attacker or defender is null"
                );
            }

            // Store who initiated so IsInitiatingCombatNode and OnUnitAttacks can read it
            SetCustomData("CombatInitiatorId", attacker.Id);

            bool combatStarted = false;
            try
            {
                Brain?.PublishCombatStarted(attacker, defender);
                combatStarted = true;

                bool attackerFirstStrike = GetCustomData<bool>($"FirstStrike_{attacker.Id}");
                bool defenderVantage = GetCustomData<bool>($"Vantage_{defender.Id}");

                OperationResult result = defenderVantage && !attackerFirstStrike
                    ? ResolveVantageExchange(attacker, defender, attackerWeapon)
                    : attackerFirstStrike
                        ? ResolveFirstStrikeExchange(attacker, defender, attackerWeapon)
                        : ResolveStandardExchange(attacker, defender, attackerWeapon);

                // Track combat count for IsFirstCombatOfTurnNode.
                attacker.IncrementCombatCount();
                defender.IncrementCombatCount();
                return result;
            }
            finally
            {
                ClearCombatScopedData(attacker, defender);
                if (combatStarted)
                {
                    Brain?.PublishCombatEnded(attacker, defender);
                }
            }
        }

        private OperationResult ResolveVantageExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            // Vantage: defender counters before the attacker's first strike.
            AttackTarget(defender, attacker);
            if (attacker.IsDefeatedInCurrentBattle)
            {
                return OperationResult.Successful();
            }

            var result = AttackTarget(attacker, defender, attackerWeapon);
            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }

            bool defenderDisabled = GetCustomData<bool>($"DisableFollowup_{defender.Id}");
            if (
                !defender.IsDefeatedInCurrentBattle
                && !defenderDisabled
                && CanFollowUp(defender, attacker)
            )
            {
                AttackTarget(defender, attacker);
            }

            return result;
        }

        private OperationResult ResolveFirstStrikeExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            // First strike: attacker makes their initial and possible follow-up attacks first.
            var result = AttackTarget(attacker, defender, attackerWeapon);
            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }
            return result;
        }

        private OperationResult ResolveStandardExchange(
            CharacterInstance attacker,
            CharacterInstance defender,
            ObjectItemInstance attackerWeapon
        )
        {
            var result = AttackTarget(attacker, defender, attackerWeapon);
            bool defenderDisabled = GetCustomData<bool>($"DisableFollowup_{defender.Id}");

            if (!defender.IsDefeatedInCurrentBattle && !defenderDisabled)
            {
                AttackTarget(defender, attacker);
            }

            if (!attacker.IsDefeatedInCurrentBattle && CanFollowUp(attacker, defender))
            {
                AttackTarget(attacker, defender, attackerWeapon);
            }

            if (
                !defender.IsDefeatedInCurrentBattle
                && !defenderDisabled
                && CanFollowUp(defender, attacker)
            )
            {
                AttackTarget(defender, attacker);
            }

            return result;
        }

        private void ClearCombatScopedData(
            CharacterInstance attacker,
            CharacterInstance defender
        )
        {
            CustomData.Remove($"FirstStrike_{attacker.Id}");
            CustomData.Remove($"FirstStrike_{defender.Id}");
            CustomData.Remove($"Vantage_{attacker.Id}");
            CustomData.Remove($"Vantage_{defender.Id}");
            CustomData.Remove($"DisableFollowup_{attacker.Id}");
            CustomData.Remove($"DisableFollowup_{defender.Id}");
            CustomData.Remove($"GuaranteeFollowup_{attacker.Id}");
            CustomData.Remove($"GuaranteeFollowup_{defender.Id}");
            CustomData.Remove($"SpeedThresholdMod_{attacker.Id}");
            CustomData.Remove($"SpeedThresholdMod_{defender.Id}");
            CustomData.Remove($"NegateTerrainEffects_{attacker.Id}");
            CustomData.Remove($"NegateTerrainEffects_{defender.Id}");
            CustomData.Remove("CombatInitiatorId");
        }

        /// <summary>
        /// Returns true when <paramref name="attacker"/> is fast enough to follow up
        /// (attack twice in one exchange).  Uses SPD difference ≥ 4,
        /// modified by GuaranteeFollowup / DisableFollowup / SpeedThresholdMod CustomData
        /// keys set by skill nodes (ChangeBattleOrderNode, etc.).
        /// </summary>
        private bool CanFollowUp(CharacterInstance attacker, CharacterInstance defender)
        {
            // Skill explicitly guarantees a follow-up (e.g. Brash Assault)
            if (GetCustomData<bool>($"GuaranteeFollowup_{attacker.Id}"))
            {
                return true;
            }

            // Skill explicitly prevents attacker from following up
            if (GetCustomData<bool>($"DisableFollowup_{attacker.Id}"))
            {
                return false;
            }

            var atkSpd = attacker.GetUnboundedStat(UnboundedStatType.Speed)?.Current ?? 0f;
            var defSpd = defender.GetUnboundedStat(UnboundedStatType.Speed)?.Current ?? 0f;

            // SpeedThresholdMod adjusts the 4-SPD gap required (negative = easier, positive = harder)
            float threshold = 4f + GetCustomData<float>($"SpeedThresholdMod_{attacker.Id}");
            return (atkSpd - defSpd) >= threshold;
        }

        /// <summary>
        /// Deals damage to a target unit using the command pattern.
        /// </summary>
        /// <param name="attacker">The attacking unit.</param>
        /// <param name="target">The target unit.</param>
        /// <param name="damage">The damage amount to deal.</param>
        /// <returns>True if the damage was applied.</returns>
    }
}
