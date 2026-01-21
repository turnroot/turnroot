using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public partial class UnitAppearanceBrain : BrainComponent
    {
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
            // If this instance is using the battle model, prefer the class outfit prefab.
            if (unit.UseBattleModel)
            {
                var classInst = unit.GetCurrentClass();
                var prefab = classInst?.ClassData?.Identity?.ClassModelPrefab;
                if (prefab != null)
                {
                    var obj = Instantiate(prefab, parent.transform);
                    obj.name = "ClassOutfit";
                    TurnrootLogger.Log(
                        $"CreateOutfitMesh: Using class outfit prefab for {unit.CharacterTemplate?.DisplayName}"
                    );
                    return obj.GetComponentInChildren<SkinnedMeshRenderer>();
                }
            }
            else
            {
                // Use per-character non-battle outfit prefab when available
                var nbPrefab = unit.CharacterTemplate.NonBattleOutfitPrefab;
                if (nbPrefab != null)
                {
                    var obj = Instantiate(nbPrefab, parent.transform);
                    obj.name = "NonBattleOutfit";
                    TurnrootLogger.Log(
                        $"CreateOutfitMesh: Using non-battle outfit prefab for {unit.CharacterTemplate?.DisplayName}"
                    );
                    return obj.GetComponentInChildren<SkinnedMeshRenderer>();
                }
            }

            // Fallback to copying the character's default model renderer
            if (unit.CharacterTemplate.CharacterDefaultModel != null)
            {
                var obj = new GameObject("DefaultOutfit");
                obj.transform.SetParent(parent.transform);
                TurnrootLogger.Log(
                    $"CreateOutfitMesh: Falling back to CharacterDefaultModel for {unit.CharacterTemplate?.DisplayName}"
                );
                return CopyRenderer(obj, unit.CharacterTemplate.CharacterDefaultModel);
            }

            return null;
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

            TurnrootLogger.Log(
                $"TryReuseExistingModel: Reusing model {model.name} for unit {unit?.Id ?? "<null>"} at {pos}"
            );

            ApplyVisuals(unit, model);
            RemovePreviousMapping(models, model);
            models[pos] = model;

            _brain?.Publish(new ModelSpawnedEvent(unit, unit.Id, pos, model));
            return model;
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

            TurnrootLogger.Log(
                $"CreateNewModel: Created model {model.name} for unit {unit?.Id ?? "<null>"} at {pos}"
            );

            ApplyVisuals(unit, model);
            models[pos] = model;

            _brain?.Publish(new ModelSpawnedEvent(unit, unit.Id, pos, model));
            return OperationResult.Successful();
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

            return OperationResult.Successful();
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
    }
}
