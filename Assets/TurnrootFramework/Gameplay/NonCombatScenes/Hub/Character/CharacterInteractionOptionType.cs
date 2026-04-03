using Turnroot.UI;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Character
{
    public enum CharacterInteractionOptionType
    {
        Train,
        Talk,
        Meal,
        Spa,
        Dance,
        Gift,
        LostItem,
        Support,
        Recruit,
    }

    [System.Serializable]
    public struct CharacterInteractionOption
    {
        public UiChoice Choice;
        public CharacterInteractionOptionType OptionType;
    }
}
