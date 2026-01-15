using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
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
        /// Applies material to all relevant SkinnedMeshRenderers (root + children + class renderer).
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

            // Gather renderers (root renderer + children + class renderer if present)
            var renderers = GetRelevantRenderers(unit, classInstance).ToArray();

            if (renderers.Length == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "GetUnitOutfitMaterial: unit has no SkinnedMeshRenderer assigned."
                );
#endif
                return material;
            }

            // Assign material to all renderers so textures/colors are consistent across meshes
            foreach (var r in renderers)
            {
                if (r == null)
                    continue;
                r.material = material;
            }

            // Initialize class visuals on the preferred renderer (prefer class renderer when available)
            if (classInstance != null)
            {
                if (classInstance.MeshRenderer != null)
                {
                    classInstance.InitializeWithRenderer(classInstance.MeshRenderer);
                }
                else if (unit.Renderer != null)
                {
                    classInstance.InitializeWithRenderer(unit.Renderer);
                }
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

        public OperationResult SetBlendshapes(CharacterInstance unit)
        {
            var weights = unit.CharacterTemplate.Blendshapes;
            var names = weights.BlendshapeNames ?? new string[0];
            var classInst = unit.GetCurrentClass();
            var renderers = GetRelevantRenderers(unit, classInst).ToArray();

            if (renderers.Length == 0)
            {
                return OperationResult.Failure(
                    "SetBlendshapes: unit has no SkinnedMeshRenderer assigned."
                );
            }

            foreach (var shapeName in names)
            {
                bool applied = false;
                var shapeWeight = weights.GetBlendshapeByName(shapeName);

                foreach (var r in renderers)
                {
                    if (r == null)
                        continue;
                    var mesh = r.sharedMesh;
                    if (mesh == null)
                        continue;
                    int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                    if (shapeIndex >= 0)
                    {
                        r.SetBlendShapeWeight(shapeIndex, shapeWeight);
                        applied = true;
                    }
                }

                if (!applied)
                {
                    return OperationResult.Failure(
                        $"Could not set blendshape weight for {shapeName}: shape not found on any renderer. Fix the mesh to include the blendshape(s)."
                    );
                }
            }

            return OperationResult.SuccessResult();
        }

        /// <summary>
        /// Returns all SkinnedMeshRenderers relevant for a unit's visual (root renderer, its children, and the class renderer if different).
        /// </summary>
        private IEnumerable<SkinnedMeshRenderer> GetRelevantRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInstance
        )
        {
            var list = new List<SkinnedMeshRenderer>();
            if (unit?.Renderer != null)
            {
                // include the root renderer and any child renderers (e.g., head/hands separated on another renderer)
                var root = unit.Renderer.gameObject;
                list.AddRange(root.GetComponentsInChildren<SkinnedMeshRenderer>(true));
            }

            // include class-specific renderer if it's on a different object than the root
            if (classInstance?.MeshRenderer != null && !list.Contains(classInstance.MeshRenderer))
            {
                list.Add(classInstance.MeshRenderer);
            }

            return list.Distinct();
        }
    }
}
