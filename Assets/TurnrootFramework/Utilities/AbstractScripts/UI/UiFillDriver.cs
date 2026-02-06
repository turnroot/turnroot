using UnityEngine;

namespace Turnroot.Utilities
{
    /// <summary>
    /// Drives a material's fill amount parameter, animating UI fill effects.
    /// </summary>
    public class UiFillDriver : MonoBehaviour
    {
        public Material material;
        public float Amount = 0;

        private void Update() => material?.SetFloat("_Amount", Amount);

        public void SetAmount(float amount) => Amount = amount;

        private void Awake()
        {
            material?.SetFloat("_Amount", 0f);
            Amount = 0;
        }

        private void OnDestroy()
        {
            material?.SetFloat("_Amount", 0f);
            Amount = 0;
        }
    }
}
