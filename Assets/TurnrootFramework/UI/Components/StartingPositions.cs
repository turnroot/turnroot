using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.PreBattle;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.UI.Components
{
    public class StartingPositions : MonoBehaviour
    {
        public List<GameObject> TileProjectors;
        public GameObject Selected;
        public GameObject Swap;
        public UnitCellDataOnly SelectedUnit;
        public GameObject SwapGraphic;
        public UnitCellDataOnly SwapUnit;
        private MapGrid mapGrid;
        private Dictionary<Vector2Int, GameObject> _unitModels = new();

        private BattlePreparationObject _prepObject;

        // If this instance has been superseded by a newer StartingPositions instance,
        // avoid spawning any further models (prevents duplicate visible models when multiple UI instances exist).
        private bool _replaced = false;

        public OperationResult Initialize(BattlePreparationObject battlePreparationObject)
        {
            mapGrid = battlePreparationObject.MapGrid;

            // If this instance was replaced by a newer StartingPositions, skip initialization to avoid duplicate models.
            if (_replaced)
            {
                return OperationResult.SuccessResult();
            }

            var StartingPositions = mapGrid.PlayerTeamSpawnPoints;
            if (StartingPositions == null || StartingPositions.Count <= 0)
            {
                return OperationResult.Failure("Improper starting positions");
            }
            for (var i = 0; i < StartingPositions.Count; i++)
            {
                var coordinates = StartingPositions[i];
                SetUpDecalProjector(i, coordinates);
            }
            var DecalCount = TileProjectors.Count;
            for (var j = StartingPositions.Count; j < DecalCount; j++)
            {
                TileProjectors[j].SetActive(false);
            }
            Selected.SetActive(false);
            Swap.SetActive(false);
            SwapGraphic.SetActive(false);
            SelectedUnit.gameObject.SetActive(false);
            SwapUnit.gameObject.SetActive(false);

            _prepObject = battlePreparationObject;

            // If another StartingPositions already exists in the scene, ask it to despawn its models
            // and unsubscribe so we don't end up with duplicate spawned models across multiple UI instances.
            var all = FindObjectsByType<StartingPositions>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var other in all)
            {
                if (other == null || other == this)
                {
                    continue;
                }

                other.ReplaceBy(this);
            }

            // If placements were not initialized yet, attempt to initialize them now.
            if (_prepObject.placements == null || _prepObject.placements.Count == 0)
            {
                _prepObject.InitializePlacements();
                // If placements still not available, subscribe to placements-initialized and wait to spawn models.
                if (_prepObject.placements == null || _prepObject.placements.Count == 0)
                {
                    if (_prepObject.Brain != null)
                    {
                        _prepObject.Brain.OnPlacementsInitialized += HandlePlacementsInitialized;
                        // Subscribe to unit selection changes so we can refresh spawned models when selections change
                        _prepObject.Brain.OnUnitSelectionChanged += HandleUnitSelectionChanged;
#if UNITY_EDITOR
                        Debug.Log(
                            "StartingPositions.Initialize: Subscribed to placement and selection events to wait for placements and update models."
                        );
#endif
                    }
                    return OperationResult.SuccessResult();
                }
            }

            SpawnAllUnitModels();
            return OperationResult.SuccessResult();
        }

        private void SetUpDecalProjector(int index, Vector2Int tileCoordinates)
        {
            var projector = TileProjectors[index];
            var worldPosition = mapGrid.GetTerrainAdjustedWorldPosition(tileCoordinates);

            projector.transform.position =
                worldPosition + new Vector3(0, mapGrid.GridScale / 2f, 0f);
        }

        public void SetSelected(Vector2Int tileCoordinates)
        {
            var worldPosition = mapGrid.GetTerrainAdjustedWorldPosition(tileCoordinates);
            Selected.transform.position =
                worldPosition + new Vector3(0, mapGrid.GridScale / 2f, 0f);
            // Ensure object is active for fade and call Show if a UIFade component exists
            Selected.SetActive(true);
            if (Selected.TryGetComponent<UIFade>(out var selectedFade))
            {
                selectedFade.Show();
            }

            // When a unit becomes selected, ensure swap visuals are hidden until a hover occurs
            HideWithFade(Swap);
            HideWithFade(SwapGraphic);
        }

        public void SetSwap(Vector2Int tileCoordinates)
        {
            var worldPosition = mapGrid.GetTerrainAdjustedWorldPosition(tileCoordinates);
            Swap.transform.position = worldPosition + new Vector3(0, mapGrid.GridScale / 2f, 0f);
            Swap.SetActive(true);
            if (Swap.TryGetComponent<UIFade>(out var swapFade))
            {
                swapFade.Show();
            }
        }

        public void Clears()
        {
            // No selection: hide all preview visuals via UIFade
            HideWithFade(Selected);
            HideWithFade(Swap);
            HideWithFade(SwapGraphic);
            HideWithFade(SelectedUnit.gameObject);
            HideWithFade(SwapUnit.gameObject);
            SelectedUnit.ClearData();
            SwapUnit.ClearData();
        }

        public void SetSelectedUnit(string name, string className, Sprite portrait)
        {
            SelectedUnit.SetData(name, className, portrait);
            SelectedUnit.gameObject.SetActive(true);
            if (SelectedUnit.gameObject.TryGetComponent<UIFade>(out var selUnitFade))
            {
                selUnitFade.Show();
            }
        }

        public void SetSwapUnit(string name, string className, Sprite portrait)
        {
            SwapUnit.SetData(name, className, portrait);
            SwapUnit.gameObject.SetActive(true);
            if (SwapUnit.gameObject.TryGetComponent<UIFade>(out var swapUnitFade))
            {
                swapUnitFade.Show();
            }
            SwapGraphic.SetActive(true);
            if (SwapGraphic.TryGetComponent<UIFade>(out var swapGraphicFade))
            {
                swapGraphicFade.Show();
            }
        }

        public void ClearSelectedUnit() => SelectedUnit.ClearData();

        public void ClearSwapUnit()
        {
            SwapUnit.ClearData();
            HideWithFade(SwapUnit.gameObject);
            HideWithFade(SwapGraphic);
        }

        /// <summary>
        /// Clears swap preview visuals (swap projector and swap unit data) without
        /// affecting the selected unit visuals.
        /// </summary>
        public void ClearSwapPreview()
        {
            HideWithFade(Swap);
            SwapUnit.ClearData();
            HideWithFade(SwapUnit.gameObject);
            HideWithFade(SwapGraphic);
        }

        private void HideWithFade(GameObject go)
        {
            if (go == null || !go.activeInHierarchy)
            {
                return;
            }

            if (go.TryGetComponent<UIFade>(out var fade))
            {
                fade.Hide();
                // Let UIFade handle disabling the object when done
            }
            else
            {
                go.SetActive(false);
            }
        }

        /* ------------------------------ Spawn models ------------------------------ */
        private void SpawnAllUnitModels()
        {
            if (_replaced)
            {
                return;
            }

            // Destroy any orphaned unit model objects in the scene that are not part of the current placements.
            // This handles cases where models were created outside of our mapping (leftover duplicates etc).
            var keepIds = new HashSet<string>();
            if (_prepObject?.placements != null)
            {
                foreach (var p in _prepObject.placements)
                {
                    if (p.Value != null && !string.IsNullOrEmpty(p.Value.Id))
                    {
                        keepIds.Add(p.Value.Id);
                    }
                }
            }

            var ownerships = FindObjectsByType<UnitModelOwnership>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
            foreach (var own in ownerships)
            {
                if (own == null || string.IsNullOrEmpty(own.UnitId))
                {
                    continue;
                }

                if (!keepIds.Contains(own.UnitId))
                {
                    try
                    {
                        own.gameObject.SetActive(false);
                    }
                    catch { }
                    Destroy(own.gameObject);

                    // Also remove any mapping referencing this object
                    var keys = new List<Vector2Int>(_unitModels.Keys);
                    foreach (var k in keys)
                    {
                        if (_unitModels.TryGetValue(k, out var obj) && obj == own.gameObject)
                        {
                            _unitModels.Remove(k);
                        }
                    }
                }
            }

            if (_unitModels.Count > 0)
            {
                // Despawn existing models first; take a snapshot of keys to avoid modifying the dictionary while iterating.
                var keys = new List<Vector2Int>(_unitModels.Keys);
                foreach (var pos in keys)
                {
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnitModelFromGrid(
                        pos,
                        _unitModels
                    );
                }
            }

            if (_prepObject?.placements == null || _prepObject.placements.Count == 0)
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log($"SpawnAllUnitModels: spawning {_prepObject.placements.Count} unit models.");
#endif
            foreach (var placement in _prepObject.placements)
            {
                // We're in the pre-battle/roster positioning UI, so tell the appearance brain to spawn prebattle models.
                _prepObject.Brain.unitAppearanceBrain.SpawnUnitModelOnGrid(
                    placement.Key,
                    placement.Value,
                    _unitModels,
                    prebattle: true
                );
            }
        }

        private void HandlePlacementsInitialized()
        {
            // Unsubscribe to avoid duplicate handling
            if (_prepObject == null || _prepObject.Brain == null)
            {
                return;
            }

            _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;

            if (_prepObject.placements != null && _prepObject.placements.Count > 0)
            {
#if UNITY_EDITOR
                Debug.Log(
                    "HandlePlacementsInitialized: placements are now available, spawning unit models."
                );
#endif
                SpawnAllUnitModels();
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning(
                    "HandlePlacementsInitialized: placements still empty after initialization."
                );
#endif
            }
        }

        private void HandleUnitSelectionChanged(CharacterInstance unit, bool selected)
        {
            if (_prepObject == null || _prepObject.Brain == null)
            {
                return;
            }

            // Recompute placements and refresh models when selection changes in the brain
            _prepObject.InitializePlacements();
            SpawnAllUnitModels();
        }

        /// <summary>
        /// Instructs this instance to despawn all models and unsubscribe from events.
        /// Called by newer StartingPositions instances that replace this one to avoid duplicate spawns.
        /// </summary>
        public void ReplaceBy(StartingPositions newOwner)
        {
            // Prevent this instance from spawning further models
            _replaced = true;

            // Try to update the preparation object to point at the new owner's starting positions
            if (_prepObject != null)
            {
                _prepObject.StartingPositionsComponent = newOwner;
            }

            // Unsubscribe from brain events if applicable
            if (_prepObject != null && _prepObject.Brain != null)
            {
                _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;
                _prepObject.Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
            }

#if UNITY_EDITOR
            Debug.Log(
                $"StartingPositions.ReplaceBy: {name} was replaced by {newOwner.name}, despawning {_unitModels.Count} models and unsubscribing."
            );
#endif

            // Despawn any existing models spawned by this instance
            DespawnAllModels();
        }

        /// <summary>
        /// Remove all models spawned by this StartingPositions instance.
        /// </summary>
        public void DespawnAllModels()
        {
            if (_unitModels == null || _unitModels.Count == 0)
            {
                return;
            }

#if UNITY_EDITOR
            Debug.Log(
                $"StartingPositions.DespawnAllModels: {name} despawning {_unitModels.Count} models."
            );
#endif

            var keys = new List<Vector2Int>(_unitModels.Keys);
            foreach (var pos in keys)
            {
                // Use the brain associated with this prep object, if available
                if (_prepObject != null && _prepObject.Brain != null)
                {
                    _prepObject.Brain.unitAppearanceBrain.DespawnUnitModelFromGrid(
                        pos,
                        _unitModels
                    );
                }
                else
                {
                    // Fallback: directly remove/destroy local model entries
                    if (_unitModels.TryGetValue(pos, out var m) && m != null)
                    {
                        try
                        {
                            m.SetActive(false);
                        }
                        catch { }
                        Destroy(m);
                        _unitModels.Remove(pos);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            // Ensure we remove any models we own when this object goes away
            DespawnAllModels();

            if (_prepObject != null && _prepObject.Brain != null)
            {
                _prepObject.Brain.OnPlacementsInitialized -= HandlePlacementsInitialized;
                _prepObject.Brain.OnUnitSelectionChanged -= HandleUnitSelectionChanged;
            }
        }
    }
}
