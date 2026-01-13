using System.Collections.Generic;
using Turnroot.GameSettings;
using UnityEngine;

namespace TurnrootFramework.Gameplay.Brain.Segments
{
    public class MenuDepthTracker
    {
        private Stack<MenuLocation> _menuStack = new();

        // Event raised whenever depth changes (push/pop/clear/transition)
        public event System.Action OnDepthChanged;

        public int CurrentDepth => _menuStack.Count;
        public MenuLocation CurrentMenu => _menuStack.Count > 0 ? _menuStack.Peek() : null;
        public bool IsInSubMenu => CurrentDepth > 0;

        public void Clear()
        {
            _menuStack.Clear();
            OnDepthChanged?.Invoke();
        }

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
                OnDepthChanged?.Invoke();
                return;
            }

            // If the current top is 'from', this is a forward navigation: just push 'to'
            if (_menuStack.Peek() == from)
            {
                _menuStack.Push(to);
                OnDepthChanged?.Invoke();
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
                OnDepthChanged?.Invoke();
                return;
            }

            // Otherwise, this is an unexpected transition - push both from and to to preserve path
            if (from != null)
            {
                _menuStack.Push(from);
            }

            _menuStack.Push(to);
            OnDepthChanged?.Invoke();
        }

        public bool CanGoBack() => _menuStack.Count > 1;

        public (MenuLocation fromLocation, MenuLocation toLocation) PopTransition()
        {
            if (!CanGoBack())
            {
                return (null, null);
            }

            var currentMenu = _menuStack.Pop();
            var previousMenu = _menuStack.Count > 0 ? _menuStack.Peek() : null;

            return (currentMenu, previousMenu);
        }
    }
}
