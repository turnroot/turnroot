using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Maps;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(Brain))]
    public class GamewideContextBrain : BrainComponent
    {
        #region Configuration
        public enum TamperPolicy
        {
            NotifyOnly,
            Reject,
            Replace,
        }

        [field: SerializeField]
        public TamperPolicy Policy { get; } = TamperPolicy.Replace;
        #endregion

        #region Dependencies
        public Brain CentralBrain => _brain;

        private LongTermMemory _ltm;
        private RosterPersistence _rosterPersistence;
        private RosterManager _rosterManager;
        private CharacterPersistence _characterPersistence;
        private PlayerSettingsPersistence _playerSettingsPersistence;
        #endregion

        #region State
        private readonly Dictionary<string, object> _activeRosterInstances = new();

        [HideInInspector]
        public PlayerTeamRoster GamewidePersistentPlayerRoster { get; set; }
        public List<GamewideContextBrainHelpers.ExploredPartial> MapExplorationStatuses
        {
            get;
            private set;
        }

        [HideInInspector]
        public GameplayPlayerSettings PlayerSettings => _playerSettingsPersistence?.PlayerSettings;
        #endregion

        #region Initialization
        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

        protected override void Awake()
        {
            _brain = GetComponent<Brain>();
            SubscribeToBrainEvents();

            _rosterPersistence = new RosterPersistence(GetComponent<LongTermMemory>());
            _rosterManager = new RosterManager(_brain, _rosterPersistence);
            _characterPersistence = new CharacterPersistence(_brain);
            _playerSettingsPersistence = new PlayerSettingsPersistence(
                GetComponent<LongTermMemory>(),
                this
            );

            MapExplorationStatuses = new List<GamewideContextBrainHelpers.ExploredPartial>();
        }

        private void Start()
        {
            _ltm = GetComponent<LongTermMemory>();
            _playerSettingsPersistence?.Initialize();
            TryLoadAndRecallPersistentPlayerRoster();
            _brain.volumeBrain?.ApplySettingsToVolumes(PlayerSettings);
            PopulateMapExplorationStatusesFromLtm();
        }
        #endregion

        #region Event Subscription
        protected override void SubscribeToBrainEvents() =>
            _brain.OnSavePlayerRosterRequested += HandleSavePlayerRosterRequested;

        protected override void UnsubscribeFromBrainEvents() =>
            _brain.OnSavePlayerRosterRequested -= HandleSavePlayerRosterRequested;
        #endregion

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
                        runtimeInstance.ApplyDecodedPlacements(decode.Value.Placements);
                    }
                }
            }

            _rosterManager?.RecallPlayerTeamRoster(GamewidePersistentPlayerRoster);
        }

        private void HandleSavePlayerRosterRequested()
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
        }
        #endregion

        #region Roster Management API
        public GenericRosterInstance GetOrCreateGenericRoster(
            GenericRoster roster,
            bool register = false
        )
        {
            if (roster == null)
            {
                TurnrootLogger.Log(
                    "Cannot get/create null roster",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as GenericRosterInstance;
            }

            var instance = _rosterManager.InstantiateGenericRoster(roster, register);
            if (instance != null)
            {
                _activeRosterInstances[roster.Id] = instance;
            }

            return instance;
        }

        public PlayerTeamRosterInstance GetOrCreatePlayerTeamRoster(PlayerTeamRoster roster)
        {
            if (roster == null)
            {
                TurnrootLogger.Log(
                    "Cannot get/create null player roster",
                    TurnrootLogger.LogLevel.Warning
                );
                return null;
            }

            if (_activeRosterInstances.TryGetValue(roster.Id, out var existing))
            {
                return existing as PlayerTeamRosterInstance;
            }

            var instance = _rosterManager.InstantiatePlayerTeamRoster(roster);
            if (instance != null)
            {
                _activeRosterInstances[roster.Id] = instance;
            }

            return instance;
        }

        public void RecallGenericRosters(List<GenericRoster> rosters) =>
            _rosterManager?.RecallGenericRosters(rosters);

        public PlayerTeamRosterInstance RecallPlayerTeamRoster(PlayerTeamRoster roster) =>
            _rosterManager?.RecallPlayerTeamRoster(roster);

        public PlayerTeamRosterInstance GetPersistentPlayerTeamRosterInstance() =>
            _rosterManager?.GetPersistentPlayerRosterInstance();

        public List<CharacterInstance> GetSelectedForBattlePlayerTeamUnits() =>
            RosterFilters.FilterUnitsSelectedForBattle(GetPersistentPlayerTeamRosterInstance());

        public Characters.Roster.UnitPlacement[] GetSelectedForBattlePlayerTeamPlacements()
        {
            var instance = GetPersistentPlayerTeamRosterInstance();
            var placements =
                instance?.GetPlacements()
                ?? GamewidePersistentPlayerRoster?.characters
                ?? new Characters.Roster.UnitPlacement[0];

            var selectedInstances = GetSelectedForBattlePlayerTeamUnits();
            var selectedTemplates = new HashSet<CharacterData>(
                selectedInstances.Select(i => i.CharacterTemplate)
            );

            return placements
                .Where(p => p.CharacterData != null && selectedTemplates.Contains(p.CharacterData))
                .ToArray();
        }
        #endregion

        #region Character Management API
        public CharacterInstance FindInstanceByTemplate(CharacterData template) =>
            _rosterManager?.FindInstanceByTemplate(template);

        public List<CharacterInstance> GetAllActiveInstances() =>
            _rosterManager?.GetAllActiveInstances() ?? new List<CharacterInstance>();

        public void SaveUniqueCharacterProgress(CharacterInstance instance) =>
            _characterPersistence?.SaveCharacter(instance, updateIndex: false);
        #endregion

        #region Player Settings Management
        public void UpdatePlayerSetting(string settingName, object value)
        {
            _playerSettingsPersistence?.UpdatePlayerSetting(settingName, value);
            _brain?.volumeBrain?.ApplySettingsToVolumes(PlayerSettings);
        }
        #endregion

        #region Map Exploration Management
        public void RegisterMapExplorationPartial(
            GamewideContextBrainHelpers.ExploredPartial partial
        )
        {
            if (partial.map == null || string.IsNullOrEmpty(partial.map.MapName))
            {
                TurnrootLogger.Log(
                    "RegisterMapExplorationPartial: partial must have a valid map and MapName",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredPartial>();

            var existingIndex = MapExplorationStatuses.FindIndex(p =>
                p.map != null && p.map.MapName == partial.map.MapName
            );

            if (existingIndex >= 0)
            {
                MapExplorationStatuses[existingIndex] = partial;
            }
            else
            {
                MapExplorationStatuses.Add(partial);
            }
        }

        public void SaveMapExplorationStatus()
        {
            if (_ltm == null || MapExplorationStatuses == null)
            {
                TurnrootLogger.Log(
                    "SaveMapExplorationStatus: No LTM available or no statuses to save",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            foreach (var status in MapExplorationStatuses)
            {
                SaveMapExplorationStatus(status);
            }
        }

        public void SaveMapExplorationStatus(GamewideContextBrainHelpers.ExploredPartial partial)
        {
            if (partial.map == null || string.IsNullOrEmpty(partial.map.MapName))
            {
                TurnrootLogger.Log(
                    "SaveMapExplorationStatus: partial must have a valid map with MapName",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            if (_ltm == null)
            {
                TurnrootLogger.Log(
                    "SaveMapExplorationStatus: No LongTermMemory component available",
                    TurnrootLogger.LogLevel.Warning
                );
                return;
            }

            var encode = GamewideContextBrainHelpers.EncodeInstanceToString(this, partial);
            if (!encode.Success)
            {
                TurnrootLogger.Log(
                    $"Failed to encode exploration partial for map {partial.map.MapName}: {encode.Error}",
                    TurnrootLogger.LogLevel.Error
                );
                return;
            }

            var key = BuildExplorationPartialKey(partial.map.MapName);
            _ltm.Remember(key, encode.Value);
        }

        public void PopulateMapExplorationStatusesFromLtm()
        {
            if (_ltm == null)
            {
                return;
            }

            var keys = _ltm.RecallKeysByPrefix(LtmKeys.ExploredPartial);
            if (keys == null)
            {
                return;
            }

            MapExplorationStatuses ??= new List<GamewideContextBrainHelpers.ExploredPartial>();

            foreach (var key in keys)
            {
                var encoded = _ltm.Recall(key);

                if (!string.IsNullOrEmpty(encoded))
                {
                    var decoded =
                        GamewideContextBrainHelpers.DecodeInstanceFromString<GamewideContextBrainHelpers.ExploredPartial>(
                            this,
                            encoded
                        );

                    if (decoded.Success)
                    {
                        MapExplorationStatuses.Add(decoded.Value);
                        continue;
                    }
                }

                // Fallback for older key format
                var suffix =
                    key.Length > LtmKeys.ExploredPartial.Length + 1
                        ? key.Substring(LtmKeys.ExploredPartial.Length + 1)
                        : string.Empty;

                var fallbackPartial = new GamewideContextBrainHelpers.ExploredPartial
                {
                    statuses =
                        new Dictionary<
                            GamewideContextBrainHelpers.ExploredQuadrant,
                            GamewideContextBrainHelpers.ExploredState
                        >(),
                    map = string.IsNullOrEmpty(suffix) ? null : Resources.Load<MapGrid>(suffix),
                };

                MapExplorationStatuses.Add(fallbackPartial);
            }
        }

        private string BuildExplorationPartialKey(string mapId) =>
            $"{LtmKeys.ExploredPartial}.{mapId}";
        #endregion
    }

    [System.Serializable]
    public class PlayerRosterSaveData
    {
        public string RosterId;
        public Characters.Roster.UnitPlacement[] Placements;
        public CharacterInstance[] CharacterInstances;
    }
}
