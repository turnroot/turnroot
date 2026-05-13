using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Recipes
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "Turnroot/Objects/Recipe")]
    public class Recipe : ScriptableObject
    {
        public string RecipeName;

        [TextArea(4, 10)]
        public string Description;
        public int SkillBonus;
        public int StrengthBonus;
        public int MagicBonus;
        public int DefenseBonus;
        public int ResistanceBonus;
        public int SpeedBonus;
        public int LuckBonus;
        public int DexterityBonus;
        public int MovementBonus;
        public int CharmBonus;
        public ObjectItem[] Ingredients;

        public CharacterData[] UnitsLove;
        public CharacterData[] UnitsHate;
    }
}
