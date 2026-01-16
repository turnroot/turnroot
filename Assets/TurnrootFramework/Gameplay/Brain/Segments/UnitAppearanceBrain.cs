using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Characters.CharacterClass;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class UnitAppearanceBrain : BrainComponent
    {
        private GameplayGeneralSettings _settings;
        private Dictionary<Vector2Int, GameObject> _activeUnitModels = new();

        protected override EventPriority GetSubscriptionPriority() => EventPriority.Low;

        protected override void Awake()
        {
            base.Awake();
            _settings = GameSettingsLoader.LoadFirst<GameplayGeneralSettings>();
        }

        protected override void SubscribeToBrainEvents()
        {
            Brain.OnBattleObjectSet += HandleBattleObjectSet;

            if (Brain.battleBrain?.BattleObject != null)
            {
                HandleBattleObjectSet(Brain.battleBrain.BattleObject);
            }
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            if (Brain != null)
            {
                Brain.OnBattleObjectSet -= HandleBattleObjectSet;
            }
        }

        public Material GetUnitOutfitMaterial(CharacterInstance unit)
        {
            var classInst = unit.GetCurrentClass();
            var className = classInst?.ClassData?.GetClassName() ?? "";

            var material = GetOrCreateMaterial(unit, className);
            var renderers = GetRelevantRenderers(unit, classInst).ToArray();

            if (renderers.Length == 0)
            {
                Debug.LogWarning("GetUnitOutfitMaterial: unit has no SkinnedMeshRenderer");
                return material;
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

        private void InitializeClassVisuals(
            CharacterClassDataInstance classInst,
            CharacterInstance unit
        )
        {
            if (classInst == null)
            {
                return;
            }

            var renderer = classInst.MeshRenderer ?? unit.Renderer;
            if (renderer != null)
            {
                classInst.InitializeWithRenderer(renderer);
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

        public OperationResult SetBlendshapes(CharacterInstance unit)
        {
            var weights = unit.CharacterTemplate.Blendshapes;
            var names = weights.BlendshapeNames ?? new string[0];
            var renderers = GetRelevantRenderers(unit, unit.GetCurrentClass()).ToArray();

            if (renderers.Length == 0)
            {
                return OperationResult.Failure("SetBlendshapes: unit has no SkinnedMeshRenderer");
            }

            foreach (var shapeName in names)
            {
                var weight = weights.GetBlendshapeByName(shapeName);
                if (!ApplyBlendshapeToRenderers(renderers, shapeName, weight))
                {
                    return OperationResult.Failure(
                        $"Could not set blendshape weight for {shapeName}: shape not found on any renderer"
                    );
                }
            }

            return OperationResult.SuccessResult();
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

        private IEnumerable<SkinnedMeshRenderer> GetRelevantRenderers(
            CharacterInstance unit,
            CharacterClassDataInstance classInst
        )
        {
            var list = new List<SkinnedMeshRenderer>();

            if (unit?.Renderer != null)
            {
                list.AddRange(
                    unit.Renderer.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                );
            }

            if (classInst?.MeshRenderer != null && !list.Contains(classInst.MeshRenderer))
            {
                list.Add(classInst.MeshRenderer);
            }

            return list.Distinct();
        }

        public GameObject CreateModelForUnit(CharacterInstance unit)
        {
            var root = new GameObject($"{unit.CharacterTemplate.DisplayName}_Root");
            var outfitRenderer = CreateOutfitMesh(unit, root);
            var headRenderer = CreateHeadMesh(unit, root);

            SetPrimaryRenderer(unit, outfitRenderer, headRenderer, root);
            return root;
        }

        private SkinnedMeshRenderer CreateOutfitMesh(CharacterInstance unit, GameObject parent)
        {
            var classInst = unit.GetCurrentClass();
            var prefab = classInst?.ClassData?.Identity?.ClassModelPrefab;

            if (prefab != null)
            {
                var obj = Instantiate(prefab, parent.transform);
                obj.name = "ClassOutfit";
                return obj.GetComponentInChildren<SkinnedMeshRenderer>();
            }

            if (unit.CharacterTemplate.CharacterDefaultModel != null)
            {
                var obj = new GameObject("DefaultOutfit");
                obj.transform.SetParent(parent.transform);
                return CopyRenderer(obj, unit.CharacterTemplate.CharacterDefaultModel);
            }

            return null;
        }

        private SkinnedMeshRenderer CreateHeadMesh(CharacterInstance unit, GameObject parent)
        {
            if (unit.CharacterTemplate.CharacterHeadHandsAndHair == null)
            {
                return null;
            }

            var obj = new GameObject("HeadHandsHair");
            obj.transform.SetParent(parent.transform);
            return CopyRenderer(obj, unit.CharacterTemplate.CharacterHeadHandsAndHair);
        }

        private SkinnedMeshRenderer CopyRenderer(GameObject target, SkinnedMeshRenderer source)
        {
            var renderer = target.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = source.sharedMesh;
            renderer.rootBone = source.rootBone;
            renderer.bones = source.bones;
            return renderer;
        }

        private void SetPrimaryRenderer(
            CharacterInstance unit,
            SkinnedMeshRenderer outfit,
            SkinnedMeshRenderer head,
            GameObject root
        )
        {
            if (outfit != null)
            {
                unit.SetRenderer(outfit);
            }
            else if (head != null)
            {
                unit.SetRenderer(head);
            }
            else
            {
                unit.SetRenderer(CreatePlaceholderRenderer(unit, root));
            }
        }

        private SkinnedMeshRenderer CreatePlaceholderRenderer(
            CharacterInstance unit,
            GameObject parent
        )
        {
            Debug.LogWarning(
                $"No renderers for {unit.CharacterTemplate.DisplayName}, creating placeholder"
            );
            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.transform.SetParent(parent.transform);
            placeholder.GetComponent<Renderer>().material.color =
                unit.CharacterTemplate.AccentColor1;
            return placeholder.AddComponent<SkinnedMeshRenderer>();
        }

        public OperationResult SpawnUnitModelOnGrid(
            Vector2Int pos,
            CharacterInstance unit,
            Dictionary<Vector2Int, GameObject> models,
            bool prebattle = false
        )
        {
            if (unit == null)
            {
                return OperationResult.Failure("Unit is null");
            }

            var worldPos = GetWorldPosition(pos, prebattle);
            var existing = TryReuseExistingModel(unit, worldPos, pos, models);

            if (existing != null)
            {
                return OperationResult.SuccessResult();
            }

            CleanupOldModel(pos, models);
            return CreateNewModel(unit, worldPos, pos, models);
        }

        private Vector3 GetWorldPosition(Vector2Int pos, bool prebattle)
        {
            return prebattle
                ? _brain.battleBrain.PreparationObject.MapGrid.GetTerrainAdjustedWorldPosition(pos)
                : _brain.battleBrain.BattleObject.MapGrid.GetTerrainAdjustedWorldPosition(pos);
        }

        private GameObject TryReuseExistingModel(
            CharacterInstance unit,
            Vector3 worldPos,
            Vector2Int pos,
            Dictionary<Vector2Int, GameObject> models
        )
        {
            var ownerships = FindObjectsByType<UnitModelOwnership>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            var owner = ownerships.FirstOrDefault(o => o != null && o.UnitId == unit.Id);

            if (owner == null)
            {
                return null;
            }

            var model = owner.gameObject;
            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            model.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;
            owner.DisplayName = unit.CharacterTemplate.DisplayName;

            ApplyVisuals(unit, model);
            RemovePreviousMapping(models, model);
            models[pos] = model;

            _brain?.Publish(new ModelSpawnedEvent(unit, unit.Id, pos, model));
            return model;
        }

        private void ApplyVisuals(CharacterInstance unit, GameObject model)
        {
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                unit.SetRenderer(renderer);
                GetUnitOutfitMaterial(unit);
                SetBlendshapes(unit);
            }
        }

        private void RemovePreviousMapping(
            Dictionary<Vector2Int, GameObject> models,
            GameObject target
        )
        {
            var key = models.FirstOrDefault(kvp => kvp.Value == target).Key;
            if (key != default)
            {
                models.Remove(key);
            }
        }

        private void CleanupOldModel(Vector2Int pos, Dictionary<Vector2Int, GameObject> models)
        {
            if (models.TryGetValue(pos, out var old) && old != null)
            {
                try
                {
                    old.SetActive(false);
                }
                catch { }
                Destroy(old);
                models.Remove(pos);
            }
        }

        private OperationResult CreateNewModel(
            CharacterInstance unit,
            Vector3 worldPos,
            Vector2Int pos,
            Dictionary<Vector2Int, GameObject> models
        )
        {
            var model = CreateModelForUnit(unit);
            if (model == null)
            {
                return OperationResult.Failure("Failed to create model instance");
            }

            model.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            model.transform.localScale = Vector3.one * _brain.uiBrain.uiSettings.ModelsScale;

            var ownership = model.AddComponent<UnitModelOwnership>();
            ownership.UnitId = unit.Id;
            ownership.DisplayName = unit.CharacterTemplate.DisplayName;
            model.name = $"{unit.CharacterTemplate.DisplayName}_Model_{unit.Id}";

            ApplyVisuals(unit, model);
            models[pos] = model;

            _brain?.Publish(new ModelSpawnedEvent(unit, unit.Id, pos, model));
            return OperationResult.SuccessResult();
        }

        public OperationResult DespawnUnitModelFromGrid(
            Vector2Int pos,
            Dictionary<Vector2Int, GameObject> models
        )
        {
            if (models == null)
            {
                return OperationResult.Failure("Model dictionary is null");
            }

            if (!models.TryGetValue(pos, out var model) || model == null)
            {
                return OperationResult.Failure("No model found at given position");
            }

            try
            {
                model.SetActive(false);
            }
            catch { }

            PublishDespawnEvent(model, pos);
            Destroy(model);
            models.Remove(pos);

            return OperationResult.SuccessResult();
        }

        private void PublishDespawnEvent(GameObject model, Vector2Int pos)
        {
            var owner = model.GetComponent<UnitModelOwnership>();
            var unitId = owner?.UnitId;
            var unit = !string.IsNullOrEmpty(unitId)
                ? _brain
                    ?.gamewideContextBrain?.GetAllActiveInstances()
                    ?.FirstOrDefault(u => u?.Id == unitId)
                : null;

            _brain?.Publish(new ModelDespawnedEvent(unit, unitId, pos, model));
        }

        private void HandleBattleStarted()
        {
            Debug.Log("UnitAppearanceBrain: Handling battle started - spawning unit models");

            ClearExistingModels();

            var roster = _brain.battleBrain.PlayerTeamRoster;

            if (roster == null)
            {
                Debug.LogWarning(
                    $"UnitAppearanceBrain: PlayerTeamRoster is null. BattleObject: {_brain?.battleBrain?.BattleObject?.name ?? "null"}"
                );
                roster = _brain?.battleBrain?.BattleObject?.PlayerTeamRoster;
            }

            if (roster == null)
            {
                Debug.LogWarning(
                    "UnitAppearanceBrain: No player roster available to spawn models."
                );
                return;
            }

            var placements = roster.GetPlacements();
            Debug.Log(
                $"UnitAppearanceBrain: PlayerTeamRoster has {placements?.Count() ?? 0} placements."
            );

            foreach (var placement in placements)
            {
                var instance = roster.GetInstanceFor(placement.CharacterData);
                if (instance == null)
                {
                    Debug.LogWarning(
                        $"UnitAppearanceBrain: No instance for template {placement.CharacterData?.DisplayName}"
                    );
                    continue;
                }

                Debug.Log(
                    $"Spawning model for {instance.CharacterTemplate.DisplayName} at {placement.SpawnPosition}"
                );
                var spawnResult = SpawnUnitModelOnGrid(
                    placement.SpawnPosition,
                    instance,
                    _activeUnitModels,
                    prebattle: false
                );
#if UNITY_EDITOR
                if (!spawnResult.Success)
                {
                    Debug.LogWarning(
                        $"UnitAppearanceBrain: Failed to spawn model for {instance.Id} at {placement.SpawnPosition}: {spawnResult.ErrorMessage}"
                    );
                }
#endif
            }
        }

        private void ClearExistingModels()
        {
            foreach (var kvp in _activeUnitModels.ToList())
            {
                if (kvp.Value != null)
                {
                    try
                    {
                        kvp.Value.SetActive(false);
                    }
                    catch { }
                    Destroy(kvp.Value);
                }
            }
            _activeUnitModels.Clear();
        }

        private void HandleBattleObjectSet(BattleGameObject battleObject) => HandleBattleStarted();
    }
}
