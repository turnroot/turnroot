using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Characters.Components;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
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
            if (_ltm == null)
            {
                return;
            }

            BattlesWon = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesWon));
            BattlesLost = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesLost));
            BattlesRetreated = Mathf.Max(0, _ltm.RecallInt(LtmKeys.BattlesRetreated));
        }

        private void SaveBattleOutcomeStatistics()
        {
            if (_ltm == null)
            {
                return;
            }

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

        private void IncrementTurnsAliveForFaction(params string[] factionTypes)
        {
            if (_battleBrain == null)
            {
                return;
            }

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
                        _battleBrain.EnemyTeamRoster?.Instances ?? new List<CharacterInstance>()
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

            foreach (var instance in characters)
            {
                instance?.IncrementTurnsAlive();
                instance?.ResetTurnStats();
            }
        }

        #endregion

        #region Battle Statistics Management

        private void InitializeBattleStatistics()
        {
            if (_battleBrain == null)
            {
                TurnrootLogger.Log(
                    "CharactersBrain: BattleBrain not found",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.RecordBattleStart();
            }

            TurnrootLogger.Log($"CharactersBrain: Initialized {allCharacters.Count} characters");
        }

        private void ResetBattleStatistics()
        {
            if (_battleBrain == null)
            {
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            foreach (var instance in allCharacters)
            {
                instance?.ResetBattleStats();
            }
            TurnrootLogger.Log("CharactersBrain: Reset battle statistics");
        }

        private void SaveBattleParticipantsProgress()
        {
            if (_gamewideContextBrain == null || _battleBrain == null)
            {
                TurnrootLogger.Log(
                    "CharactersBrain: Cannot save - missing components",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var allCharacters = GetAllBattleCharacters();
            int savedCount = 0;

            foreach (var character in allCharacters)
            {
                if (character == null)
                {
                    continue;
                }

                character.CurrentClass?.IncrementBattleCount();

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

            if (_battleBrain.EnemyTeamRoster?.Instances != null)
            {
                characters.AddRange(_battleBrain.EnemyTeamRoster.Instances);
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
