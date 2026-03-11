using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Grid Point Helpers

        private void CreateGridPoint(int row, int col)
        {
            var point = new GameObject($"Point_R{row}_C{col}");
            var gridPoint = point.AddComponent<MapGridPoint>();
            gridPoint.Initialize(row, col);
            SetDefaultTerrainType(gridPoint);

            point.transform.parent = transform;
            point.transform.localPosition =
                new Vector3(row * GridScale, 0, col * GridScale) + _gridOffset;

            var key = new Vector2Int(row, col);
            _gridPoints[key] = point;
            _cachedGridPoints ??= new Dictionary<Vector2Int, MapGridPoint>();
            _cachedGridPoints[key] = gridPoint;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(point);
            UnityEditor.EditorUtility.SetDirty(gridPoint);

            // Keep editor color cache up-to-date for this new point
            var tt = gridPoint.GetCachedTerrainType();
            var _col = tt?.EditorColor ?? Color.yellow;
            _gridPointColorCache[new Vector2Int(row, col)] = _col;
#endif
        }

        private void RemoveGridLine(int index, bool isRow)
        {
            int outerLimit = isRow ? GridWidth : GridHeight;

            for (int i = 0; i < outerLimit; i++)
            {
                var key = isRow ? new Vector2Int(i, index) : new Vector2Int(index, i);
                var mgp = GetGridPoint(key.x, key.y);
                if (mgp == null)
                {
                    continue;
                }

                _gridPoints.Remove(key);
                DestroyImmediate(mgp.gameObject);
            }
        }

        private void SetDefaultTerrainType(MapGridPoint gridPoint)
        {
            var terrainAsset = TerrainTypes.LoadDefault();
            if (terrainAsset?.Types == null)
            {
                return;
            }

            var voidType = terrainAsset.Types.FirstOrDefault(t =>
                t != null && t.Name.Equals("Void", System.StringComparison.OrdinalIgnoreCase)
            );

            if (voidType != null)
            {
                gridPoint.SetTerrainTypeId(voidType.Id);
            }
            else if (terrainAsset.Types.Length > 0 && terrainAsset.Types[0] != null)
            {
                gridPoint.SetTerrainTypeId(terrainAsset.Types[0].Id);
            }
        }

        #endregion
    }
}
