using NaughtyAttributes;
using UnityEngine;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Cycles through an array of hint text strings with fade in/out transitions.
    /// </summary>
    public class HintText : MonoBehaviour
    {
        public int HintIndex = 0;
        public float FadeTime = 0.2f;
        public float VisibleTime = 2.5f;

        private float _timeElapsed = 0.0f;

        [ReorderableList]
        public string[] Hints;

        public TMPro.TextMeshProUGUI _textComponent;

        private void OnDestroy()
        {
            HintIndex = 0;
            _timeElapsed = 0.0f;
        }

        private void Start()
        {
            HintIndex = Random.Range(0, Hints.Length);
            _timeElapsed = 0.0f;
        }

        private void Update()
        {
            _timeElapsed += Time.deltaTime;
            if (_timeElapsed >= VisibleTime + FadeTime * 2)
            {
                _timeElapsed = 0.0f;
                HintIndex = (HintIndex + 1) % Hints.Length;
            }
            else if (_timeElapsed < FadeTime)
            {
                float alpha = _timeElapsed / FadeTime;
                _textComponent.alpha = alpha;
            }
            else if (_timeElapsed < VisibleTime + FadeTime)
            {
                _textComponent.alpha = 1.0f;
            }
            else
            {
                float alpha = 1.0f - (_timeElapsed - VisibleTime - FadeTime) / FadeTime;
                _textComponent.alpha = alpha;
            }

            _textComponent.text = Hints[HintIndex];
        }
    }
}
