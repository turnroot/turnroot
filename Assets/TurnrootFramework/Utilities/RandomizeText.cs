using UnityEngine;

namespace Turnroot.Utilities
{
    [RequireComponent(typeof(TMPro.TextMeshPro))]
    public class RandomizeText : MonoBehaviour
    {
        public string[] options;
        private TMPro.TextMeshPro textMeshPro;

        private void Awake()
        {
            textMeshPro = GetComponent<TMPro.TextMeshPro>();
            textMeshPro.text = options[Random.Range(0, options.Length)];
        }
    }
}
