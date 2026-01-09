using UnityEngine;

namespace Turnroot.Utilities
{
    public partial class UtilityFunctions
    {
        public Transform FindChildByTag(GameObject root, string tag)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.CompareTag(tag))
                {
                    return t;
                }
            }
            return null;
        }
    }
}
