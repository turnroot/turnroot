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

        /// <summary>Total battles won this playthrough.</summary>
        public const string BattlesWon = "CharactersBrain.BattlesWon";

        /// <summary>Total battles lost this playthrough.</summary>
        public const string BattlesLost = "CharactersBrain.BattlesLost";

        /// <summary>Total battles retreated from this playthrough.</summary>
        public const string BattlesRetreated = "CharactersBrain.BattlesRetreated";

        /// <summary>Total battles fought this playthrough.</summary>
        public const string TotalBattles = "CharactersBrain.TotalBattles";

        #endregion

        #region StateBrain Keys

        /// <summary>Number of high-level states stored.</summary>
        public const string HighLevelStatesCount = "StateBrain.HighLevelStates";

        /// <summary>Prefix for individual high-level state names. Append index.</summary>
        public const string HighLevelStatePrefix = "StateBrain.HighLevelState.";

        #endregion

        #region ConversationalBrain Keys

        /// <summary>Prefix for tracking completed conversations. Append conversation ID.</summary>
        public const string ConversationCompletedPrefix = "Conversation.Completed.";

        /// <summary>Prefix for tracking seen conversations. Append conversation ID.</summary>
        public const string ConversationSeenPrefix = "Conversation.Seen.";

        /// <summary>Prefix for support conversation progress. Append character pair ID.</summary>
        public const string SupportConversationPrefix = "Support.Conversation.";

        #endregion

        #region GamewideContextBrain Keys

        /// <summary>Index for roster data serialization.</summary>
        public const string RosterIndex = "GWB.Roster.Index";

        /// <summary>Index for unique character data serialization.</summary>
        public const string UniqueCharacterIndex = "GWB.UniqueCharacter.Index";

        #endregion

        #region StorehouseBrain Keys

        /// <summary>Player's current purchasing power (gold).</summary>
        public const string StorehousePurchasingPower = "Storehouse_Purchasing_Power";

        /// <summary>List of stored item IDs in the storehouse.</summary>
        public const string StorehouseStoredItems = "Storehouse_StoredItems";

        #endregion

        #region Helper Methods

        /// <summary>
        /// Builds a conversation completed key for a specific conversation.
        /// </summary>
        public static string ConversationCompleted(string conversationId) =>
            ConversationCompletedPrefix + conversationId;

        /// <summary>
        /// Builds a conversation seen key for a specific conversation.
        /// </summary>
        public static string ConversationSeen(string conversationId) =>
            ConversationSeenPrefix + conversationId;

        /// <summary>
        /// Builds a support conversation key for a character pair.
        /// </summary>
        public static string SupportConversation(string characterPairId) =>
            SupportConversationPrefix + characterPairId;

        /// <summary>
        /// Builds a high-level state key for a specific index.
        /// </summary>
        public static string HighLevelState(int index) => HighLevelStatePrefix + index;

        #endregion
    }
}
