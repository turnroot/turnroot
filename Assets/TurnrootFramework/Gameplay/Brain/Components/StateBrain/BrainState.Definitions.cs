namespace Turnroot.Gameplay.Brain
{
    public class BrainState
    {
        public string Name { get; private set; }
        public BrainState Parent { get; set; }
        public BrainState[] Children { get; set; }
        public bool IsActive { get; set; }

        public BrainState(string name, BrainState parent = null)
        {
            Name = name;
            Parent = parent;
            Children = null;
            IsActive = false;
        }

        public string GetFullPath() => Parent != null ? $"{Parent.Name}.{Name}" : Name;
    }

    /// <summary>
    /// Constant state names for type safety and refactoring ease.
    /// </summary>
    public static class BrainStateNames
    {
        // Core Game States
        public const string Combat = "Combat";
        public const string Paused = "Paused";
        public const string Cutscene = "Cutscene";
        public const string WorldMap = "WorldMap";
        public const string MainMenu = "MainMenu";
        public const string GameStart = "GameStart";
        public const string GameOver = "GameOver";
        public const string Credits = "Credits";

        // Gameplay States
        public const string NonCombatGameplay = "NonCombatGameplay";
        public const string Hub = "Hub";
        public const string Shop = "Shop";
        public const string Armory = "Armory";
        public const string SupportConversation = "SupportConversation";
        public const string Barracks = "Barracks";
        public const string Base = "Base";
        public const string Trading = "Trading";
        public const string ClassChange = "ClassChange";
        public const string Forging = "Forging";
        public const string Records = "Records";
        public const string Briefing = "Briefing";
        public const string Deployment = "Deployment";
        public const string Configuration = "Configuration";

        // Combat Child States
        public const string PreBattle = "PreBattle";
        public const string PreBattleTransitionToBattle = "PreBattleTransitionToBattle";
        public const string Battle = "Battle";
        public const string PostBattle = "PostBattle";

        // GameStart Child States
        public const string ChooseSaveFile = "ChooseSaveFile";

        /// <summary>
        /// Returns all valid state IDs as full paths. This is the single source of truth for all states.
        /// High-level states without children are listed as-is (e.g., "Paused").
        /// States with children are listed with their hierarchy (e.g., "Combat.PreBattle").
        /// Used by UI and flow systems for validation and dropdown menus.
        /// </summary>
        public static string[] GetAllStateIds()
        {
            return new[]
            {
                // High-level states without children
                Paused,
                Cutscene,
                WorldMap,
                MainMenu,
                GameStart,
                GameOver,
                Credits,
                NonCombatGameplay,
                Hub,
                Shop,
                Armory,
                SupportConversation,
                Barracks,
                Base,
                Trading,
                ClassChange,
                Forging,
                Records,
                Briefing,
                Deployment,
                Configuration,
                // combat children
                $"{Combat}.{PreBattle}",
                $"{Combat}.{PreBattleTransitionToBattle}",
                $"{Combat}.{Battle}",
                $"{Combat}.{PostBattle}",
                // game start children
                $"{GameStart}.{ChooseSaveFile}",
            };
        }
    }
}
