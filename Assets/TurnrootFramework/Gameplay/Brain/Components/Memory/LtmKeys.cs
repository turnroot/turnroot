using Turnroot.Gameplay.Objects;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Centralized static class for all LongTermMemory keys.
    /// Using const strings eliminates magic strings and enables compile-time checking.
    /// Group keys by domain/brain segment for organization.
    /// </summary>
    public static class LtmKeys
    {
        #region CharactersBrain Keys
        public const string BattlesWon = "CharactersBrain.BattlesWon";
        public const string BattlesLost = "CharactersBrain.BattlesLost";
        public const string BattlesRetreated = "CharactersBrain.BattlesRetreated";
        public const string TotalBattles = "CharactersBrain.TotalBattles";

        public const string AvatarSelectedEyeColor = "CharactersBrain.AvatarSelectedEyeColor";
        public const string AvatarSelectedHairColor = "CharactersBrain.AvatarSelectedHairColor";
        public const string AvatarSelectedSkinColor = "CharactersBrain.AvatarSelectedSkinColor";
        public const string AvatarSelectedHairStyle = "CharactersBrain.AvatarSelectedHairStyle";
        public const string AvatarSelectedHeadAccessory =
            "CharactersBrain.AvatarSelectedHeadAccessory";
        public const string AvatarSelectedOutfit = "CharactersBrain.AvatarSelectedFullOutfit";

        public const string AvatarSelectedVoice = "CharactersBrain.AvatarSelectedVoice";

        public const string AvatarDisplayName = "CharactersBrain.AvatarDisplayName";
        public const string AvatarFullName = "CharactersBrain.AvatarFullName";
        public const string AvatarPronouns = "CharactersBrain.AvatarPronouns";

        #endregion

        #region StateBrain Keys
        public const string HighLevelStatesCount = "StateBrain.HighLevelStates";
        public const string HighLevelStatePrefix = "StateBrain.HighLevelState.";

        #endregion

        #region ConversationalBrain Keys
        public const string ConversationCompletedPrefix = "Conversation.Completed.";
        public const string ConversationSeenPrefix = "Conversation.Seen.";
        public const string SupportConversationPrefix = "Support.Conversation.";

        #endregion

        #region GamewideContextBrain Keys
        public const string RosterIndex = "GamewideContextBrain.Roster.Index";

        public const string Roster = "GamewideContextBrain.Roster";

        public const string InstanceHash = "GamewideContextBrain.InstanceHash";
        public const string UniqueCharacterIndex = "GamewideContextBrain.UniqueCharacter.Index";

        public const string CharacterKey = "GamewideContextBrain.UniqueCharacter";

        public const string ExploredPartial = "GamewideContextBrain.ExploredMapStatus";

        #endregion

        #region GameDate Keys
        // Stored game calendar information. Separate fields allow easy numeric queries.
        public const string GameDateYear = "GameDate.Year";
        public const string GameDateMonth = "GameDate.Month"; // 1-based (1=January)
        public const string GameDateDay = "GameDate.Day";

        #endregion

        #region StorehouseBrain Keys
        public const string StorehousePurchasingPower = "Storehouse.Purchasing_Power";

        public const string StorehouseStoredItems = "Storehouse.StoredItems";

        public const string StorehouseMaterialPrefix = "Storehouse.Material_";
        public const string StorehouseMaterialIdPrefix = "Storehouse.MaterialId_";

        /// <summary>Build a storehouse material key for a given material name.</summary>
        public static string StorehouseMaterialKey(string materialName) =>
            StorehouseMaterialPrefix + materialName;

        /// <summary>Build a storehouse material key for a given object ID.</summary>
        public static string StorehouseMaterialIdKey(string materialId) =>
            StorehouseMaterialIdPrefix + materialId;

        public static string StorehouseMaterialKey(ObjectItem item) =>
            item == null ? null : StorehouseMaterialIdKey(item.Id);

        #endregion

        #region MapExploration Keys
        public const string MapExploration = "MapExploration";

        /// <summary>Build the LTM key for a battle's quadrant exploration status.</summary>
        public static string MapExplorationKey(string battleSceneName) =>
            $"{MapExploration}.{battleSceneName}";

        #endregion

        #region BattleBrain Keys
        public const string UnitSelectedForBattlePrefix = "BattleBrain.UnitSelectedForBattle.";
        public const string UnitSelectionsAutoFilled = "BattleBrain.UnitSelectionsAutoFilled";

        // Per-battle deterministic RNG seed (keyed by preparation object / map name)
        public const string BattleSeedPrefix = "Battle.Seed.";

        public static string BattleSeedKey(string battleId) => BattleSeedPrefix + battleId;
        #endregion

        #region Helper Methods

        public static string ConversationCompleted(string conversationId) =>
            ConversationCompletedPrefix + conversationId;

        public static string ConversationSeen(string conversationId) =>
            ConversationSeenPrefix + conversationId;

        public static string SupportConversation(string characterPairId) =>
            SupportConversationPrefix + characterPairId;

        public static string HighLevelState(int index) => HighLevelStatePrefix + index;

        #endregion
    }
}
