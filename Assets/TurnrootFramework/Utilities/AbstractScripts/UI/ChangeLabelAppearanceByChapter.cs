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

        private Gameplay.Brain.Brain _brain;

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
            _brain = GetAndCacheBrain.GetBrain();
            if (_brain == null)
            {
                $"ChangeLabelAppearanceByChapter: Brain instance not found, cannot subscribe to OnLongTermMemoryInitialized".LogWarning();
                return;
            }

            if (_brain.ltm != null && _brain.ltm.Initialized)
            {
                TryUpdateFromActiveSaveFile();
            }
            else
            {
                _brain.OnLongTermMemoryInitialized += HandleLongTermMemoryInitialized;
            }
        }

        private void OnDisable()
        {
            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized -= HandleLongTermMemoryInitialized;
                _brain = null;
            }
        }

        private void HandleLongTermMemoryInitialized()
        {
            if (_brain != null)
            {
                _brain.OnLongTermMemoryInitialized -= HandleLongTermMemoryInitialized;
            }

            if (_brain == null)
            {
                $"ChangeLabelAppearanceByChapter: Brain instance is null in HandleLongTermMemoryInitialized".LogWarning();
                return;
            }

            if (_brain.saveFileBrain == null)
            {
                $"ChangeLabelAppearanceByChapter: SaveFileBrain is null, cannot update label appearance".LogWarning();
                return;
            }

            TryUpdateFromActiveSaveFile();
        }

        private void TryUpdateFromActiveSaveFile()
        {
            if (_brain == null || _brain.saveFileBrain == null)
            {
                return;
            }

            var activeSaveFile = _brain.saveFileBrain.ActiveSaveFile;
            UpdateLabelAppearance(activeSaveFile.ChapterNumber);
        }
    }
}
