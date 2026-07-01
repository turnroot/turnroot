using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Utilities.UI
{
    public struct LabelAppearance
    {
        public Color TextColor;
        public TMP_FontAsset FontAsset;
        public Color ImageColor;
        public Sprite ImageSprite;
        public int Chapter;
        public UIEffect UIEffect;
    }

    public class ChangeLabelAppearanceByChapter : MonoBehaviour
    {
        public LabelAppearance[] LabelAppearances;
        public Image image;
        public TextMeshProUGUI text;

        public void UpdateLabelAppearance(int chapter)
        {
            foreach (var appearance in LabelAppearances)
            {
                appearance.UIEffect.enabled = false; // Disable all effects first
                if (appearance.Chapter == chapter)
                {
                    text.color = appearance.TextColor;
                    text.font = appearance.FontAsset;
                    image.color = appearance.ImageColor;
                    image.sprite = appearance.ImageSprite;
                    appearance.UIEffect.enabled = true;
                    return;
                }
            }
            $"ChangeLabelAppearanceByChapter: No label appearance found for chapter {chapter}".LogWarning();
        }

        private void OnEnable()
        {
            var brain = GetAndCacheBrain.GetBrain();
            brain.OnLongTermMemoryInitialized += () =>
            {
                UpdateLabelAppearance(brain.saveFileBrain.ActiveSaveFile.ChapterNumber);
            };
        }

        private void OnDisable()
        {
            var brain = GetAndCacheBrain.GetBrain();
            if (brain != null)
            {
                brain.OnLongTermMemoryInitialized -= () =>
                {
                    UpdateLabelAppearance(brain.saveFileBrain.ActiveSaveFile.ChapterNumber);
                };
            }
        }
    }
}
