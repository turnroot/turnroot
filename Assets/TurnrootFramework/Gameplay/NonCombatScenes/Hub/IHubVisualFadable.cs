using System.Collections;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public interface IHubVisualFadable
    {
        GameObject PoiVisual { get; }
        float FadeDuration { get; set; }

        void Show();
        void Hide();
        void FaceCamera();

        void InitializeVisualMaterials();
        float GetAlpha(Material mat);
        IEnumerator FadeRoutine(float from, float to);
        void SetAlpha(float a);
    }
}
