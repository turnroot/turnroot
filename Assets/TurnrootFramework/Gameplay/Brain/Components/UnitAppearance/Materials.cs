using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain : BrainComponent
    {
        public Material GetUnitOutfitMaterial(CharacterInstance unit)
        {
            var classInst = unit.GetCurrentClass();
            var className = classInst?.ClassData?.GetClassName() ?? "";

            var material = GetOrCreateMaterial(unit, className);
            var renderers = GetRelevantRenderers(unit, classInst).ToArray();

            if (renderers.Length == 0)
            {
                _ = OperationResult.Failure(
                    "GetUnitOutfitMaterial: unit has no SkinnedMeshRenderer"
                );
                return null;
            }

            ApplyMaterialToRenderers(renderers, material);
            InitializeClassVisuals(classInst, unit);
            ApplyColorSettings(material, unit);
            ApplyClassTextures(material, classInst);

            return material;
        }

        private Material GetOrCreateMaterial(CharacterInstance unit, string className)
        {
            if (unit.classNameToOutfitMaterials.TryGetValue(className, out var existing))
            {
                return existing;
            }

            var material = new Material(_settings.UnitOutfitMaterialTemplate)
            {
                name = $"{unit.CharacterTemplate.DisplayName}_OutfitMaterial",
            };
            unit.classNameToOutfitMaterials[className] = material;
            return material;
        }

        private void ApplyMaterialToRenderers(SkinnedMeshRenderer[] renderers, Material material)
        {
            foreach (var r in renderers)
            {
                if (r != null)
                {
                    r.material = material;
                }
            }
        }

        private void ApplyColorSettings(Material material, CharacterInstance unit)
        {
            material.SetColor("_Accent_Color_1", unit.CharacterTemplate.AccentColor1);
            material.SetColor("_Accent_Color_2", unit.CharacterTemplate.AccentColor2);
            material.SetColor("_Accent_Color_3", unit.CharacterTemplate.AccentColor3);
            material.SetColor("_Skin_Color", unit.CharacterTemplate.SkinColor);
        }

        private void ApplyClassTextures(Material material, CharacterClassDataInstance classInst)
        {
            var identity = classInst?.ClassData?.Identity;
            if (identity == null)
            {
                return;
            }

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
    }
}
