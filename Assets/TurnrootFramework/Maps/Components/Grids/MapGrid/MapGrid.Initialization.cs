using System.Linq;
using Turnroot.Gameplay.Combat;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Initialization and Validation

        private void Awake()
        {
            // Rebuild dictionaries from existing children at runtime
            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                if (transform.childCount > 0)
                {
                    RebuildGridDictionary();
                }
                // _single3dHeightMesh.SetActive(false);
            }

            // Ensure cache is built
            EnsureCachedGridPoints();

            // Rebuild raycast colors for gizmos in play mode
            if (
                _single3dHeightMeshRaycastPoints != null
                && _single3dHeightMeshRaycastPoints.Length > 0
            )
            {
                RebuildRaycastColors();
            }

            if (UseHeightMeshAsTerrainModel)
            {
                if (_single3dHeightMesh != null)
                {
                    _single3dHeightMesh.SetActive(UseHeightMeshAsTerrainModel);
                    // _single3dHeightMesh.GetComponent<Renderer>().renderingLayerMask = (uint)GetRenderingLayerMask.Get("Receive Map Grid Decals").value;
                }
            }
            else
            {
                if (TerrainLevelModel != null)
                {
                    TerrainLevelModel.SetActive(!UseHeightMeshAsTerrainModel);
                    // TerrainLevelModel.GetComponent<Renderer>().renderingLayerMask = (uint)GetRenderingLayerMask.Get("Receive Map Grid Decals").value;
                }
                else
                {
                    "MapGrid: No TerrainLevelModel assigned while UseHeightMeshAsTerrainModel is false.".LogError();
                }
            }
        }

        public void EnsureGridPoints()
        {
            int expectedCount = GridWidth * GridHeight;
            int actualCount = transform
                .Cast<Transform>()
                .Count(child => child != null && child.TryGetComponent<MapGridPoint>(out _));

            bool needsRebuild =
                actualCount > 0
                && (actualCount != expectedCount || _gridPoints?.Count != expectedCount);
            bool needsCreate = actualCount == 0;

            if (needsRebuild)
            {
                RebuildGridDictionary();
                "MapGrid: Rebuilt grid dictionary from existing children.".LogInfo();
            }
            else if (needsCreate)
            {
                CreateChildrenPoints();
                "MapGrid: Created missing grid points.".LogInfo();
            }
            else if (_gridPoints?.Count == 0 && transform.childCount > 0)
            {
                RebuildGridDictionary();
                "MapGrid: Rebuilt grid dictionary from existing children.".LogInfo();
            }

            RepositionGridPoints();
            "MapGrid: Grid points ensured.".LogInfo();
        }

        private void RepositionGridPoints()
        {
            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                return;
            }

            foreach (var kv in _gridPoints)
            {
                if (kv.Value == null)
                {
                    continue;
                }

                kv.Value.transform.localPosition =
                    new Vector3(kv.Key.x * GridScale, 0, kv.Key.y * GridScale) + _gridOffset;
            }
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif

            if (UseHeightMeshAsTerrainModel)
            {
                TerrainLevelModel = _single3dHeightMesh;
            }

            // get the BattleGameObject in the parent (includes self)
            battleGameObject = GetComponentInParent<BattleGameObject>();
            if (PlayerTeamSpawnPoints.Count > battleGameObject.MaxPlayerTeamUnits)
            {
                Debug.LogWarning(
                    $"MapGrid: Trimming PlayerTeamSpawnPoints to max allowed units ({battleGameObject.MaxPlayerTeamUnits})"
                );
                PlayerTeamSpawnPoints = PlayerTeamSpawnPoints
                    .Take(battleGameObject.MaxPlayerTeamUnits)
                    .ToList();
            }

            if (_gridPoints == null || _gridPoints.Count == 0)
            {
                if (transform.childCount > 0)
                {
                    RebuildGridDictionary();
                }
            }

            RepositionGridPoints();

            if (_features != null && _features.Count > 0)
            {
                LoadFeatureLayer();
            }

            if (
                _single3dHeightMeshRaycastPoints != null
                && _single3dHeightMeshRaycastPoints.Length > 0
            )
            {
                RebuildRaycastColors();
            }

#if UNITY_EDITOR
            // Keep editor color cache in sync when validating in the Inspector
            RebuildEditorPointColorCache();
#endif

#if UNITY_EDITOR
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        #endregion
    }
}