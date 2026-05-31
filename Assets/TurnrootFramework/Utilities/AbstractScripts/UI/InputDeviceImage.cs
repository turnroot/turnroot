using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Turnroot.Utilities.AbstractScripts
{
    /// <summary>
    /// Swaps a UI Image sprite between keyboard and gamepad variants based on
    /// whether any gamepad is currently connected. Updates automatically when
    /// devices are plugged in or removed during play.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class InputDeviceImage : MonoBehaviour
    {
        public Image TargetImage;
        public Sprite KeyboardImage;
        public Sprite GamepadImage;

        private void OnEnable()
        {
            InputSystem.onDeviceChange += OnDeviceChange;
            Refresh();
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (TargetImage == null)
            {
                return;
            }

            TargetImage.sprite = Gamepad.all.Count > 0 ? GamepadImage : KeyboardImage;
        }
    }
}
