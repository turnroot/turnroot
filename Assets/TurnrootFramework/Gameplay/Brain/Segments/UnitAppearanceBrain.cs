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

        public GameObject CreateModelForUnit(CharacterInstance unit)
        {
            // Create a parent container for the complete character
            var characterRoot = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");

            // === PART 1: Body/Outfit Mesh ===
            GameObject outfitObject = null;
            SkinnedMeshRenderer outfitRenderer = null;

            // Priority 1: Use class-specific prefab if available
            var classInstance = unit.GetCurrentClass();
            if (classInstance?.ClassData?.Identity?.ClassModelPrefab != null)
            {
                outfitObject = Instantiate(
                    classInstance.ClassData.Identity.ClassModelPrefab,
                    characterRoot.transform
                );
                outfitObject.name = "ClassOutfit";
                outfitRenderer = outfitObject.GetComponentInChildren<SkinnedMeshRenderer>();
            }
            // Priority 2: Use character's default model
            else if (unit.CharacterTemplate.CharacterDefaultModel != null)
            {
                outfitObject = new GameObject("DefaultOutfit");
                outfitObject.transform.SetParent(characterRoot.transform);
                outfitRenderer = outfitObject.AddComponent<SkinnedMeshRenderer>();

                var sourceRenderer = unit.CharacterTemplate.CharacterDefaultModel;
                outfitRenderer.sharedMesh = sourceRenderer.sharedMesh;
                outfitRenderer.rootBone = sourceRenderer.rootBone;
                outfitRenderer.bones = sourceRenderer.bones;
            }

            // === PART 2: Head/Hands/Hair Mesh ===
            GameObject headObject = null;
            SkinnedMeshRenderer headRenderer = null;

            if (unit.CharacterTemplate.CharacterHeadHandsAndHair != null)
            {
                headObject = new GameObject("HeadHandsHair");
                headObject.transform.SetParent(characterRoot.transform);
                headRenderer = headObject.AddComponent<SkinnedMeshRenderer>();

                var sourceRenderer = unit.CharacterTemplate.CharacterHeadHandsAndHair;
                headRenderer.sharedMesh = sourceRenderer.sharedMesh;
                headRenderer.rootBone = sourceRenderer.rootBone;
                headRenderer.bones = sourceRenderer.bones;
            }

            // === PART 3: Set the primary renderer on the unit ===
            // The "main" renderer is the outfit renderer - UnitAppearanceBrain uses this as the primary reference
            if (outfitRenderer != null)
            {
                unit.SetRenderer(outfitRenderer);
                outfitRenderer.renderingLayerMask = (uint)
                    ~(1 << LayerMask.NameToLayer("NoMapGridDecals"));
                if (headRenderer != null)
                {
                    // If there's also a head renderer, set its layer mask too
                    headRenderer.renderingLayerMask = (uint)
                        ~(1 << LayerMask.NameToLayer("NoMapGridDecals"));
                }
            }
            else if (headRenderer != null)
            {
                // Fallback: if somehow there's no outfit but there's a head
                unit.SetRenderer(headRenderer);
                headRenderer.renderingLayerMask = (uint)
                    ~(1 << LayerMask.NameToLayer("NoMapGridDecals"));
            }
            else
            {
                // No renderers at all - create placeholder
                Debug.LogWarning(
                    $"No renderers found for {unit.CharacterTemplate.DisplayName}, creating placeholder"
                );
                var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                placeholder.transform.SetParent(characterRoot.transform);
                placeholder.GetComponent<Renderer>().material.color =
                    unit.CharacterTemplate.AccentColor1;
                var smr = placeholder.AddComponent<SkinnedMeshRenderer>();
                smr.renderingLayerMask = (uint)~(1 << LayerMask.NameToLayer("NoMapGridDecals"));
                unit.SetRenderer(smr);
            }

            return characterRoot;
        }

        public OperationResult SpawnUnitModelOnGrid(
            Vector2Int position,
            CharacterInstance unit,
            Dictionary<Vector2Int, GameObject> _unitModels,
            bool prebattle = false
        )
        {
            // TODO: Add brain events
            if (unit == null)
            {
                return OperationResult.Failure("Unit is null");
            }

            // Determine world position for the model
            var worldPos = prebattle
                ? _brain.battleBrain.PreparationObject.MapGrid.GetTerrainAdjustedWorldPosition(
                    position
                )
                : _brain.battleBrain.BattleObject.MapGrid.GetTerrainAdjustedWorldPosition(position);

            // Use ownership component to find an existing model for this unit (robust, avoids name parsing)
            var existingOwners = FindObjectsByType<UnitModelOwnership>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            var existingOwner = existingOwners.FirstOrDefault(o =>
                o != null && !string.IsNullOrEmpty(o.UnitId) && o.UnitId == unit.Id
            );
            if (existingOwner != null)
            {
                var existing = existingOwner.gameObject;
#if UNITY_EDITOR
                Debug.Log(
                    "SpawnUnitModelOnGrid: reusing model for unit id '"
                        + unit.Id
                        + "' ; moving to "
                        + position
                );
#endif
                // Move and rescale existing model to the requested position
                existing.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
                existing.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;

                // Update debug display name
                existingOwner.DisplayName = unit.CharacterTemplate.DisplayName;

                // Ensure renderer visuals are applied
                var existingRenderer = existing.GetComponentInChildren<SkinnedMeshRenderer>();
                if (existingRenderer != null)
                {
                    unit.SetRenderer(existingRenderer);
                    GetUnitOutfitMaterial(unit);
                    SetBlendshapes(unit);
                }

                // Remove any prior mapping in this dictionary that referenced the same GameObject
                var keys = new List<Vector2Int>(_unitModels.Keys);
                foreach (var k in keys)
                {
                    if (_unitModels.TryGetValue(k, out var obj) && obj == existing)
                    {
                        _unitModels.Remove(k);
                        break;
                    }
                }

                // Register existing model at the new position
                _unitModels[position] = existing;
                return OperationResult.SuccessResult();
            }

            // Clean up existing model at this position before spawning a new one
            if (_unitModels.TryGetValue(position, out var oldModel))
            {
                try
                {
                    oldModel.SetActive(false);
                }
                catch { }
                Destroy(oldModel);
                _unitModels.Remove(position);
            }

            // Instantiate model now that we know we won't reuse an existing one
            GameObject modelInstance = CreateModelForUnit(unit);

            if (modelInstance == null)
            {
                return OperationResult.Failure("Failed to create model instance");
            }

            modelInstance.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            modelInstance.transform.localScale =
                Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;

            // Attach ownership info and name model for debugging
            var ownership = modelInstance.AddComponent<UnitModelOwnership>();
            ownership.UnitId = unit.Id;
            ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            modelInstance.name = $"{unit.CharacterTemplate.DisplayName}_Model_{unit.Id}";

            // Get the SkinnedMeshRenderer and set it on the unit
            var renderer = modelInstance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                unit.SetRenderer(renderer);
                GetUnitOutfitMaterial(unit);
                SetBlendshapes(unit);
            }

            _unitModels[position] = modelInstance;
            return OperationResult.SuccessResult();
        }

        public OperationResult DespawnUnitModelFromGrid(
            Vector2Int position,
            Dictionary<Vector2Int, GameObject> _unitModels
        )
        {
            if (_unitModels == null)
            {
                return OperationResult.Failure("Model dictionary is null");
            }

            if (_unitModels.TryGetValue(position, out var model) && model != null)
            {
                // Hide immediately to avoid visible duplicates until Destroy executes
                try
                {
                    model.SetActive(false);
                }
                catch { }

                // Destroy and remove mapping
                Destroy(model);
                _unitModels.Remove(position);
#if UNITY_EDITOR
                Debug.Log($"DespawnUnitModelFromGrid: removed model for {position}");
#endif
                return OperationResult.SuccessResult();
            }
#if UNITY_EDITOR
            Debug.LogWarning($"DespawnUnitModelFromGrid: no model found at {position}");
#endif
            return OperationResult.Failure("No model found at given position");
        }
    }
}
