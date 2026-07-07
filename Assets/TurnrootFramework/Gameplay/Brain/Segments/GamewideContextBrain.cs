using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Subclasses;
using Turnroot.Gameplay.Brain.Components;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Manages gamewide context including player roster, character persistence, and exploration state.
    /// Core logic is split into partial files in the GamewideContextBrainPartials subfolder.
    /// </summary>
    [RequireComponent(typeof(LongTermMemory))]
    [RequireComponent(typeof(Brain))]
    public partial class GamewideContextBrain : BrainComponent
    {
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

        [HideInInspector]
        public GameplayPlayerSettings PlayerSettings => _playerSettingsPersistence?.PlayerSettings;
        #endregion

        #region Game Date API

        /// <summary>
        /// Returns the currently stored game date.  Falls back to the starting date
        /// from settings if memory isn't ready or no date is stored yet.
        /// </summary>
        public GameDate GetCurrentGameDate() =>
            _ltm != null && _ltm.Initialized
                ? _ltm.GetGameDate()
                : GameplayGeneralSettings.Instance?.StartingGameDate ?? GameDate.Default;

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
        }

        private void Start() => _ltm = GetComponent<LongTermMemory>();

        private void InitializeLTMDependentData()
        {
            _playerSettingsPersistence?.Initialize();
            TryLoadAndRecallPersistentPlayerRoster();
            RestoreAvatarProfileFromLtm();
            _brain.volumeBrain?.ApplySettingsToVolumes(PlayerSettings);
        }

        private void RestoreAvatarProfileFromLtm()
        {
            if (!TryGetComponent<LongTermMemory>(out var ltm))
            {
                return;
            }

            var settings = GameplayGeneralSettings.Instance;
            var fallbackHair =
                settings.AvatarHairColorChoices != null
                && settings.AvatarHairColorChoices.Length > 0
                    ? settings.AvatarHairColorChoices[0]
                    : Color.white;
            var fallbackEye =
                settings.AvatarEyeColorChoices != null && settings.AvatarEyeColorChoices.Length > 0
                    ? settings.AvatarEyeColorChoices[0]
                    : Color.white;
            var fallbackSkin =
                settings.AvatarSkinColorChoices != null
                && settings.AvatarSkinColorChoices.Length > 0
                    ? settings.AvatarSkinColorChoices[0]
                    : Color.white;

            AvatarHairColor = ParseColorOrFallback(ltm.Recall("Avatar/HairColor"), fallbackHair);
            AvatarEyeColor = ParseColorOrFallback(ltm.Recall("Avatar/EyeColor"), fallbackEye);
            AvatarSkinColor = ParseColorOrFallback(ltm.Recall("Avatar/SkinColor"), fallbackSkin);

            var displayName = ltm.Recall("Avatar/DisplayName");
            if (string.IsNullOrEmpty(displayName))
            {
                return;
            }

            var avatar = GetOrCreateAvatarInstance();
            if (avatar?.CharacterTemplate == null)
            {
                return;
            }

            var fullName = ltm.Recall("Avatar/FullName");
            var pronounType = ltm.Recall("Avatar/Pronouns");
            avatar.CharacterTemplate.SetAvatarNameAndPronouns(
                displayName,
                fullName ?? displayName,
                new Pronouns(pronounType ?? Pronouns.KeyThey)
            );

            var growthJson = ltm.Recall("Avatar/GrowthRates");
            if (!string.IsNullOrEmpty(growthJson))
            {
                try
                {
                    var rates = JsonConvert.DeserializeObject<List<UnboundedStatModifier>>(
                        growthJson
                    );
                    if (rates != null)
                    {
                        avatar.CharacterTemplate.PersonalGrowthRates.Clear();
                        foreach (var r in rates)
                        {
                            avatar.CharacterTemplate.PersonalGrowthRates.Add(r);
                        }
                    }
                }
                catch (Exception ex)
                {
                    $"GamewideContextBrain: Failed to restore avatar growth rates: {ex.Message}".LogWarning();
                }
            }

            $"GamewideContextBrain: Restored avatar profile '{displayName}' from LTM.".LogInfo();
        }

        private static Color ParseColorOrFallback(string serializedColor, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(serializedColor))
            {
                return fallback;
            }

            if (ColorUtility.TryParseHtmlString(serializedColor, out var htmlColor))
            {
                return htmlColor;
            }

            if (serializedColor.Length is 6 or 8)
            {
                if (
                    ColorUtility.TryParseHtmlString($"#{serializedColor}", out var compactHtmlColor)
                )
                {
                    return compactHtmlColor;
                }
            }

            if (serializedColor.StartsWith("{"))
            {
                try
                {
                    return JsonUtility.FromJson<Color>(serializedColor);
                }
                catch
                {
                    "GamewideContextBrain: Failed to parse color from JSON, using fallback.".LogWarning();
                }
            }

            return fallback;
        }
        #endregion

        #region Event Subscription
        protected override void SubscribeToBrainEvents()
        {
            _brain.OnSavePlayerRosterRequested += HandleSavePlayerRosterRequested;
            _brain.OnSavePlayerRosterRequestedWithTurn += HandleSavePlayerRosterRequestedWithTurn;
            _brain.OnPreBattleCompleted += HandlePreBattleCompleted;
            _brain.OnLongTermMemoryInitialized += InitializeLTMDependentData;
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            _brain.OnSavePlayerRosterRequested -= HandleSavePlayerRosterRequested;
            _brain.OnSavePlayerRosterRequestedWithTurn -= HandleSavePlayerRosterRequestedWithTurn;
            _brain.OnPreBattleCompleted -= HandlePreBattleCompleted;
            _brain.OnLongTermMemoryInitialized -= InitializeLTMDependentData;
        }

        private void OnApplicationQuit()
        {
            var rosterInstance = _rosterManager?.GetPersistentPlayerRosterInstance();
            if (rosterInstance == null)
            {
                return;
            }

            foreach (var instance in rosterInstance.Instances)
            {
                PersistIfNeeded(instance);
            }
        }

        private void HandlePreBattleCompleted()
        {
            // Save roster with unit selections and placements before battle starts
            // First, update the runtime instance with placements from BattlePreparationObject
            var prep = _brain?.battleBrain?.PreparationObject;
            if (prep != null && prep.placements != null && prep.placements.Count > 0)
            {
                var runtimeInstance = _rosterManager?.GetPersistentPlayerRosterInstance();
                if (runtimeInstance != null)
                {
                    // Convert placements dictionary to UnitPlacement array
                    var placementList = new List<Characters.Roster.UnitPlacement>();
                    foreach (var kvp in prep.placements)
                    {
                        placementList.Add(
                            new Characters.Roster.UnitPlacement
                            {
                                CharacterData = kvp.Value,
                                SpawnPosition = kvp.Key,
                                Order = placementList.Count,
                            }
                        );
                    }
                    runtimeInstance.ApplyDecodedPlacements(placementList.ToArray());
                }
            }

            // Use turn 1 since this is pre-battle (no battle turns yet)
            SavePlayerRoster(1);
        }
        #endregion

        // Remaining API methods are implemented in partial files within GamewideContextBrainPartials/
        // - Persistence.cs: Character and roster persistence methods
        // - RosterManagement.cs: Roster and character management API
        // - MapExploration.cs: Map exploration state management
        // - PlayerSettings.cs: Player settings management
    }

    [Serializable]
    public class PlayerRosterSaveData
    {
        public string RosterId;
        public Characters.Roster.UnitPlacement[] Placements;
        public CharacterInstance[] CharacterInstances;

        // Last saved battle turn number (0 = no battle saved, 1 = first turn, >1 ongoing)
        public int LastSavedBattleTurn = 0;
    }
}
