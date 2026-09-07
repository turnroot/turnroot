using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Utilities
{
    public static class ManageObjectsEnabled
    {
        public static void SetVisibilityStateOfObjectsToHide(Dictionary<GameObject, bool> originalVisibilityState, GameObject[] objectsToHide)
        {
            originalVisibilityState.Clear();
            if (objectsToHide != null)
            {
                foreach (var obj in objectsToHide)
                {
                    if (obj != null)
                    {
                        originalVisibilityState[obj] = obj.activeSelf;
                    }
                }
            }
        }

        public static void RestoreVisibilityStateOfObjectsThatWereHidden(Dictionary<GameObject, bool> originalVisibilityState)
        {
            if (originalVisibilityState != null)
            {
                foreach (var kvp in originalVisibilityState)
                {
                    if (kvp.Key != null)
                    {
                        kvp.Key.SetActive(kvp.Value);
                    }
                }
            }
        }

        public static void HideObjectsToHide(GameObject[] objectsToHide)
        {
            if (objectsToHide != null)
            {
                foreach (var obj in objectsToHide)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }
    }
}