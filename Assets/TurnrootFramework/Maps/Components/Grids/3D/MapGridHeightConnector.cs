using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapGridHeightConnector
{
    public Vector3[] RaycastPointsDownTo3DMap(
        GameObject mapObject,
        Dictionary<Vector2Int, GameObject> gridPoints,
        LayerMask layerMask
    )
    {
        Vector3[] raycastPoints = new Vector3[gridPoints.Count];
        int index = 0;

        // If the provided 3D map object is null, simply return original positions
        if (mapObject == null)
        {
            // Return original positions in consistent order
            var ordered = gridPoints.OrderBy(kv => kv.Key.x).ThenBy(kv => kv.Key.y);
            foreach (var kv in ordered)
            {
                var point = kv.Value;
                raycastPoints[index++] = point.transform.position;
            }
            return raycastPoints;
        }

        // Cache the transform for hierarchy checks
        var targetRoot = mapObject.transform;

        // Iterate the provided grid points in consistent order
        var ordered = gridPoints.OrderBy(kv => kv.Key.x).ThenBy(kv => kv.Key.y);
        foreach (var kv in ordered)
        {
            var point = kv.Value;
            Vector3 rayOrigin = point.transform.position + Vector3.up * 50f; // Start the ray well above the grid point
            Ray ray = new Ray(rayOrigin, Vector3.down);

            int mask = layerMask.value;
            if (mask == 0)
            {
                mask = ~0;
            }

            // Use RaycastAll so we can filter hits that belong to the provided 3D object (or its children)
            // Include trigger colliders to be robust
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                200f,
                mask,
                QueryTriggerInteraction.Collide
            );

            // Sort hits by distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool found = false;
            foreach (var hit in hits)
            {
                if (hit.collider == null)
                {
                    continue;
                }

                var hitRoot = hit.collider.transform;
                // Walk up the hierarchy to see if this collider belongs to the provided object
                while (hitRoot != null)
                {
                    if (hitRoot == targetRoot)
                    {
                        raycastPoints[index] = hit.point;
                        found = true;
                        break;
                    }
                    hitRoot = hitRoot.parent;
                }
                if (found)
                {
                    break;
                }
            }

            if (!found)
            {
                // If no hit on the target object, fallback to first hit overall (if any)
                if (hits.Length > 0 && hits[0].collider != null)
                {
                    raycastPoints[index] = hits[0].point;
                }
                else
                {
                    raycastPoints[index] = point.transform.position; // Fallback to original position if no hit
                }
            }

            index++;
        }

        return raycastPoints;
    }
}
