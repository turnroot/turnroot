using System.Collections.Generic;
using Turnroot.GameSettings;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class MenuDepthTracker
    {
        private Stack<MenuLocation> _menuStack = new();

        public int CurrentDepth => _menuStack.Count;
        public MenuLocation CurrentMenu => _menuStack.Count > 0 ? _menuStack.Peek() : null;
        public bool IsInSubMenu => CurrentDepth > 0;

        public void Push(MenuLocation menu)
        {
            if (menu != null)
            {
                _menuStack.Push(menu);
#if UNITY_EDITOR
                Debug.Log($"MenuDepthTracker: Pushed menu. New depth: {CurrentDepth}");
#endif
            }
        }

        public MenuLocation Pop()
        {
            if (_menuStack.Count > 0)
            {
                var popped = _menuStack.Pop();
#if UNITY_EDITOR
                Debug.Log($"MenuDepthTracker: Popped menu. New depth: {CurrentDepth}");
#endif
                return popped;
            }
            return null;
        }

        public void Clear()
        {
            _menuStack.Clear();
#if UNITY_EDITOR
            Debug.Log("MenuDepthTracker: Cleared all menus");
#endif
        }

        public MenuLocation GetParent()
        {
            if (_menuStack.Count < 2)
            {
                return null;
            }

            // Temporarily pop current to peek at parent
            var current = _menuStack.Pop();
            var parent = _menuStack.Peek();
            _menuStack.Push(current);
            return parent;
        }

        public void Set(int depth)
        {
            // For backwards compatibility with existing code that sets depth directly
            while (_menuStack.Count > depth && _menuStack.Count > 0)
            {
                _menuStack.Pop();
            }
#if UNITY_EDITOR
            Debug.Log($"MenuDepthTracker: Set depth to {depth}. Current depth: {CurrentDepth}");
#endif
        }

        public MenuLocation FindActiveSubmenu(params MenuLocation[] possibleSubmenus)
        {
            foreach (var submenu in possibleSubmenus)
            {
                if (submenu?.activeInstance != null)
                {
                    return submenu;
                }
            }
            return null;
        }

        public bool HasActiveMenu() => CurrentMenu?.activeInstance != null;

        public void TrackTransition(MenuLocation from, MenuLocation to)
        {
            if (to == null)
            {
                return;
            }

            // If stack is empty, initialize with 'from' (if present), then push 'to'
            if (_menuStack.Count == 0)
            {
                if (from != null)
                {
                    _menuStack.Push(from);
                }

                _menuStack.Push(to);
#if UNITY_EDITOR
                Debug.Log($"MenuDepthTracker: Tracked transition. New depth: {CurrentDepth}");
#endif
                return;
            }

            // If the current top is 'from', this is a forward navigation: just push 'to'
            if (_menuStack.Peek() == from)
            {
                _menuStack.Push(to);
#if UNITY_EDITOR
                Debug.Log(
                    $"MenuDepthTracker: Tracked forward transition. New depth: {CurrentDepth}"
                );
#endif
                return;
            }

            // If 'from' exists somewhere in the stack, pop back to it then push 'to'
            if (_menuStack.Contains(from))
            {
                while (_menuStack.Count > 0 && _menuStack.Peek() != from)
                {
                    _menuStack.Pop();
                }

                _menuStack.Push(to);
#if UNITY_EDITOR
                Debug.Log(
                    $"MenuDepthTracker: Tracked rewind transition. New depth: {CurrentDepth}"
                );
#endif
                return;
            }

            // Otherwise, this is an unexpected transition - push both from and to to preserve path
            if (from != null)
            {
                _menuStack.Push(from);
            }

            _menuStack.Push(to);
#if UNITY_EDITOR
            Debug.Log($"MenuDepthTracker: Tracked transition. New depth: {CurrentDepth}");
#endif
        }

        public bool CanGoBack()
        {
            bool canGoBack = _menuStack.Count > 1;
#if UNITY_EDITOR
            Debug.Log($"MenuDepthTracker: CanGoBack = {canGoBack}");
#endif
            return canGoBack;
        }

        public (MenuLocation fromLocation, MenuLocation toLocation) PopTransition()
        {
            if (!CanGoBack())
            {
#if UNITY_EDITOR
                Debug.Log($"MenuDepthTracker: Cannot go back. Stack count: {_menuStack.Count}");
#endif
                return (null, null);
            }

            var currentMenu = _menuStack.Pop();
            var previousMenu = _menuStack.Count > 0 ? _menuStack.Peek() : null;

#if UNITY_EDITOR
            Debug.Log($"MenuDepthTracker: PopTransition. New depth: {CurrentDepth}");
#endif

            return (currentMenu, previousMenu);
        }
    }
}
