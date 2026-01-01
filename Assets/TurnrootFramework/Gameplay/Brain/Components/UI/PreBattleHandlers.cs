using Turnroot.Gameplay.Brain;
using Turnroot.UI.Components.RadialMenu;
using UnityEngine;
#if COFFEE_UIEFFECTS
using Coffee.UIEffects;
#endif

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public partial class UiBrain : BrainComponent
    {
        private void HandlePreBattleMenuNavigate(RadialMenuItemBase item)
        {
            // Handle navigation to item
#if UNITY_EDITOR
            Debug.Log($"UiBrain: Navigated to item: {item.name}");
#endif
        }

        private void HandlePreBattleMenuSelect(RadialMenuItemBase item)
        {
            // Handle selection of item
            if (item.name == "Center")
            {
                _brain.PublishPreBattleCompleted();
                var uiFade = PreBattleMenuInstance.GetComponent<UIFade>();
                uiFade.Hide();
#if COFFEE_UIEFFECTS

                PreBattleMenuInstance
                    .GetComponent<RadialMenu>()
                    .centerItem.GetComponent<UIEffectTweener>()
                    .Play();

#endif
#if UNITY_EDITOR
                Debug.Log($"UiBrain: Pre-battle completed, transitioning to Battle");
#endif
            }
        }
    }
}
