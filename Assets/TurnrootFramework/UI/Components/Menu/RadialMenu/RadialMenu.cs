using System;
using System.Collections;
using System.Collections.Generic;
using Turnroot.Gameplay.Brain.UI;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using TurnrootFramework.Gameplay.Brain.Segments;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.RadialMenu
{
    [RequireComponent(typeof(CanvasGroup))]
    public class RadialMenu : MonoBehaviour
    {
        [HideInInspector]
        public List<string> segmentNames = new();
        private GamewideUiSettings _uiSettings;

        private static readonly Vector2 DirectionRight = Vector2.right;
        private static readonly Vector2 DirectionLeft = Vector2.left;
        private static readonly Vector2 DirectionUp = Vector2.up;
        private static readonly Vector2 DirectionDown = Vector2.down;

        private Vector2 GetCardinalDirection(Vector2 input)
        {
            return input.magnitude <= joystickDeadzone ? Vector2.zero
                : Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                    ? (input.x > 0 ? DirectionRight : DirectionLeft)
                : (input.y > 0 ? DirectionUp : DirectionDown);
        }

        private enum NavigationState
        {
            Idle,
            FirstPress,
            Holding,
            Repeating,
        }

        private NavigationState _navState = NavigationState.Idle;

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
        private float _navHoldTime = 0f;
        private float _navRepeatTimer = 0f;

        private bool _justNavigated = false;
        private float _justNavTimer = 0f;

        [SerializeField]
        private float reversalWindow = 1.2f;

        [SerializeField]
        private float inputRepeatDelay = 0.2f;

        [Header("Input Actions")]
        [SerializeField]
        public InputAction navigateAction;

        [SerializeField]
        public InputAction selectAction;

        private int _selectedIndex = 0;
        private bool _centerSelected = false;
        private float _rotStep;
        private Vector2 _lastNavigateInput;

        public PrebattleOptions FindPreBattleOptionByName(string name)
        {
            switch (name)
            {
                case "Team":
                    return PrebattleOptions.Team;
                case "Items":
                    return PrebattleOptions.Items;
                case "Settings":
                    return PrebattleOptions.Settings;
                case "Skills":
                    return PrebattleOptions.Skills;
                case "Withdraw":
                    return PrebattleOptions.Withdraw;
                case "Map":
                    return PrebattleOptions.Map;
                case "Support":
                    return PrebattleOptions.Support;
                default:
                    throw new Exception($"No PrebattleOption found for segment name: {name}");
            }
        }

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
            _uiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
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
                navigationRepeatDelay = 0.08f;
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

        private void OnEnable()
        {
            navigateAction?.Enable();
            selectAction?.Enable();
        }

        private void OnDisable()
        {
            navigateAction?.Disable();
            selectAction?.Disable();
        }

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
            if (selectAction != null)
            {
                selectAction.performed -= OnSelectPerformed;
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

            if (selectAction != null)
            {
                selectAction.performed += OnSelectPerformed;
            }
        }

        private void OnSelectPerformed(InputAction.CallbackContext context) => ConfirmSelection();

        private void Update()
        {
            HandleNavigationInput();
            UpdateReversalWindow();
        }

        private void HandleNavigationInput()
        {
            if (navigateAction == null || (menuItems.Count == 0 && centerItem == null))
            {
                return;
            }

            Vector2 input = navigateAction.ReadValue<Vector2>();
            bool hasInput = input.magnitude > joystickDeadzone;
            Vector2 direction = GetCardinalDirection(input);

            // If the user reverses direction within the reversal window after a navigation,
            // treat it as a request to select the center item (if present). Require an exact reversal
            // (opposite cardinal direction) to avoid accidental center selection on repeated input.
            if (
                hasInput
                && _justNavigated
                && centerItem != null
                && direction == -_lastNavigateInput
            )
            {
                SelectItemByIndex(0, true);
                TransitionTo(NavigationState.Idle, Vector2.zero);
                return;
            }

            switch (_navState)
            {
                case NavigationState.Idle:
                    if (hasInput)
                    {
                        TransitionTo(NavigationState.FirstPress, input);
                    }
                    break;

                case NavigationState.FirstPress:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else if (direction != _lastNavigateInput)
                    {
                        // Direction changed - check for reversal (require exact opposite)
                        if (
                            _justNavigated
                            && centerItem != null
                            && direction == -_lastNavigateInput
                        )
                        {
                            // Exact opposite direction during reversal window = select center
                            SelectItemByIndex(0, true);
                            TransitionTo(NavigationState.Idle, Vector2.zero);
                        }
                        else
                        {
                            // Different direction, not an exact reversal - navigate again
                            NavigateInDirection(direction);
                            _lastNavigateInput = direction;
                            _justNavigated = true;
                            _justNavTimer = 0f;
                            // Stay in FirstPress to allow chaining
                        }
                    }
                    else
                    {
                        // Same direction still held - transition to holding
                        TransitionTo(NavigationState.Holding, input);
                    }
                    break;

                case NavigationState.Holding:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else
                    {
                        _navHoldTime += Time.deltaTime;
                        if (_navHoldTime >= navigationInitialDelay)
                        {
                            TransitionTo(NavigationState.Repeating, input);
                        }
                    }
                    break;

                case NavigationState.Repeating:
                    if (!hasInput)
                    {
                        TransitionTo(NavigationState.Idle, Vector2.zero);
                    }
                    else
                    {
                        _navRepeatTimer += Time.deltaTime;
                        if (_navRepeatTimer >= navigationRepeatDelay)
                        {
                            NavigateSegments(_lastNavigateInput);
                            _navRepeatTimer = 0f;
                        }
                    }
                    break;
            }
        }

        private void UpdateReversalWindow()
        {
            if (_justNavigated)
            {
                _justNavTimer += Time.deltaTime;
                if (_justNavTimer > reversalWindow)
                {
                    _justNavigated = false;
                }
            }
        }

        private void TransitionTo(NavigationState newState, Vector2 input)
        {
            var oldState = _navState;
            _navState = newState;

            switch (newState)
            {
                case NavigationState.Idle:
                    // Do not clear _lastNavigateInput here — keep last direction available for
                    // reversal detection across a brief release between inputs.
                    _navHoldTime = 0f;
                    _navRepeatTimer = 0f;
                    break;

                case NavigationState.FirstPress:
                    Vector2 dir = GetCardinalDirection(input);
                    NavigateInDirection(dir);
                    _lastNavigateInput = dir;
                    _justNavigated = true;
                    _justNavTimer = 0f;
                    break;

                case NavigationState.Holding:
                    _navHoldTime = 0f;
                    break;

                case NavigationState.Repeating:
                    _navRepeatTimer = 0f;
                    break;
            }
        }

        // Add this helper method to RadialMenu.cs to find the visual index closest to a target angle
        private int GetItemIndexAtAngle(float targetAngle)
        {
            if (menuItems.Count == 0)
            {
                return -1;
            }

            // Normalize target angle to 0-360
            targetAngle = (targetAngle + 360f) % 360f;

            int closestIndex = 0;
            float closestDiff = float.MaxValue;

            for (int i = 0; i < menuItems.Count; i++)
            {
                float itemAngle = (i * _rotStep + _rotStep * 0.5f) % 360f;
                float diff = Mathf.Abs(Mathf.DeltaAngle(targetAngle, itemAngle));

                if (diff < closestDiff)
                {
                    closestDiff = diff;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private void NavigateInDirection(Vector2 direction)
        {
            if (centerItem == null)
            {
                NavigateSegments(direction);
                return;
            }

            if (_centerSelected)
            {
                // From center, navigate to the item in the direction pressed
                // Convert direction to angle (0° = up/top, 90° = right, etc.)
                float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                if (targetAngle < 0)
                {
                    targetAngle += 360f;
                }

                int targetIndex = GetItemIndexAtAngle(targetAngle);
                SelectItemByIndex(targetIndex, false);
            }
            else
            {
                NavigateSegments(direction);
            }
        }

        private void NavigateSegments(Vector2 direction)
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            int delta = 0;

            if (direction == Vector2.right || direction == Vector2.down)
            {
                delta = 1;
            }
            else if (direction == Vector2.left || direction == Vector2.up)
            {
                delta = -1;
            }

            if (delta != 0)
            {
                int newIndex = (_selectedIndex + delta + menuItems.Count) % menuItems.Count;
                SelectItemByIndex(newIndex, false);
            }
        }

        private void ArrangeItemsInCircle()
        {
            if (menuItems.Count == 0)
            {
                return;
            }

            _rotStep = 360f / menuItems.Count;

            float segmentSize = menuRadiusPixels * 2f;

            for (int i = 0; i < menuItems.Count; i++)
            {
                RectTransform itemRect = menuItems[i].GetComponent<RectTransform>();
                itemRect.localRotation = Quaternion.identity;
                itemRect.anchorMin = new Vector2(0.5f, 0.5f);
                itemRect.anchorMax = new Vector2(0.5f, 0.5f);
                itemRect.pivot = new Vector2(0.5f, 0.5f);

                itemRect.sizeDelta = new Vector2(segmentSize, segmentSize);

                // Start at top (0°) and go clockwise. Subtract from 360 to reverse the order
                // so menu items match visual layout (index 0 at top, increasing clockwise)
                float startAngle = (360f - (i * _rotStep)) % 360f;
                float endAngle = (360f - ((i + 1) * _rotStep)) % 360f;

                // Swap start/end since we reversed the direction
                float temp = startAngle;
                startAngle = endAngle;
                endAngle = temp;

                menuItems[i].SetSegmentAngles(startAngle, endAngle, innerRadiusPercent, segmentGap);

                float centerAngle = (startAngle + endAngle) * 0.5f;
                menuItems[i]
                    .PositionContent(
                        centerAngle,
                        innerRadiusPercent,
                        1f,
                        menuRadiusPixels,
                        contentRadialOffset
                    );
            }
        }

        private void SetupCenterItem()
        {
            // Do not resize or reposition a minimal sprite-only center item
            if (centerItem is null or RadialMenuItemSprite)
            {
                return;
            }

            RectTransform centerRect = centerItem.GetComponent<RectTransform>();
            centerRect.localRotation = Quaternion.identity;
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);

            float segmentSize = menuRadiusPixels * 2f;
            centerRect.sizeDelta = new Vector2(segmentSize, segmentSize);

            centerItem.SetSegmentAngles(0, 360, innerRadiusPercent, 0);
            centerItem.PositionContent(0f, 0f, 1f, menuRadiusPixels);
        }

        private void HandleItemHover(int index, bool isCenter) =>
            SelectItemByIndex(index, isCenter);

        private void SelectItemByIndex(int index, bool isCenter)
        {
            if (_centerSelected && centerItem != null)
            {
                centerItem.Deselect();
            }
            else if (!_centerSelected && _selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                menuItems[_selectedIndex].Deselect();
            }

            _centerSelected = isCenter;
            if (isCenter && centerItem != null)
            {
                centerItem.Select();
                OnNavigate?.Invoke(centerItem);
            }
            else if (!isCenter && index >= 0 && index < menuItems.Count)
            {
                _selectedIndex = index;
                menuItems[_selectedIndex].Select();
                OnNavigate?.Invoke(menuItems[_selectedIndex]);
            }
        }

        private void ConfirmSelection()
        {
            if (_centerSelected && centerItem != null)
            {
                OnItemSelected?.Invoke(centerItem);
            }
            else if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                OnItemSelected?.Invoke(menuItems[_selectedIndex]);
            }
        }

        public void SetSelectedIndex(int index, bool selectCenter = false) =>
            SelectItemByIndex(index, selectCenter);

        public int GetSelectedIndex() => _selectedIndex;

        public bool IsCenterSelected() => _centerSelected;

        public MenuItemBase GetSelectedItem()
        {
            if (_centerSelected)
            {
                return centerItem;
            }
            else if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                return menuItems[_selectedIndex];
            }
            return null;
        }

        public void RefreshLayout()
        {
            ArrangeItemsInCircle();
            if (centerItem != null)
            {
                SetupCenterItem();
            }

            for (int i = 0; i < menuItems.Count; i++)
            {
                menuItems[i].EnsureContentOnTop();
            }
            centerItem?.EnsureContentOnTop();
        }

        public void SetContentRadialOffset(float offset)
        {
            contentRadialOffset = Mathf.Clamp(offset, -0.5f, 0.5f);
            RefreshLayout();
        }

        public void SetMenuRadius(float radiusPixels)
        {
            menuRadiusPixels = radiusPixels;
            RefreshLayout();
        }

        private void ShowMenu()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            // If we're not configured to hide the menu until ready, just notify immediately.
            if (!hideUntilReady)
            {
                NotifyMenuReady();
                return;
            }

            // Stop any existing reveal coroutine
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            if (showFadeTime <= 0f)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                NotifyMenuReady();
            }
            else
            {
                _showCoroutine = StartCoroutine(FadeIn(_canvasGroup, showFadeTime));
            }
        }

        private IEnumerator FadeIn(CanvasGroup cg, float duration)
        {
            float t = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;
            while (t < duration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / duration);
                yield return null;
            }
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
            _showCoroutine = null;
            NotifyMenuReady();
        }

        private void NotifyMenuReady() => OnMenuReady?.Invoke(this);
    }
}
