using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnroot.UI.Components.RadialMenu
{
    public class RadialMenu : MonoBehaviour
    {
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
        public List<RadialMenuItemBase> menuItems = new List<RadialMenuItemBase>();

        [Header("Layout Settings")]
        [SerializeField]
        private RadialMenuItemBase centerItem;

        [SerializeField]
        private float innerRadiusPercent = 0.35f;

        [SerializeField]
        private float segmentGap = 0.01f;

        [SerializeField]
        private float menuRadiusPixels = 800f;

        [Header("Content Settings")]
        [Range(-0.5f, 0.5f)]
        [SerializeField]
        private float contentRadialOffset = 0f;

        [Header("Input Settings")]
        [SerializeField]
        private float joystickDeadzone = 0.3f;

        [Header("Startup Visibility")]
        [SerializeField]
        [Tooltip("If true, the menu will be hidden until initialization/layout completes.")]
        private bool hideUntilReady = true;

        [SerializeField]
        [Tooltip(
            "Fade time (sec) to reveal the menu after initialization. Set to 0 for instant show."
        )]
        private float showFadeTime = 0.12f;

        // CanvasGroup used to hide/show the whole menu during startup
        private CanvasGroup _canvasGroup;
        private Coroutine _showCoroutine;

        [Header("Navigation Repeat")]
        [SerializeField]
        private float navigationInitialDelay = 0.4f;

        [SerializeField]
        private float navigationRepeatDelay = 0.08f;

        private int _navDir = 0;
        private int _lastNavDir = 0;
        private float _navHoldTime = 0f;
        private float _navRepeatTimer = 0f;

        private bool _justNavigated = false;
        private float _justNavTimer = 0f;

        [SerializeField]
        private float reversalWindow = 0.6f;

        [SerializeField]
        private float inputRepeatDelay = 0.2f;

        [Header("Input Actions")]
        [SerializeField]
        private InputAction navigateAction;

        [SerializeField]
        private InputAction selectAction;

        private int _selectedIndex = 0;
        private bool _centerSelected = false;
        private float _rotStep;
        private Vector2 _lastNavigateInput;
        private float _lastInputTime;

        public event Action<RadialMenuItemBase> OnItemSelected;

        /// <summary>
        /// Fired when the radial menu has completed initialization and is visible/ready.
        /// Passes the menu instance so managers can subscribe and act on it.
        /// </summary>
        public event Action<RadialMenu> OnMenuReady;

        private void Awake()
        {
            // Ensure a CanvasGroup exists so we can hide the menu until ready.
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

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
                RadialMenuItemBase[] allItems = GetComponentsInChildren<RadialMenuItemBase>();
                foreach (var item in allItems)
                {
                    if (item != centerItem && item.transform.parent == transform)
                    {
                        menuItems.Add(item);
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
                return;

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

        private void NavigateInDirection(Vector2 direction)
        {
            if (centerItem == null)
            {
                NavigateSegments(direction);
                return;
            }

            if (_centerSelected)
            {
                if (direction == Vector2.up)
                {
                    SelectItemByIndex(0, false);
                }
                else if (direction == Vector2.down)
                {
                    SelectItemByIndex(menuItems.Count / 2, false);
                }
                else if (direction == Vector2.right)
                {
                    SelectItemByIndex(menuItems.Count / 4, false);
                }
                else if (direction == Vector2.left)
                {
                    SelectItemByIndex((menuItems.Count * 3) / 4, false);
                }
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

                float startAngle = i * _rotStep;
                float endAngle = (i + 1) * _rotStep;

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
            if (centerItem == null || centerItem is RadialMenuItemSprite)
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
            }
            else if (!isCenter && index >= 0 && index < menuItems.Count)
            {
                _selectedIndex = index;
                menuItems[_selectedIndex].Select();
            }
        }

        private void ConfirmSelection()
        {
            if (_centerSelected && centerItem != null)
            {
                centerItem.Activate();
                OnItemSelected?.Invoke(centerItem);
            }
            else if (_selectedIndex >= 0 && _selectedIndex < menuItems.Count)
            {
                menuItems[_selectedIndex].Activate();
                OnItemSelected?.Invoke(menuItems[_selectedIndex]);
            }
        }

        public void SetSelectedIndex(int index, bool selectCenter = false) =>
            SelectItemByIndex(index, selectCenter);

        public int GetSelectedIndex() => _selectedIndex;

        public bool IsCenterSelected() => _centerSelected;

        public RadialMenuItemBase GetSelectedItem()
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
                return;

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

        private void NotifyMenuReady()
        {
            OnMenuReady?.Invoke(this);
        }
    }
}
