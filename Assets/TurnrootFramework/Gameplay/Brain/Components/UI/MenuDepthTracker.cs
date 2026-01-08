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
                return null;

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

        public bool HasActiveMenu()
        {
            return CurrentMenu?.activeInstance != null;
        }

        public void TrackTransition(MenuLocation from, MenuLocation to)
        {
            // Remove the 'from' menu if it's on the stack
            if (_menuStack.Count > 0 && _menuStack.Peek() == from)
            {
                _menuStack.Pop();
            }

            // Add the 'to' menu to the stack
            if (to != null)
            {
                _menuStack.Push(to);
            }

#if UNITY_EDITOR
            Debug.Log($"MenuDepthTracker: Tracked transition. New depth: {CurrentDepth}");
#endif
        }
    }
}
