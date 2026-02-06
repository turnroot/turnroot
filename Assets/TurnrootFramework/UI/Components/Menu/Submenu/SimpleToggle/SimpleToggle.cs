using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Menu.Submenu
{
    [RequireComponent(typeof(Image))]
    public class SimpleToggle : MonoBehaviour
    {
        public Image backgroundImage;
        public Image checkmarkImage;

        [Header("Colors")]
        [SerializeField]
        private Color normalColor = Color.white;

        [SerializeField]
        private Color highlightedColor = Color.yellow;

        [Header("State")]
        [SerializeField]
        private bool _isOn;

        public UnityEvent<bool> onValueChanged = new();

        public bool isOn
        {
            get => _isOn;
            set
            {
                if (_isOn != value)
                {
                    _isOn = value;
                    UpdateCheckmark();
                    onValueChanged?.Invoke(_isOn);
                }
            }
        }

        private void Awake()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            UpdateCheckmark();
            SetHighlighted(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = highlighted ? highlightedColor : normalColor;
            }
        }

        public void Toggle() => isOn = !isOn;

        private void UpdateCheckmark()
        {
            if (checkmarkImage != null)
            {
                checkmarkImage.enabled = _isOn;
            }
        }

        public void SetColors(Color normal, Color highlighted)
        {
            normalColor = normal;
            highlightedColor = highlighted;
        }
    }
}
