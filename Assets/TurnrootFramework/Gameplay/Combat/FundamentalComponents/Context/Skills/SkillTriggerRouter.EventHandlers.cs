using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Skills;
using Turnroot.Skills.Nodes;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class SkillTriggerRouter
    {
        #region Evaluation Helpers

        private void OnUnitTurnEndedHandler(CharacterInstance unit) =>
            ExecuteTriggerSkills(unit, _turnEndsSkills, "TurnEnds");

        private void OnUnitMovedHandler(CharacterInstance unit, Vector2Int pos) =>
            ExecuteTriggerSkills(unit, _unitMovesSkills, "UnitMoves");

        /// <summary>
        /// Fires when any unit completes an attack. Executes that unit's UnitAttacksNode skills.
        /// At this point context.Participants.Targets already contains the combat target(s).
        /// </summary>
        private void OnUnitAttacksHandler(CharacterInstance attacker)
        {
            var context = BattleObject?.Context;
            if (context != null)
            {
                // Set IsInitiatingCombat so skill nodes can check whether this unit started combat
                string initiatorId = context.GetCustomData("CombatInitiatorId", string.Empty);
                context.SetCustomData("IsInitiatingCombat", attacker.Id == initiatorId);
            }
            ExecuteTriggerSkills(attacker, _unitAttacksSkills, "UnitAttacks");
        }

        private void OnUnitTurnStartedHandler(CharacterInstance unit)
        {
            // AnyTurnStarts runs every owner's graph for every unit turn start.
            foreach (var skillOwner in _anyTurnStartsSkills.Keys)
            {
                ExecuteTriggerSkills(skillOwner, _anyTurnStartsSkills, "AnyTurnStarts");
            }

            // UnitTurnStarts fires only for the owner whose individual turn is starting.
            ExecuteTriggerSkills(unit, _unitTurnStartsSkills, "UnitTurnStarts");
        }

        /// <summary>
        /// Fires when a unit is set as the last attacker — i.e., it just attacked a target.
        /// If the attacker is an enemy and the defender is a player ally, fire EnemyAttacksNode
        /// skills for the defender with Targets temporarily set to [attacker].
        /// </summary>
        private void OnLastAttackerSetHandler(
            CharacterInstance defender,
            CharacterInstance attacker
        )
        {
            var context = BattleObject?.Context;
            if (context == null || defender == null || attacker == null)
            {
                return;
            }

            // Only fire if an enemy attacked a player unit
            if (!context.IsEnemyUnit(attacker) || !context.IsPlayerControlledUnit(defender))
            {
                return;
            }

            ExecuteTriggerSkillsForTarget(
                context,
                defender,
                _enemyAttacksSkills,
                "EnemyAttacks",
                attacker
            );
        }

        /// <summary>
        /// Fires via the typed event bus when any unit is defeated.
        /// If the killer is a player ally and the defeated unit is an enemy, fire EnemyDefeatedNode
        /// skills for the killer with Targets temporarily set to [defeated unit].
        /// </summary>
        private void OnUnitDefeatedHandler(UnitDefeatedEvent evt)
        {
            var context = BattleObject?.Context;
            if (context == null || evt.Unit == null || evt.Killer == null)
            {
                return;
            }

            // Only fire if a player unit killed an enemy
            if (!context.IsPlayerControlledUnit(evt.Killer) || !context.IsEnemyUnit(evt.Unit))
            {
                return;
            }

            ExecuteTriggerSkillsForTarget(
                context,
                evt.Killer,
                _enemyDefeatedSkills,
                "EnemyDefeated",
                evt.Unit
            );
        }

        /// <summary>
        /// Fires before the first strike of a combat exchange. Clears combat bonuses first,
        /// then fires CombatStartsNode skills for both the attacker and defender.
        /// </summary>
        private void OnCombatStartedHandler(CharacterInstance attacker, CharacterInstance defender)
        {
            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            // Reset combat-scoped bonuses and stale damage-reduction data for both participants.
            // CombatStartsNode skills will write fresh values immediately below.
            attacker?.ClearCombatBonuses();
            defender?.ClearCombatBonuses();
            if (attacker != null)
            {
                context.CustomData.Remove($"DamageReduction_{attacker.Id}");
            }

            if (defender != null)
            {
                context.CustomData.Remove($"DamageReduction_{defender.Id}");
            }

            // Fire CombatStarts skills for attacker (target = defender, is initiating = true)
            if (attacker != null)
            {
                context.SetCustomData("IsInitiatingCombat", true);
                ExecuteTriggerSkillsForTarget(
                    context,
                    attacker,
                    _combatStartSkills,
                    "CombatStarts",
                    defender
                );
            }

            // Fire CombatStarts skills for defender (target = attacker, is initiating = false)
            if (defender != null)
            {
                context.SetCustomData("IsInitiatingCombat", false);
                ExecuteTriggerSkillsForTarget(
                    context,
                    defender,
                    _combatStartSkills,
                    "CombatStarts",
                    attacker
                );
            }
        }

        /// <summary>
        /// Fires after all strikes in a combat exchange resolve. Fires PostCombatNode
        /// skills for both participants, then clears combat-scoped bonuses.
        /// </summary>
        private void OnCombatEndedHandler(CharacterInstance attacker, CharacterInstance defender)
        {
            var context = BattleObject?.Context;
            if (context == null)
            {
                return;
            }

            // Fire PostCombat skills for attacker (target = defender)
            if (attacker != null)
            {
                ExecuteTriggerSkillsForTarget(
                    context,
                    attacker,
                    _postCombatSkills,
                    "PostCombat",
                    defender
                );
            }

            // Fire PostCombat skills for defender (target = attacker)
            if (defender != null)
            {
                ExecuteTriggerSkillsForTarget(
                    context,
                    defender,
                    _postCombatSkills,
                    "PostCombat",
                    attacker
                );
            }

            // Clear combat-scoped bonuses after post-combat skills have fired
            attacker?.ClearCombatBonuses();
            defender?.ClearCombatBonuses();
        }

        #endregion
    }
}
