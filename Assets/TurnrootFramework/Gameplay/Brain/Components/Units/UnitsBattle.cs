using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Tracks battle statistics, outcomes, and participant progress for characters during and after battles.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    public partial class CharactersBrain : BrainComponent
    {
        #region Battle Statistics

        public int BattlesWon { get; private set; }
        public int BattlesLost { get; private set; }
        public int BattlesRetreated { get; private set; }
        public int TotalBattles => BattlesWon + BattlesLost + BattlesRetreated;

        #endregion
        #region Battle Outcome Statistics

        private void LoadBattleOutcomeStatistics()
        {
            BattlesWon = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesWon));
            BattlesLost = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesLost));
            BattlesRetreated = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesRetreated));
        }

        private void SaveBattleOutcomeStatistics()
        {
            _ltm.RememberInt(LtmKeys.BattlesWon, BattlesWon);
            _ltm.RememberInt(LtmKeys.BattlesLost, BattlesLost);
            _ltm.RememberInt(LtmKeys.BattlesRetreated, BattlesRetreated);
            _ltm.RememberInt(LtmKeys.TotalBattles, TotalBattles);
        }

        private void RecordBattleOutcome(Combat.BattleExitType exitType)
        {
            switch (exitType)
            {
                case Combat.BattleExitType.Victory:
                    BattlesWon++;
                    break;
                case Combat.BattleExitType.Defeat:
                    BattlesLost++;
                    break;
                case Combat.BattleExitType.Retreat:
                    BattlesRetreated++;
                    break;
            }

            SaveBattleOutcomeStatistics();
            TurnrootLogger.Log(
                $"CharactersBrain: Recorded {exitType}. Total: W{BattlesWon}/L{BattlesLost}/R{BattlesRetreated}"
            );
        }

        #endregion

        #region Battle Lifecycle

        private void HandleStartBattle() => InitializeBattleStatistics();

        private void HandleExitBattle(Combat.BattleExitType exitType)
        {
            TurnrootLogger.Log($"CharactersBrain: Handling battle exit - {exitType}");
            RecordBattleOutcome(exitType);

            if (exitType is Combat.BattleExitType.Victory or Combat.BattleExitType.Bookmark)
            {
                SaveBattleParticipantsProgress();
            }

            ResetBattleStatistics();
        }

        #endregion


        #region Turn Phase Handlers

        private void HandlePlayerTurnStarted(CharacterInstance character) =>
            IncrementTurnsAliveForFaction(CharacterWhich.ALLY, CharacterWhich.AVATAR);

        private void HandleEnemyTurnStarted() =>
            IncrementTurnsAliveForFaction(CharacterWhich.ENEMY);

        private void HandleThirdPartyTurnStarted() =>
            IncrementTurnsAliveForFaction(CharacterWhich.NPC);

        private List<CharacterInstance> GetRosterInstancesForFactions(params string[] factionTypes)
        {
            var characters = new List<CharacterInstance>();

            foreach (var factionType in factionTypes)
            {
                if (factionType is CharacterWhich.ALLY or CharacterWhich.AVATAR)
                {
                    characters.AddRange(
                        _battleBrain.PlayerTeamRoster?.Instances ?? new List<CharacterInstance>()
                    );
                }
                else if (factionType == CharacterWhich.ENEMY)
                {
                    characters.AddRange(
                        _battleBrain.BattleObject?.Context?.Participants?.Targets
                            ?? new List<CharacterInstance>()
                    );
                }
                else if (factionType == CharacterWhich.NPC)
                {
                    characters.AddRange(
                        _battleBrain.ThirdPartyTeamRoster?.Instances
                            ?? new List<CharacterInstance>()
                    );
                }
            }

            return characters;
        }

        private void IncrementTurnsAliveForFaction(params string[] factionTypes)
        {
            var characters = GetRosterInstancesForFactions(factionTypes);

            foreach (var instance in characters)
            {
                if (instance == null)
                {
                    continue;
                }

                // Award class mastery per turn (base 1). If the unit recorded a kill in the
                // previous turn, award +1 extra — mirrors "per-turn + kill bonus" behaviour.
                var masteryPoints = 1 + (instance.LastTurnKilledEnemy ? 1 : 0);
                instance.CurrentClass?.IncrementBattleCount(instance, masteryPoints);

                instance.IncrementTurnsAlive();
                instance.ResetTurnStats();
            }
        }

        #endregion

        #region Battle Statistics Management

        private void InitializeBattleStatistics()
        {
            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.RecordBattleStart();
            }
        }

        private void ResetBattleStatistics()
        {
            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.ResetBattleStats();
            }
            TurnrootLogger.Log("CharactersBrain: Reset battle statistics");
        }

        private void SaveBattleParticipantsProgress()
        {
            var allCharacters = GetAllBattleCharacters();
            int savedCount = 0;

            foreach (var character in allCharacters)
            {
                if (character == null)
                {
                    continue;
                }

                if (character.CharacterTemplate?.IsUnique == true)
                {
                    _battleBrain.SaveUniqueCharacterProgress(character);
                    savedCount++;
                }
            }

            TurnrootLogger.Log($"CharactersBrain: Saved {savedCount} unique characters");
        }

        private List<CharacterInstance> GetAllBattleCharacters()
        {
            var characters = new List<CharacterInstance>();

            if (_battleBrain == null)
            {
                return characters;
            }

            if (_battleBrain.PlayerTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.PlayerTeamRoster.Instances);
            }

            var enemyTargets = _battleBrain.BattleObject?.Context?.Participants?.Targets;
            if (enemyTargets != null)
            {
                characters.AddRange(enemyTargets);
            }

            if (_battleBrain.ThirdPartyTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.ThirdPartyTeamRoster.Instances);
            }

            return characters;
        }

        #endregion
    }
}
