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
        }

        private void HandlePreBattleMenuSelect(RadialMenuItemBase item)
        {
            // Handle selection of item
            if (item.IsCenter)
            {
                if (PreBattleMenuInstance == null)
                {
                    return;
                }

                var menuInstance = PreBattleMenuInstance;

                if (!menuInstance.TryGetComponent<UIFade>(out var uiFade))
                {
                    _brain.PublishPreBattleCompleted();
                    Destroy(menuInstance);
                    PreBattleMenuInstance = null;
                    return;
                }

#if COFFEE_UIEFFECTS
                var radialMenu = menuInstance.GetComponent<RadialMenu>();
                var effectTweener = radialMenu?.centerItem?.GetComponent<UIEffectTweener>();
#endif

                // Subscribe to the fade completion event to destroy menu when done
                uiFade.OnHidden ??= new UnityEngine.Events.UnityEvent();

                uiFade.OnHidden.AddListener(() =>
                {
                    if (menuInstance != null)
                    {
                        // Unsubscribe from events to prevent memory leaks
                        var radialMenu = menuInstance.GetComponent<RadialMenu>();
                        if (radialMenu != null)
                        {
                            radialMenu.OnNavigate -= HandlePreBattleMenuNavigate;
                            radialMenu.OnItemSelected -= HandlePreBattleMenuSelect;
                        }

                        Destroy(menuInstance);
                        PreBattleMenuInstance = null;

                        // Publish completion after cleanup to ensure proper sequencing
                        _brain.PublishPreBattleCompleted();
                    }
                });

                uiFade.Hide();

#if COFFEE_UIEFFECTS
                effectTweener?.Play();
#endif
#if UNITY_EDITOR
                Debug.Log($"UiBrain: Pre-battle completed, transitioning to Battle");
#endif
            }
        }
    }
}
