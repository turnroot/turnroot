using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Recipes
{
    [CreateAssetMenu(fileName = "RecipeCatalog", menuName = "Turnroot/Objects/Recipe Catalog")]
    public class RecipeCatalog : SingletonScriptableObject<RecipeCatalog>
    {
        public Recipe[] AllRecipes;
        private Recipe[] _availableRecipes;
    }
}
