using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    /// <summary>
    /// Handles unit visual configuration including materials, colors, textures, and blendshapes.
    /// </summary>
    public partial class UnitAppearanceBrain
    {
        private void ApplyVisuals(CharacterInstance unit, GameObject model)
        {
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                unit.SetRenderer(renderer);
                ApplyMaterials(unit);
                SetBlendshapes(unit);
            }

            SetupWalkAnimation(model, unit);
        }

        private void ApplyMaterials(CharacterInstance unit) => GetUnitOutfitMaterial(unit);

        public Material GetUnitOutfitMaterial(CharacterInstance unit)
        {
            if (!unit.UseBattleModel)
            {
                return null;
            }

            var classInst = unit.GetCurrentClass();
            if (classInst?.ClassData == null || !classInst.ClassData.HasOutfit)
            {
                return null;
            }

            var material = GetOrCreateMaterial(unit, classInst.ClassData.GetClassName() ?? "");
            var classRenderer = classInst.MeshRenderer;

            if (classRenderer == null)
            {
                LogWarning("No class MeshRenderer found; skipping class material application");
                return null;
            }

            ApplyMaterialToRenderers(new[] { classRenderer }, material);
            InitializeClassVisuals(classInst, unit);
            ApplyColorSettings(material, unit);
            ApplyClassTextures(material, classInst);

            return material;
        }

        private void InitializeClassVisuals(
            CharacterClassDataInstance classInst,
            CharacterInstance unit
        )
        {
            var renderer = classInst?.MeshRenderer ?? unit.Renderer;
            if (renderer != null)
            {
                classInst?.InitializeWithRenderer(renderer);
            }
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

        private IEnumerable<SkinnedMeshRenderer> GetBlendshapeRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        ) => GetOutfitRenderers(unit, classInst);

        private IEnumerable<SkinnedMeshRenderer> GetOutfitRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        )
        {
            var list = new List<SkinnedMeshRenderer>();
            if (unit?.Renderer == null)
            {
                return list.Distinct();
            }

            var root = unit.Renderer.gameObject.transform.parent;
            var searchRoot = root != null ? root.gameObject : unit.Renderer.gameObject;

            foreach (var r in searchRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var rn = r.gameObject.name ?? string.Empty;
                if (
                    !rn.StartsWith("HeadHands")
                    && !rn.Equals("Hair")
                    && !rn.StartsWith("NonBattleOutfit")
                )
                {
                    list.Add(r);
                }
            }

            if (classInst?.MeshRenderer != null && !list.Contains(classInst.MeshRenderer))
            {
                list.Add(classInst.MeshRenderer);
            }

            return list.Distinct();
        }

        public OperationResult SetBlendshapes(CharacterInstance unit)
        {
            var weights = unit.CharacterTemplate.Blendshapes;
            var names = weights.BlendshapeNames ?? new string[0];
            var renderers = GetBlendshapeRenderers(unit, unit.GetCurrentClass()).ToArray();

            if (renderers.Length == 0)
            {
                LogWarning(
                    $"SetBlendshapes: no outfit renderers found for {unit.CharacterTemplate?.DisplayName}"
                );
                return OperationResult.Successful();
            }

            foreach (var shapeName in names)
            {
                var weight = weights.GetBlendshapeByName(shapeName);
                if (!ApplyBlendshapeToRenderers(renderers, shapeName, weight))
                {
                    LogWarning(
                        $"Could not set blendshape weight for {shapeName} on {unit.CharacterTemplate?.DisplayName}: shape not found on any renderer"
                    );
                }
            }

            return OperationResult.Successful();
        }

        private bool ApplyBlendshapeToRenderers(
            SkinnedMeshRenderer[] renderers,
            string name,
            float weight
        )
        {
            bool applied = false;
            foreach (var r in renderers)
            {
                if (r?.sharedMesh == null)
                {
                    continue;
                }

                int index = r.sharedMesh.GetBlendShapeIndex(name);
                if (index >= 0)
                {
                    r.SetBlendShapeWeight(index, weight);
                    applied = true;
                }
            }

            return applied;
        }
    }
}
