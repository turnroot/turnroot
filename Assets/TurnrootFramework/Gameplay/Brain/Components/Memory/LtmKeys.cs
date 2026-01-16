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

        #region StorehouseBrain Keys
        public const string StorehousePurchasingPower = "Storehouse.Purchasing_Power";

        public const string StorehouseStoredItems = "Storehouse.StoredItems";

        public const string StorehouseMaterialPrefix = "Storehouse.Material_";

        /// <summary>Build a storehouse material key for a given material name.</summary>
        public static string StorehouseMaterialKey(string materialName) =>
            StorehouseMaterialPrefix + materialName;

        #endregion

        #region BattleBrain Keys
        public const string UnitSelectedForBattlePrefix = "BattleBrain.UnitSelectedForBattle.";
        public const string UnitSelectionsAutoFilled = "BattleBrain.UnitSelectionsAutoFilled";
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
