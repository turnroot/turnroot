using System;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.Segments;
using Turnroot.GameSettings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.RadialMenu
{
    /// <summary>
    /// A circular radial menu with joystick/keyboard navigation and customizable layout settings.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public partial class RadialMenu : MonoBehaviour
    {
        [HideInInspector]
        public List<string> segmentNames = new();
        private GamewideUiSettings _uiSettings;

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

        [Header("Content Settings")]
        [Range(-0.5f, 0.5f)]
        [SerializeField]
        private float contentRadialOffset = 0f;

        [Header("Input Settings")]
        [SerializeField]
        private float joystickDeadzone;

        [Header("Startup Visibility")]
        [SerializeField]
        [Tooltip("If true, the menu will be hidden until initialization/layout completes.")]
        private bool hideUntilReady = true;

        [SerializeField]
        [Tooltip(
            "Fade time (sec) to reveal the menu after initialization. Set to 0 for instant show."
        )]
        private float showFadeTime;

        // CanvasGroup used to hide/show the whole menu during startup
        private CanvasGroup _canvasGroup;
        private Coroutine _showCoroutine;

        [Header("Navigation Repeat")]
        [SerializeField]
        private float navigationInitialDelay;

        [SerializeField]
        private float navigationRepeatDelay;

        [SerializeField]
        private float reversalWindow = 1.2f;

        [SerializeField]
        private float inputRepeatDelay = 0.2f;

        private int _selectedIndex = 0;
        private bool _centerSelected = false;
        private float _rotStep;

        public UiBrain uiBrain;

        public event Action<MenuItemBase> OnNavigate;
        public event Action<MenuItemBase> OnItemSelected;

        /// <summary>
        /// Fired when the radial menu has completed initialization and is visible/ready.
        /// Passes the menu instance so managers can subscribe and act on it.
        /// </summary>
        public event Action<RadialMenu> OnMenuReady;

        private void Awake()
        {
            // Load UI settings and apply them
            _uiSettings = GamewideUiSettings.Instance;
            if (_uiSettings != null)
            {
                innerRadiusPercent = _uiSettings.RadialMenuInnerRadius;
                segmentGap = _uiSettings.RadialMenuSegmentGap;
                showFadeTime = _uiSettings.MenuFadeTime;
                joystickDeadzone = _uiSettings.RadialMenuJoystickDeadzone;
                navigationInitialDelay = _uiSettings.RadialMenuNavigationInitialDelay;
                navigationRepeatDelay = _uiSettings.RadialMenuNavigationRepeatDelay;
                menuRadiusPixels = _uiSettings.RadialMenuDefaultRadiusPixels;
            }
            else
            {
                innerRadiusPercent = 0.3f;
                segmentGap = 0.02f;
                showFadeTime = 0.75f;
                joystickDeadzone = 0.3f;
                navigationInitialDelay = 0.4f;
                navigationRepeatDelay = inputRepeatDelay;
                menuRadiusPixels = 800f;
            }

            // Ensure a CanvasGroup exists so we can hide the menu until ready.
            _canvasGroup = GetComponent<CanvasGroup>();

            if (hideUntilReady)
            {
                // Hide immediately so the menu doesn't flash while children are instantiating/layouting.
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }
        }

        private void OnEnable() { }

        private void OnDisable() { }

        private void Start() => Canvas.willRenderCanvases += OnFirstRender;

        private void OnFirstRender()
        {
            // Unsubscribe so this only runs once
            Canvas.willRenderCanvases -= OnFirstRender;

            InitializeMenu();
            RefreshLayout();

            // Reveal the menu now that initialization & layout are complete
            ShowMenu();
        }

        private void OnDestroy()
        {
            Canvas.willRenderCanvases -= OnFirstRender;
            var select = UIInputActionDefaults.Select;
            if (select != null)
            {
                select.performed -= OnSelectPerformed;
            }
        }

        private void InitializeMenu()
        {
            if (menuItems.Count == 0)
            {
                MenuItemBase[] allItems = GetComponentsInChildren<MenuItemBase>();
                foreach (var item in allItems)
                {
                    if (item != centerItem && item.transform.parent == transform)
                    {
                        menuItems.Add(item);
                        segmentNames.Add(item.ItemName);
                    }
                }
            }

            ArrangeItemsInCircle();

            for (int i = 0; i < menuItems.Count; i++)
            {
                int index = i;
                menuItems[i].OnHoverEnter += () => HandleItemHover(index, false);
                menuItems[i].OnClick += () => ConfirmSelection();
            }

            if (centerItem != null)
            {
                centerItem.SetIsCenter(true);
                centerItem.OnHoverEnter += () => HandleItemHover(0, true);
                centerItem.OnClick += () => ConfirmSelection();
                SetupCenterItem();
            }

            var select = UIInputActionDefaults.Select;
            if (select != null)
            {
                select.performed += OnSelectPerformed;
            }

            // Ensure item 0 is visually highlighted from the start — prevents the phantom
            // selection where the first item is logically selected but shows no highlight
            // until the player navigates.
            if (menuItems.Count > 0)
            {
                SelectItemByIndex(0, false);
            }
        }

        private void OnSelectPerformed(InputAction.CallbackContext context) => ConfirmSelection();

        private void Update()
        {
            HandleNavigationInput();
            UpdateReversalWindow();
        }

        // Navigation methods moved to RadialMenuPartials/Navigation.cs
        // Layout methods moved to RadialMenuPartials/Layout.cs
    }
}
