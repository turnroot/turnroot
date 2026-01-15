using System.Collections;
using System.Collections.Generic;
using Turnroot.Characters;
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

        public OperationResult Initialize(BattlePreparationObject battlePreparationObject)
        {
            mapGrid = battlePreparationObject.MapGrid;
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

            // Store reference to prep object before spawning models so SpawnAllUnitModels can access placements.
            _prepObject = battlePreparationObject;

            // If placements were not initialized yet, attempt to initialize them now.
            if (_prepObject.placements == null || _prepObject.placements.Count == 0)
            {
                var initResult = _prepObject.InitializePlacements();
                if (!initResult.Success)
                {
                    Debug.LogWarning(
                        $"StartingPositions.Initialize: InitializePlacements failed: {initResult.ErrorMessage}"
                    );
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
            StartCoroutine(HideAfterFade(Swap));
            StartCoroutine(HideAfterFade(SwapGraphic));
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
            StartCoroutine(HideAfterFade(Selected));
            StartCoroutine(HideAfterFade(Swap));
            StartCoroutine(HideAfterFade(SwapGraphic));
            StartCoroutine(HideAfterFade(SelectedUnit.gameObject));
            StartCoroutine(HideAfterFade(SwapUnit.gameObject));
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
            StartCoroutine(HideAfterFade(SwapUnit.gameObject));
            StartCoroutine(HideAfterFade(SwapGraphic));
        }

        /// <summary>
        /// Clears swap preview visuals (swap projector and swap unit data) without
        /// affecting the selected unit visuals.
        /// </summary>
        public void ClearSwapPreview()
        {
            StartCoroutine(HideAfterFade(Swap));
            SwapUnit.ClearData();
            StartCoroutine(HideAfterFade(SwapUnit.gameObject));
            StartCoroutine(HideAfterFade(SwapGraphic));
        }

        private IEnumerator HideAfterFade(GameObject go)
        {
            if (go.TryGetComponent<UIFade>(out var fade))
            {
                fade.Hide();
                yield return new WaitForSeconds(fade.lerpTime + 0.02f);
                go.SetActive(false);
            }
            else
            {
                // No UIFade present, disable immediately
                go.SetActive(false);
                yield break;
            }
        }

        /* ------------------------------ Spawn models ------------------------------ */
        private void SpawnAllUnitModels()
        {
            Debug.Log("Starting SpawnAllUnitModels");
            if (_prepObject?.placements == null)
            {
                Debug.Log("No placements found, aborting SpawnAllUnitModels");
                return;
            }

            foreach (var placement in _prepObject.placements)
            {
                Debug.Log($"Spawning unit model for {placement.Key} at {placement.Value}");
                _prepObject.Brain.unitAppearanceBrain.SpawnUnitModelOnGrid(
                    placement.Key,
                    placement.Value,
                    _unitModels
                );
            }
        }
    }
}
