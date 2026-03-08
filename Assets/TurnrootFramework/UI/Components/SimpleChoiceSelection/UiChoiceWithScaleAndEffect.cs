using Coffee.UIEffects;
using UnityEngine;

namespace Turnroot.UI
{
    /// <summary>
    /// A simple, reusable UI component for selectable choices in menus.
    /// Handles visual feedback for selection state by scaling and applying effects.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UiChoiceWithScaleAndEffect : MonoBehaviour
    {
        public RectTransform ToScale => GetComponent<RectTransform>();
        public UIEffect Effect;

        public bool IsActive { get; private set; } = false;

        public void Select()
        {
            IsActive = true;
            ToScale.localScale = Vector3.one * 1.1f;
            Effect.enabled = true;
        }

        public void Deselect()
        {
            IsActive = false;
            ToScale.localScale = Vector3.one;
            Effect.enabled = false;
        }
    }
}
