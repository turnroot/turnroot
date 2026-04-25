using System;
using System.Collections;
using NaughtyAttributes;
using TMPro;
using Turnroot.AbstractScripts.Graphics2D;
using Turnroot.Utilities;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;
using UnityEngine.UI;
using Ease = Turnroot.AbstractScripts.Graphics2D.Graphics2DUtils.Ease;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public class ExploreMenuCarousel : MonoBehaviour
    {
        #region Nested Types

        public enum CarouselAxis
        {
            Horizontal,
            Vertical,
        }

        [Serializable]
        public struct ExploreEntry
        {
            [Tooltip("The explore location this slot represents.")]
            public HubExploreLocation Location;

            [Tooltip(
                "The GameObject (world-space or UI RectTransform) that visually represents this entry."
            )]
            public GameObject Visual;

            [Tooltip(
                "Enabled when the location is locked; disabled when it is accessible. "
                    + "Use this to show a lock icon, greyed-out overlay, etc."
            )]
            public GameObject LockedOverlay;

            [Tooltip("Short flavour text shown in the FlavorTextLabel when this entry is focused.")]
            [ResizableTextArea]
            public string FlavorText;

            public string LockedFlavorText;

            public string Name;
        }

        #endregion

        #region Inspector

        [BoxGroup("Setup")]
        [Tooltip("Fade used to show/hide the entire explore menu panel.")]
        public UIFade MenuFade;

        [BoxGroup("Setup")]
        [Tooltip(
            "One entry per explore location: a reference to the HubExploreLocation "
                + "and the corresponding visual GameObject."
        )]
        [ReorderableList]
        public ExploreEntry[] Entries;

        [BoxGroup("Setup")]
        [Tooltip(
            "TextMeshPro label that displays the flavour text of the currently focused entry. "
                + "Updated whenever the active index changes."
        )]
        public TextMeshProUGUI FlavorTextLabel;
        public TextMeshProUGUI LocationNameLabel;

        [BoxGroup("Carousel")]
        [Tooltip("Whether items scroll left-right or up-down.")]
        public CarouselAxis Axis = CarouselAxis.Horizontal;

        [BoxGroup("Carousel")]
        [Tooltip(
            "How many entries are visible on screen at once. "
                + "Odd values centre the selected item cleanly (e.g. 3 = left / selected / right)."
        )]
        [Range(1, 9)]
        public int VisibleCount = 3;

        [BoxGroup("Carousel")]
        [Tooltip(
            "Distance between the centre-points of adjacent items "
                + "(pixels for Canvas / units for world-space)."
        )]
        public float ItemSpacing = 200f;

        [BoxGroup("Carousel")]
        [Tooltip("Duration of the scroll animation in seconds.")]
        public float ScrollDuration = 0.2f;

        [BoxGroup("Carousel")]
        [Tooltip("Easing curve applied to each scroll step.")]
        public Ease ScrollEase = Ease.OutCubic;

        [BoxGroup("Audio")]
        [Tooltip("AudioSource used to play all carousel sounds.")]
        public AudioSource UiFx;

        [BoxGroup("Audio")]
        [Tooltip("Played when scrolling to a different entry.")]
        public AudioClip NavigateClip;

        [BoxGroup("Audio")]
        [Tooltip("Played when confirming entry into an unlocked location.")]
        public AudioClip SelectClip;

        [BoxGroup("Audio")]
        [Tooltip("Played when attempting to enter a locked location.")]
        public AudioClip SelectLockedClip;

        #endregion

        #region Runtime State

        private int _activeIndex;
        private float[] _visualOffsets;
        private HubManager _hubManager;
        private bool _isScrolling;
        private Coroutine _scrollCoroutine;
        private bool _isOpen;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _hubManager = FindFirstObjectByType<HubManager>();
        }

        private void OnEnable()
        {
            if (_hubManager != null)
            {
                _hubManager.OnExploreMenuOpened += OnExploreMenuOpened;
            }
        }

        private void OnDisable()
        {
            if (_hubManager != null)
            {
                _hubManager.OnExploreMenuOpened -= OnExploreMenuOpened;
            }
        }

        #endregion

        #region Open / Close

        private void OnExploreMenuOpened()
        {
            if (Entries == null || Entries.Length == 0)
            {
                return;
            }

            _isOpen = true;
            _activeIndex = 0;
            InitOffsets();
            ApplyOffsets();
            UpdateEntryStates();
            MenuFade?.Show();
        }

        public void Close()
        {
            _isOpen = false;

            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
                _isScrolling = false;
            }

            MenuFade?.Hide();
        }

        #endregion

        #region Input
        public void HandleInput(string action)
        {
            if (!_isOpen)
            {
                return;
            }

            if (action is InputActionConstants.Cancel or "Back")
            {
                Close();
                _hubManager?.BackFromExploreMenu();
                return;
            }

            if (
                action
                is InputActionConstants.Submit
                    or InputActionConstants.Select
                    or InputActionConstants.Confirm
                    or InputActionConstants.Start
            )
            {
                TrySelectCurrent();
                return;
            }

            if (
                action
                is InputActionConstants.NavigateRight
                    or InputActionConstants.ScrollRight
                    or InputActionConstants.NavigateDown
            )
            {
                Scroll(1);
                return;
            }

            if (
                action
                is InputActionConstants.NavigateLeft
                    or InputActionConstants.ScrollLeft
                    or InputActionConstants.NavigateUp
            )
            {
                Scroll(-1);
            }
        }

        #endregion

        #region Selection

        private void TrySelectCurrent()
        {
            if (Entries == null || _activeIndex < 0 || _activeIndex >= Entries.Length)
            {
                return;
            }

            var entry = Entries[_activeIndex];
            if (entry.Location == null)
            {
                return;
            }

            if (entry.Location.IsLocked)
            {
                UiFx?.PlayOneShot(SelectLockedClip);
                return;
            }

            UiFx?.PlayOneShot(SelectClip);
            Close();
            _hubManager?.EnterExploreLocation(entry.Location);
        }

        #endregion

        #region Carousel Scrolling

        private void Scroll(int direction)
        {
            if (Entries == null || Entries.Length <= 1 || _isScrolling)
            {
                return;
            }

            _activeIndex = Converters.PosMod(_activeIndex + direction, Entries.Length);
            UpdateEntryStates();
            UiFx?.PlayOneShot(NavigateClip);

            float scrollDelta = -direction * ItemSpacing;
            float totalSpan = Entries.Length * ItemSpacing;
            float halfSpan = totalSpan * 0.5f;

            // Instant wrap: snap any item that would overshoot the span boundary to the other side.
            // This keeps all items within the [-halfSpan, +halfSpan] window, making the loop seamless.
            for (int i = 0; i < _visualOffsets.Length; i++)
            {
                float projected = _visualOffsets[i] + scrollDelta;
                if (projected > halfSpan)
                {
                    _visualOffsets[i] -= totalSpan;
                }
                else if (projected < -halfSpan)
                {
                    _visualOffsets[i] += totalSpan;
                }
            }

            // Apply the instant snap before the tween so there's no one-frame pop.
            ApplyOffsets();

            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
            }

            _scrollCoroutine = StartCoroutine(ScrollRoutine(scrollDelta));
        }

        private IEnumerator ScrollRoutine(float scrollDelta)
        {
            _isScrolling = true;

            float[] startOffsets = (float[])_visualOffsets.Clone();
            float[] targetOffsets = new float[_visualOffsets.Length];
            for (int i = 0; i < _visualOffsets.Length; i++)
            {
                targetOffsets[i] = startOffsets[i] + scrollDelta;
            }

            float elapsed = 0f;
            while (elapsed < ScrollDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ScrollDuration);
                float easedT = EvaluateEase(t);

                for (int i = 0; i < _visualOffsets.Length; i++)
                {
                    _visualOffsets[i] = Mathf.Lerp(startOffsets[i], targetOffsets[i], easedT);
                }

                ApplyOffsets();
                yield return null;
            }

            // Snap to exact target to clear any floating-point drift.
            for (int i = 0; i < _visualOffsets.Length; i++)
            {
                _visualOffsets[i] = targetOffsets[i];
            }

            ApplyOffsets();

            _isScrolling = false;
            _scrollCoroutine = null;
        }

        #endregion

        #region Position Helpers
        private void InitOffsets()
        {
            if (Entries == null)
            {
                return;
            }

            _visualOffsets = new float[Entries.Length];
            for (int i = 0; i < Entries.Length; i++)
            {
                _visualOffsets[i] = WrapOffset((i - _activeIndex) * ItemSpacing);
            }
        }

        /// </summary>
        private void ApplyOffsets()
        {
            if (Entries == null || _visualOffsets == null)
            {
                return;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                var visual = Entries[i].Visual;
                if (visual == null)
                {
                    continue;
                }

                float offset = _visualOffsets[i];

                if (visual.TryGetComponent<RectTransform>(out var rt))
                {
                    var pos = rt.anchoredPosition;
                    if (Axis == CarouselAxis.Horizontal)
                    {
                        pos.x = offset;
                    }
                    else
                    {
                        pos.y = offset;
                    }

                    rt.anchoredPosition = pos;
                }
                else
                {
                    var pos = visual.transform.localPosition;
                    if (Axis == CarouselAxis.Horizontal)
                    {
                        pos.x = offset;
                    }
                    else
                    {
                        pos.y = offset;
                    }

                    visual.transform.localPosition = pos;
                }
            }
        }

        private float WrapOffset(float offset)
        {
            if (Entries == null || Entries.Length == 0)
            {
                return offset;
            }

            float totalSpan = Entries.Length * ItemSpacing;
            float halfSpan = totalSpan * 0.5f;

            while (offset > halfSpan)
            {
                offset -= totalSpan;
            }

            while (offset < -halfSpan)
            {
                offset += totalSpan;
            }

            return offset;
        }

        private float EvaluateEase(float t)
        {
            return ScrollEase switch
            {
                Ease.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
                Ease.OutCubic => 1f - Mathf.Pow(1f - t, 3f),
                _ => t,
            };
        }

        #endregion

        #region Entry State

        private void UpdateEntryStates()
        {
            if (Entries == null)
            {
                return;
            }

            for (int i = 0; i < Entries.Length; i++)
            {
                var entry = Entries[i];
                if (entry.LockedOverlay != null)
                {
                    bool locked = entry.Location != null && entry.Location.IsLocked;
                    entry.LockedOverlay.SetActive(locked);
                }
            }

            if (FlavorTextLabel != null && _activeIndex >= 0 && _activeIndex < Entries.Length)
            {
                FlavorTextLabel.text =
                    Entries[_activeIndex].Location != null
                    && Entries[_activeIndex].Location.IsLocked
                        ? Entries[_activeIndex].LockedFlavorText ?? string.Empty
                        : Entries[_activeIndex].FlavorText ?? string.Empty;
            }
            if (LocationNameLabel != null && _activeIndex >= 0 && _activeIndex < Entries.Length)
            {
                LocationNameLabel.text = Entries[_activeIndex].Name ?? string.Empty;
            }
        }

        #endregion
    }
}
