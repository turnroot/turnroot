using System.Linq;
using Turnroot.Characters;
using Turnroot.Utilities;

namespace Turnroot.Gameplay.Brain
{
    public partial class GamewideContextBrain
    {
        #region Persistent Player Roster Management
        public PlayerTeamRoster CreateOrRecallGamewidePersistentPlayerRoster()
        {
            if (GamewidePersistentPlayerRoster == null)
            {
                TryLoadAndRecallPersistentPlayerRoster();

                if (GamewidePersistentPlayerRoster == null)
                {
                    TurnrootLogger.Log(
                        "GamewideContextBrain: No GamewidePersistentPlayerRoster assigned",
                        TurnrootLogger.LogLevel.Warning
                    );
                    return null;
                }
            }

            if (_rosterPersistence?.HasPlayerRosterInLTM(GamewidePersistentPlayerRoster) == true)
            {
                _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
            }

            return GamewidePersistentPlayerRoster;
        }

        private void TryLoadAndRecallPersistentPlayerRoster()
        {
            var persistent = Roster.PersistentPlayerRoster.Instance;
            if (persistent == null)
            {
                return;
            }

            GamewidePersistentPlayerRoster = persistent.PlayerRoster;

            if (GamewidePersistentPlayerRoster == null)
            {
                TurnrootLogger.Log(
                    "GamewideContextBrain: PersistentPlayerRoster.asset has no PlayerRoster assigned",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                GamewidePersistentPlayerRoster.Id
            );
            var encoded = _ltm?.Recall(key);

            if (!string.IsNullOrEmpty(encoded))
            {
                var decode =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerRosterSaveData>(
                        this,
                        encoded
                    );
                if (decode.Success && decode.Value != null)
                {
                    var runtimeInstance = _rosterManager?.InstantiatePlayerTeamRoster(
                        GamewidePersistentPlayerRoster
                    );
                    if (runtimeInstance != null)
                    {
                        // Only apply saved placements if the saved roster indicates an ongoing battle
                        // (LastSavedBattleTurn > 1). If the saved roster is from the first turn or
                        // hasn't recorded a turn, prefer current runtime placements (e.g., pre-battle)
                        // which will be saved at battle start.
                        if (decode.Value.LastSavedBattleTurn > 1)
                        {
                            runtimeInstance.ApplyDecodedPlacements(decode.Value.Placements);
                        }
                        else
                        {
                            TurnrootLogger.Log(
                                "GamewideContextBrain: Skipping persisted placements because saved roster is first-turn or empty; using current pre-battle placements",
                                TurnrootLogger.LogLevel.Info
                            );
                        }
                    }
                }
            }

            _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
        }

        private void HandleSavePlayerRosterRequested() =>
            // Default behavior: save using the current turn number (0 if out of battle)
            SavePlayerRoster(Brain.battleBrain?.CurrentTurnNumber ?? 0);

        /// <summary>
        /// Save the player roster to LTM, recording the provided lastSavedBattleTurn.
        /// If lastSavedBattleTurn <= 1, this indicates first-turn placements which will be
        /// preferred over previously saved placements on next load.
        /// </summary>
        public void SavePlayerRoster(int lastSavedBattleTurn)
        {
            if (GamewidePersistentPlayerRoster == null)
            {
                TurnrootLogger.Log(
                    "GamewideContextBrain: No persistent player roster to save",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var runtimeInstance = _rosterManager?.GetPersistentPlayerRosterInstance();
            if (runtimeInstance == null)
            {
                TurnrootLogger.Log(
                    "GamewideContextBrain: No runtime instance available to save",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var saveData = new PlayerRosterSaveData
            {
                RosterId = GamewidePersistentPlayerRoster.Id,
                Placements = runtimeInstance.GetPlacements(),
                CharacterInstances = runtimeInstance.Instances.ToArray(),
                LastSavedBattleTurn = lastSavedBattleTurn,
            };

            var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, saveData);
            if (!encode.Success)
            {
                TurnrootLogger.Log(
                    $"GamewideContextBrain: Failed to encode player roster: {encode.Error}",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                GamewidePersistentPlayerRoster.Id
            );
            _ltm?.Remember(key, encode.Value);
            _rosterPersistence?.RegisterPlayerRoster(GamewidePersistentPlayerRoster);
            TurnrootLogger.Log(
                $"GamewideContextBrain: Saved player roster (LastSavedBattleTurn={lastSavedBattleTurn})"
            );
        }

        /// <summary>
        /// Returns the LastSavedBattleTurn recorded in the persisted roster, or 0 if none.
        /// </summary>
        public int GetSavedPlayerRosterLastBattleTurn()
        {
            if (GamewidePersistentPlayerRoster == null || _ltm == null)
            {
                return 0;
            }

            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                GamewidePersistentPlayerRoster.Id
            );
            var encoded = _ltm?.Recall(key);
            if (string.IsNullOrEmpty(encoded))
            {
                return 0;
            }

            var decode = GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerRosterSaveData>(
                this,
                encoded
            );
            return decode.Success && decode.Value != null ? decode.Value.LastSavedBattleTurn : 0;
        }

        public CharacterInstance RecallCharacter(CharacterData template) =>
            _characterPersistence?.RecallCharacter(template);

        /// <summary>
        /// Persist a character instance to LongTermMemory. updateIndex indicates whether
        /// the roster/index should be updated (usually false for in-battle saves).
        /// </summary>
        public void PersistCharacter(CharacterInstance instance, bool updateIndex = false) =>
            _characterPersistence?.SaveCharacter(instance, updateIndex);

        /// <summary>
        /// Persist a character only if it is marked as needing persistence (recovered during deserialization).
        /// Clears the flag on success and returns true if a persist occurred.
        /// </summary>
        public bool PersistIfNeeded(CharacterInstance instance, bool updateIndex = false)
        {
            if (instance == null || !instance.NeedsPersist)
            {
                return false;
            }

            PersistCharacter(instance, updateIndex);
            instance.NeedsPersist = false;
            TurnrootLogger.Log(
                $"GamewideContextBrain: Persisted repaired character {instance.Id}",
                TurnrootLogger.LogLevel.Info
            );
            return true;
        }
        #endregion
    }
}
