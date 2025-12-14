using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Popup window content that displays a scrollable grid of sprite previews and
/// invokes a selection callback when the user clicks a tile. Editor-only.
/// </summary>
public class FilteredSpritePicker : PopupWindowContent
{
    private Sprite[] _sprites;
    private readonly System.Action<Sprite> _onSelect;
    private Vector2 _scroll;
    private const int TileSize = 64;
    private int _selectedIndex = -1;
    private readonly string _tag;

    public FilteredSpritePicker(
        string tag,
        System.Action<Sprite> onSelect,
        Sprite initialSelected = null
    )
    {
        _tag = tag ?? string.Empty;
        _sprites = PortraitLayerSpriteCache.GetSprites(_tag) ?? new Sprite[0];
        _onSelect = onSelect;
        if (initialSelected != null)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] == initialSelected)
                {
                    _selectedIndex = i;
                    break;
                }
            }
        }
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(360, 360);
    }

    public override void OnGUI(Rect rect)
    {
        if (_sprites == null || _sprites.Length == 0)
        {
            GUILayout.Label("(no sprites)");
            return;
        }

        int padding = 8;
        int cols = Mathf.Max(1, Mathf.FloorToInt(rect.width / (TileSize + padding)));
        _scroll = GUILayout.BeginScrollView(_scroll);

        int index = 0;
        while (index < _sprites.Length)
        {
            GUILayout.BeginHorizontal();
            for (int c = 0; c < cols && index < _sprites.Length; c++, index++)
            {
                var s = _sprites[index];

                // Reserve a rect for the tile so we can draw a highlight/border
                Rect tileRect = GUILayoutUtility.GetRect(
                    TileSize,
                    TileSize,
                    GUILayout.Width(TileSize),
                    GUILayout.Height(TileSize)
                );

                // Background highlight for selected index
                if (index == _selectedIndex)
                {
                    EditorGUI.DrawRect(tileRect, new Color(0f, 0.5f, 1f, 0.12f));
                    // outline
                    Handles.DrawSolidRectangleWithOutline(
                        tileRect,
                        Color.clear,
                        new Color(0f, 0.5f, 1f, 0.9f)
                    );
                }

                Texture preview =
                    AssetPreview.GetAssetPreview(s)
                    ?? (Texture)EditorGUIUtility.IconContent("Sprite Icon").image;

                // Click behavior: select and close
                if (GUI.Button(tileRect, GUIContent.none))
                {
                    _selectedIndex = index;
                    _onSelect?.Invoke(s);
                    if (editorWindow != null)
                    {
                        editorWindow.Close();
                    }

                    GUIUtility.ExitGUI();
                    return;
                }

                // Draw the preview texture inside the tile rect
                if (preview != null)
                {
                    GUI.DrawTexture(tileRect, preview, ScaleMode.ScaleToFit);
                }

                // Tooltip (sprite name) for the tile
                if (!string.IsNullOrEmpty(s.name))
                {
                    var tooltipRect = new Rect(tileRect.x, tileRect.yMax - 14, tileRect.width, 14);
                    GUI.Label(tooltipRect, s.name, EditorStyles.miniLabel);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();

        // Refresh button at bottom
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            PortraitLayerSpriteCache.Refresh(_tag);
            _sprites = PortraitLayerSpriteCache.GetSprites(_tag) ?? new Sprite[0];
            if (editorWindow != null)
            {
                editorWindow.Repaint();
            }
        }
        GUILayout.EndHorizontal();

        // Keyboard navigation
        var ev = Event.current;
        if (ev.type == EventType.KeyDown)
        {
            int prev = _selectedIndex;
            if (ev.keyCode == KeyCode.RightArrow)
            {
                if (_selectedIndex < _sprites.Length - 1)
                {
                    _selectedIndex++;
                }

                ev.Use();
            }
            else if (ev.keyCode == KeyCode.LeftArrow)
            {
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                }

                ev.Use();
            }
            else if (ev.keyCode == KeyCode.UpArrow)
            {
                _selectedIndex = Mathf.Max(0, _selectedIndex - cols);
                ev.Use();
            }
            else if (ev.keyCode == KeyCode.DownArrow)
            {
                if (_selectedIndex < 0)
                {
                    _selectedIndex = 0;
                }
                else
                {
                    _selectedIndex = Mathf.Min(_sprites.Length - 1, _selectedIndex + cols);
                }

                ev.Use();
            }
            else if (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter)
            {
                if (_selectedIndex >= 0 && _selectedIndex < _sprites.Length)
                {
                    var s = _sprites[_selectedIndex];
                    _onSelect?.Invoke(s);
                    if (editorWindow != null)
                    {
                        editorWindow.Close();
                    }

                    GUIUtility.ExitGUI();
                    return;
                }
            }

            if (prev != _selectedIndex && editorWindow != null)
            {
                editorWindow.Repaint();
            }
        }
    }
}
