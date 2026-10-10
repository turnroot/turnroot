using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Segments;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// A circular radial menu driven by the shared UI input actions (Navigate / Select).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public partial class RadialMenu : MonoBehaviour
    {
        [Header("Menu Items")]
        public List<MenuItemBase> menuItems = new();

        [Header("Layout Settings")]
        [SerializeField]
        public MenuItemBase centerItem;

        [SerializeField]
        private float innerRadiusPercent;

        [SerializeField]
        private float segmentGap;

        [SerializeField]
        private float menuRadiusPixels;

        [Range(-0.5f, 0.5f)]
        [SerializeField]
        private float contentRadialOffset = 0f;

        [Header("Input Settings")]
        [SerializeField]
        private float joystickDeadzone;

        [SerializeField]
        private float navigationInitialDelay;

        [SerializeField]
        private float navigationRepeatDelay;

        [Header("Startup Visibility")]
        [SerializeField]
        private bool hideUntilReady = true;

        [SerializeField]
        private float showFadeTime;

        private CanvasGroup _canvasGroup;
        private Coroutine _showCoroutine;
        private InputAction _subscribedSelect;
        private bool _initialized;

        private int _selectedIndex = 0;
        private bool _centerSelected = false;
        private float _rotStep;

        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        private void Awake()
        {
            var settings = GamewideUiSettings.Instance;
            if (settings != null)
            {
                innerRadiusPercent = settings.RadialMenuInnerRadius;
                segmentGap = settings.RadialMenuSegmentGap;
                showFadeTime = settings.MenuFadeTime;
                joystickDeadzone = settings.RadialMenuJoystickDeadzone;
                navigationInitialDelay = settings.RadialMenuNavigationInitialDelay;
                navigationRepeatDelay = settings.RadialMenuNavigationRepeatDelay;
                menuRadiusPixels = settings.RadialMenuDefaultRadiusPixels;
            }
            else
            {
                innerRadiusPercent = 0.3f;
                segmentGap = 0.02f;
                showFadeTime = 0.75f;
                joystickDeadzone = 0.3f;
                navigationInitialDelay = 0.4f;
                navigationRepeatDelay = 0.2f;
                menuRadiusPixels = 800f;
            }

            _canvasGroup = GetComponent<CanvasGroup>();
            if (hideUntilReady)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            SubscribeSelect();
            if (!_initialized)
            {
                Canvas.willRenderCanvases += OnFirstRender;
            }
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= OnFirstRender;
            UnsubscribeSelect();
        }

        private void SubscribeSelect()
        {
            var select = UIInputActionDefaults.Select;
            if (select == null)
            {
                "UIInputActionDefaults.Select is null; cannot confirm selections.".LogWarning();
                return;
            }

            if (_subscribedSelect == select)
            {
                return;
            }

            UnsubscribeSelect();
            select.performed += OnSelectPerformed;
            _subscribedSelect = select;
        }

        private void UnsubscribeSelect()
        {
            if (_subscribedSelect != null)
            {
                _subscribedSelect.performed -= OnSelectPerformed;
                _subscribedSelect = null;
            }
        }

        private void OnFirstRender()
        {
            Canvas.willRenderCanvases -= OnFirstRender;
            InitializeMenu();
            RefreshLayout();
            ShowMenu();
        }

        private void InitializeMenu()
        {
            _initialized = true;

            if (menuItems.Count == 0)
            {
                foreach (var item in GetComponentsInChildren<MenuItemBase>())
                {
                    if (item != centerItem && item.transform.parent == transform)
                    {
                        menuItems.Add(item);
                    }
                }
            }

            if (centerItem != null)
            {
                centerItem.SetIsCenter(true);
            }

            ArrangeItemsInCircle();
            if (centerItem != null)
            {
                SetupCenterItem();
            }

            if (menuItems.Count > 0)
            {
                SelectItemByIndex(0, false);
            }
        }

        private void OnSelectPerformed(InputAction.CallbackContext context)
        {
            if (!isActiveAndEnabled || !_initialized)
            {
                return;
            }

            ConfirmSelection();
        }

        private void Update() => HandleNavigationInput();

        private void OnDestroy() => UnsubscribeSelect();
    }
}
