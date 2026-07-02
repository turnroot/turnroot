using Coffee.UIEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Turnroot.Utilities.UI
{
    [System.Serializable]
    public struct LabelAppearance
    {
        public Color TextColor;
        public TMP_FontAsset FontAsset;
        public Color ImageColor;
        public Sprite ImageSprite;
        public int Chapter;

        [GradientUsage(true)]
        public Gradient gradient;
    }

    public class ChangeLabelAppearanceByChapter : MonoBehaviour
    {
        public LabelAppearance[] LabelAppearances;
        public Image image;
        public TextMeshProUGUI text;

        public UIEffect UIEffect;

        public void UpdateLabelAppearance(int chapter)
        {
            $"ChangeLabelAppearanceByChapter: Updating label appearance for chapter {chapter}".LogInfo();
            foreach (var appearance in LabelAppearances)
            {
                if (appearance.Chapter <= chapter)
                {
                    text.color = appearance.TextColor;
                    text.font = appearance.FontAsset;
                    image.color = appearance.ImageColor;
                    image.sprite = appearance.ImageSprite;
                    UIEffect.SetTransitionGradientKeys(appearance.gradient);
                    return;
                }
            }
            $"ChangeLabelAppearanceByChapter: No label appearance found for chapter {chapter}".LogWarning();
        }

        private void OnEnable()
        {
            $"ChangeLabelAppearanceByChapter: OnEnable called, subscribing to Brain.OnLongTermMemoryInitialized".LogInfo();
            var brain = GetAndCacheBrain.GetBrain();
            if (brain == null)
            {
                $"ChangeLabelAppearanceByChapter: Brain instance not found, cannot subscribe to OnLongTermMemoryInitialized".LogWarning();
                return;
            }
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
