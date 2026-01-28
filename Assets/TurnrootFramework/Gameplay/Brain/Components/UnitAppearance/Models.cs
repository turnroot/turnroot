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

            // Some outfits (non-battle prefabs) include their own head/hands or hair.
            // Only create head/hair separately if they are not present on the outfit instance.
            var hasHead = root.transform.Find("HeadAndHands") != null;
            var hasHair = root.transform.Find("Hair") != null;

            var headRenderer = hasHead ? outfitRenderer : CreateHeadMesh(unit, root);
            var hairRenderer = hasHair ? null : CreateHairMesh(unit, root);

            SetPrimaryRenderer(unit, outfitRenderer, headRenderer, root);
            return root;
        }

        private SkinnedMeshRenderer CreateHairMesh(CharacterInstance unit, GameObject parent)
        {
            var classInst = unit.GetCurrentClass();
            // Always use unit hair for non-battle models; when using the battle model respect the class flags
            var useHair =
                !unit.UseBattleModel
                || classInst?.ClassData == null
                || !classInst.ClassData.HasOutfit
                || classInst.ClassData.UseUnitHairOnModel;
            if (!useHair)
            {
                return null;
            }

            if (unit.CharacterTemplate.HairPrefab == null)
            {
                return null;
            }

            var instance = Instantiate(unit.CharacterTemplate.HairPrefab, parent.transform);
            instance.name = "Hair";
            TurnrootLogger.Log(
                $"CreateHairMesh: Instantiated hair prefab for {unit.CharacterTemplate?.DisplayName} (useBattleModel={unit.UseBattleModel})"
            );
            return instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        }

        private SkinnedMeshRenderer CreateOutfitMesh(CharacterInstance unit, GameObject parent)
        {
            // If this instance is using the battle model, prefer the class outfit prefab.
            if (unit.UseBattleModel)
            {
                var classInst = unit.GetCurrentClass();
                // Respect class HasOutfit flag: if the class explicitly does not supply an outfit,
                // fall through to per-character default non-battle outfit behavior.
                if (classInst?.ClassData != null && classInst.ClassData.HasOutfit)
                {
                    var prefab = classInst.ClassData.Identity?.ClassModelPrefab;
                    if (prefab != null)
                    {
                        var obj = Instantiate(prefab, parent.transform);
                        obj.name = "ClassOutfit";
                        var smr = obj.GetComponentInChildren<SkinnedMeshRenderer>(true);
                        if (smr != null)
                        {
                            TurnrootLogger.Log(
                                $"CreateOutfitMesh: Using class outfit prefab for {unit.CharacterTemplate?.DisplayName}"
                            );
                            return smr;
                        }
                        else
                        {
                            TurnrootLogger.Log(
                                $"CreateOutfitMesh: Class outfit prefab '{prefab.name}' is missing a SkinnedMeshRenderer. Falling back to NonBattleOutfitPrefab for {unit.CharacterTemplate?.DisplayName}",
                                TurnrootLogger.LogLevel.Warning
                            );
                            try
                            {
                                Destroy(obj);
                            }
                            catch { }
                            // Fall through to non-battle fallback below
                        }
                    }
                }
            }

            // If not using battle model or class prefab failed, use the per-character non-battle outfit prefab.
            var nbPrefab = unit.CharacterTemplate.NonBattleOutfitPrefab;
            if (nbPrefab != null)
            {
                var nbInstance = Instantiate(nbPrefab, parent.transform);
                nbInstance.name = "NonBattleOutfit";
                var nbSmr = nbInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (nbSmr == null)
                {
                    TurnrootLogger.Log(
                        $"CreateOutfitMesh: Non-battle outfit prefab '{nbPrefab.name}' does not contain a SkinnedMeshRenderer. Cannot create outfit for {unit.CharacterTemplate?.DisplayName}",
                        TurnrootLogger.LogLevel.Error
                    );
                    try
                    {
                        Destroy(nbInstance);
                    }
                    catch { }
                    return null;
                }

                TurnrootLogger.Log(
                    $"CreateOutfitMesh: Using non-battle outfit prefab for {unit.CharacterTemplate?.DisplayName}"
                );

                // Ensure the non-battle instance uses the prefab's original materials.
                // This avoids carrying over any class-created materials from prior runs.
                var prefabSmr = nbPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (prefabSmr != null)
                {
                    nbSmr.sharedMaterials = prefabSmr.sharedMaterials;
                }

                // Attach head/hands and hair if available
                if (unit.CharacterTemplate.HeadAndHandsPrefab != null)
                {
                    var hh = Instantiate(
                        unit.CharacterTemplate.HeadAndHandsPrefab,
                        parent.transform
                    );
                    hh.name = "HeadAndHands";
                }

                if (unit.CharacterTemplate.HairPrefab != null)
                {
                    var hair = Instantiate(unit.CharacterTemplate.HairPrefab, parent.transform);
                    hair.name = "Hair";
                }

                return nbSmr;
            }

            TurnrootLogger.Log(
                $"CreateOutfitMesh: No suitable outfit found for {unit.CharacterTemplate?.DisplayName}. Ensure class model or NonBattleOutfitPrefab is assigned.",
                TurnrootLogger.LogLevel.Error
            );
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

            model.SetActive(false);

            PublishDespawnEvent(model, pos);
            Destroy(model);
            models.Remove(pos);

            return OperationResult.Successful();
        }

        /// <summary>
        /// Public helper used by precompute systems to ensure a model exists for the unit at the
        /// specified grid position. This uses the brain's internal _activeUnitModels dictionary
        /// and returns an OperationResult so callers can report progress/failures.
        /// </summary>
        public OperationResult PrecomputeSpawnModelAt(
            CharacterInstance unit,
            Vector2Int pos,
            bool prebattle = false
        )
        {
            if (unit == null)
            {
                return OperationResult.Failure("Unit is null");
            }

            try
            {
                return SpawnUnitModelOnGrid(pos, unit, _activeUnitModels, prebattle);
            }
            catch (System.Exception ex)
            {
                return OperationResult.Failure($"PrecomputeSpawnModelAt failed: {ex.Message}");
            }
        }

        private void ClearExistingModels()
        {
            foreach (var kvp in _activeUnitModels.ToList())
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);

                    Destroy(kvp.Value);
                }
            }
            _activeUnitModels.Clear();
        }
    }
}
