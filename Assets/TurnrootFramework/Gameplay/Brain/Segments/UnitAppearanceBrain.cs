using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class UnitAppearanceBrain : BrainComponent
    {
        private GameplayGeneralSettings generalSettings;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake()
        {
            base.Awake();
            generalSettings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
        }

        /// <summary>
        /// Get the material for the given unit given their class. If it doesn't exist, create it.
        /// Uses a cached dictionary on the CharacterInstance to avoid recreating materials.
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        public Material GetUnitOutfitMaterial(CharacterInstance unit)
        {
            Material material;
            var classInstance = unit.GetCurrentClass();
            var className = classInstance?.ClassData?.GetClassName() ?? "";
            if (unit.classNameToOutfitMaterials.ContainsKey(className))
            {
                material = unit.classNameToOutfitMaterials[className];
            }
            else
            {
                material = new Material(generalSettings.UnitOutfitMaterialTemplate)
                {
                    name = $"{unit.CharacterTemplate.DisplayName}_OutfitMaterial",
                };
                unit.classNameToOutfitMaterials[className] = material;
            }

            // Assign material to the character's renderer and initialize visuals via the class instance
            var renderer = unit.Renderer;
            if (renderer != null)
            {
                renderer.material = material;
                classInstance?.InitializeWithRenderer(renderer);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("GetUnitOutfitMaterial: unit has no Renderer assigned.");
#endif
            }

            material.SetColor("_Accent_Color_1", unit.CharacterTemplate.AccentColor1);
            material.SetColor("_Accent_Color_2", unit.CharacterTemplate.AccentColor2);
            material.SetColor("_Accent_Color_3", unit.CharacterTemplate.AccentColor3);
            material.SetColor("_Skin_Color", unit.CharacterTemplate.SkinColor);

            // Apply class textures if available on the current class
            var identity = classInstance?.ClassData?.Identity;
            if (identity != null)
            {
                if (identity.Base != null)
                {
                    material.SetTexture("_Base", identity.Base);
                }

                if (identity.MSE != null)
                {
                    material.SetTexture("_MSE", identity.MSE);
                }

                if (identity.TintMask != null)
                {
                    material.SetTexture("_Tint_Mask", identity.TintMask);
                }
            }

            return material;
        }
    }
}
