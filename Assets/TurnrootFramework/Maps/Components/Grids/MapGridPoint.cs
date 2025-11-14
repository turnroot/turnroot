using UnityEngine;

public class MapGridPoint : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Gizmo sphere radius (world units)")]
    private float _gizmoRadius = 0.15f;

    [SerializeField]
    [Tooltip(
        "ID of the terrain type from the TerrainTypes asset. If empty, the editor/runtime will try to find the default asset and allow selection."
    )]
    private string _terrainTypeId = string.Empty;

    public TerrainType SelectedTerrainType
    {
        get
        {
            var asset = FindDefaultTerrainTypesAsset();
            if (asset == null)
                return null;
            var t = asset.GetTypeById(_terrainTypeId);
            if (t != null)
                return t;
            // fallback to first type if available
            if (asset.Types != null && asset.Types.Length > 0)
                return asset.Types[0];
            return null;
        }
    }

    private void OnDrawGizmos()
    {
        var tt = SelectedTerrainType;
        if (tt != null)
        {
            Gizmos.color = tt.EditorColor;
        }
        else
        {
            Gizmos.color = Color.white;
        }
        Gizmos.DrawSphere(transform.position, _gizmoRadius);
    }

    private void OnDrawGizmosSelected()
    {
        var tt = SelectedTerrainType;
        if (tt != null)
        {
            Gizmos.color = tt.EditorColor;
            Gizmos.DrawSphere(transform.position, _gizmoRadius);
            Gizmos.DrawWireSphere(transform.position, _gizmoRadius * 1.5f);
        }
    }

    private TerrainTypes FindDefaultTerrainTypesAsset()
    {
        // Use the centralized loader which prefers Resources at runtime and falls back to
        // AssetDatabase in the editor.
        return TerrainTypes.LoadDefault();
    }
}
