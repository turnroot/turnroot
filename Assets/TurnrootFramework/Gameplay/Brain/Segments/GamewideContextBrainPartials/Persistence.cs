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
            var encoded = _ltm.Recall(key);

            if (!string.IsNullOrEmpty(encoded))
            {
                var decode =
                    GamewideContextBrainHelpers.DecodeInstanceFromString<PlayerRosterSaveData>(
                        this,
                        encoded
                    );
                if (decode.Success && decode.Value != null)
                {
                    var runtimeInstanceResult = _rosterManager.InstantiatePlayerTeamRoster(
                        GamewidePersistentPlayerRoster
                    );
                    if (runtimeInstanceResult.Success == true)
                    {
                        if (decode.Value.LastSavedBattleTurn > 1)
                        {
                            runtimeInstanceResult.Value.ApplyDecodedPlacements(
                                decode.Value.Placements
                            );
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

            _rosterManager.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
        }

        private void HandleSavePlayerRosterRequested() =>
            SavePlayerRoster(Brain.battleBrain.CurrentTurnNumber);

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
            _ltm.Remember(key, encode.Value);
            _rosterPersistence.RegisterPlayerRoster(GamewidePersistentPlayerRoster);
        }

        public int GetSavedPlayerRosterLastBattleTurn()
        {
            var key = GamewideContextBrainHelpers.BuildRosterLedgerKey(
                GamewidePersistentPlayerRoster.Id
            );
            var encoded = _ltm.Recall(key);
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
            _characterPersistence.RecallCharacter(template);

        public void PersistCharacter(CharacterInstance instance, bool updateIndex = false) =>
            _characterPersistence.SaveCharacter(instance, updateIndex);

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
