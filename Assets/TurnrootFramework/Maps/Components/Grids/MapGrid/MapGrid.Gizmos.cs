using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGrid : MonoBehaviour
    {
        #region Gizmos

        private void OnDrawGizmos()
        {
            Vector3 getPos(int x, int y) =>
                transform.position + new Vector3(x * GridScale, 0, y * GridScale) + _gridOffset;

            Vector3 topLeft = getPos(0, 0);
            Vector3 topRight = getPos(GridWidth - 1, 0);
            Vector3 bottomLeft = getPos(0, GridHeight - 1);
            Vector3 bottomRight = getPos(GridWidth - 1, GridHeight - 1);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            var corners = new[] { topLeft, topRight, bottomLeft, bottomRight };
            foreach (var corner in corners)
            {
                Gizmos.DrawSphere(corner, 1f);
            }

            if (_showRaycastGizmos)
            {
                float s = 0.25f * GridScale;

                // Prefer explicit raycast points when available (from a connected 3D height mesh)
                if (
                    _single3dHeightMeshRaycastPoints != null
                    && _single3dHeightMeshRaycastPoints.Length > 0
                )
                {
                    for (int i = 0; i < _single3dHeightMeshRaycastPoints.Length; i++)
                    {
                        var p = _single3dHeightMeshRaycastPoints[i];
                        var c =
                            (
                                _single3dHeightMeshRaycastColors != null
                                && i < _single3dHeightMeshRaycastColors.Length
                            )
                                ? _single3dHeightMeshRaycastColors[i]
                                : Color.magenta;
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawSphere(p, s * (_showRaycastCoordinates ? 0.5f : 1f));
                        // add a Handle Label with coordinates
                        if (_showRaycastCoordinates)
                        {
                            Gizmos.color = Color.white;
#if UNITY_EDITOR
                            UnityEditor.Handles.Label(
                                p + (Vector3.up * s * 2f),
                                _single3dHeightMeshRaycastIndices != null
                                && i < _single3dHeightMeshRaycastIndices.Length
                                    ? $"({_single3dHeightMeshRaycastIndices[i].x}, {_single3dHeightMeshRaycastIndices[i].y})"
                                    : "(?, ?)"
                            );
#endif
                        }
                    }
                }
                else if (_gridPoints != null && _gridPoints.Count > 0)
                {
                    // Performance guard: avoid expensive per-object checks and labels when grid is large.
                    bool heavy = _gridPoints.Count > 999;
                    bool showLabels = _showRaycastCoordinates && !heavy;

                    // Ensure we have map point references available quickly
                    EnsureCachedGridPoints();

                    foreach (var kv in _gridPoints)
                    {
                        var go = kv.Value;
                        if (go == null)
                        {
                            continue;
                        }

                        var p = go.transform.position;
                        Color c = Color.yellow;

#if UNITY_EDITOR
                        // Prefer the editor color cache first (fast, populated by editor tools)
                        if (TryGetEditorPointColor(kv.Key, out var cachedColor))
                        {
                            c = cachedColor;
                        }
                        // Then prefer the MapGrid's cached MapGridPoint lookup (cheap)
                        else if (
                            _cachedGridPoints != null
                            && _cachedGridPoints.TryGetValue(kv.Key, out var mgp)
                        )
                        {
                            var tt = mgp?.GetCachedTerrainType();
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
                        // As a last resort (small grids), try component lookup
                        else if (!heavy && go.TryGetComponent<MapGridPoint>(out var mgp2))
                        {
                            var tt = mgp2?.SelectedTerrainType;
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
#else
                        if (
                            _cachedGridPoints != null
                            && _cachedGridPoints.TryGetValue(kv.Key, out var mgp)
                        )
                        {
                            var tt = mgp?.GetCachedTerrainType();
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
                        else if (!heavy && go.TryGetComponent<MapGridPoint>(out var mgp2))
                        {
                            var tt = mgp2?.SelectedTerrainType;
                            c = tt != null ? tt.EditorColor : Color.yellow;
                        }
#endif
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawSphere(p, s * (showLabels ? 0.5f : 1f));

                        if (showLabels)
                        {
                            Gizmos.color = Color.white;
#if UNITY_EDITOR
                            UnityEditor.Handles.Label(
                                p + (Vector3.up * s * 2f),
                                $"({kv.Key.x}, {kv.Key.y})"
                            );
#endif
                        }
                    }

                    if (heavy)
                    {
                        // Small visual indicator when full labeling is suppressed for performance.
                        Gizmos.color = Color.gray;
                        Gizmos.DrawSphere(
                            transform.position + (Vector3.up * 0.1f),
                            0.02f * GridScale
                        );
                    }
                }
            }
        }

        #endregion
    }
}
