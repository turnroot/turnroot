using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.UI.Components.Menu.Submenu
{
    public class MenuCarousel : MonoBehaviour
    {
        public List<string> Options = new();
        public int CurrentIndex = 0;
        public Dictionary<string, System.Enum> OptionStringToEnumValue = new();

        public Image leftArrowImage;
        public Image rightArrowImage;
        public TMPro.TextMeshProUGUI optionText;

        public event System.Action<int> onValueChanged;

        private void Start() => UpdateDisplay();

        public void InitializeCarousel<T>(T currentValue = default)
            where T : System.Enum
        {
            Options.Clear();
            OptionStringToEnumValue.Clear();

            var enumValues = System.Enum.GetValues(typeof(T));
            foreach (T value in enumValues)
            {
                Options.Add(value.ToString());
                OptionStringToEnumValue[value.ToString()] = value;
            }

            if (currentValue != null)
            {
                CurrentIndex = System.Array.IndexOf(enumValues, currentValue);
            }
            else
            {
                CurrentIndex = 0;
            }

            UpdateDisplay();
        }

        public void IncrementIndex()
        {
            int oldIndex = CurrentIndex;
            CurrentIndex = (CurrentIndex + 1) % Options.Count;
            UpdateDisplay();
            if (oldIndex != CurrentIndex)
            {
                onValueChanged?.Invoke(CurrentIndex);
            }
        }

        public void DecrementIndex()
        {
            int oldIndex = CurrentIndex;
            CurrentIndex = (CurrentIndex - 1 + Options.Count) % Options.Count;
            UpdateDisplay();
            if (oldIndex != CurrentIndex)
            {
                onValueChanged?.Invoke(CurrentIndex);
            }
        }

        public void GetCurrentEnumValue<T>(out T enumValue)
            where T : System.Enum => enumValue = (T)OptionStringToEnumValue[Options[CurrentIndex]];

        public void UpdateDisplay()
        {
            if (Options.Count == 0)
            {
                optionText.text = "No Options";
                return;
            }

            optionText.text = SplitByUppercase(Options[CurrentIndex].ToString());
        }

        public string SplitByUppercase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var result = new System.Text.StringBuilder();
            result.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }

            return result.ToString();
        }
    }
}
