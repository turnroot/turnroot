using System.Collections.Generic;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Objects.Recipes
{
    [CreateAssetMenu(fileName = "RecipeCatalog", menuName = "Turnroot/Objects/Recipe Catalog")]
    public class RecipeCatalog : SingletonScriptableObject<RecipeCatalog>
    {
        public Recipe[] AllRecipes;
        private Recipe[] _availableRecipes;

        public void OnValidate()
        {
            var i = 0;
            foreach (var recipe in AllRecipes)
            {
                i++;
                if (recipe == null)
                {
                    $"RecipeCatalog Validate: Recipe at index {i - 1} is null.".LogError();
                    var tempList = new List<Recipe>(AllRecipes);
                    tempList.RemoveAt(i - 1);
                    AllRecipes = tempList.ToArray();
                }
            }
        }
    }
}
