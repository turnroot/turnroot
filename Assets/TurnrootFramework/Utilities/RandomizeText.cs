using UnityEngine;

namespace Turnroot.Utilities
{
    [RequireComponent(typeof(TMPro.TextMeshProUGUI))]
    public class RandomizeText : MonoBehaviour
    {
        public string[] options;
        private TMPro.TextMeshProUGUI textMeshPro;

        private void Awake()
        {
            textMeshPro = GetComponent<TMPro.TextMeshProUGUI>();
            textMeshPro.text = options[Random.Range(0, options.Length)];
        }
    }
}
