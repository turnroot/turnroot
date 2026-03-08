using Coffee.UIEffects;
using UnityEngine;

namespace Turnroot.UI
{
    public class UiChoiceWithEffect : MonoBehaviour
    {
        public UIEffect Effect;

        public bool IsActive { get; private set; } = false;

        public void Select()
        {
            IsActive = true;
            Effect.enabled = true;
        }

        public void Deselect()
        {
            IsActive = false;
            Effect.enabled = false;
        }
    }
}
