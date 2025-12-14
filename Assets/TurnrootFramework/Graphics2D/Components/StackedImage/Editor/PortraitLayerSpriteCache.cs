using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only cache that provides sprites and names for portrait layer tags.
/// Tag -> folder partial mapping convention: Tag "Hair" -> "PortraitComponents/Hair/"
/// It performs an AssetDatabase scan and caches results per tag. Call Refresh(tag)
/// or RefreshAll() when assets change.
/// </summary>
public static class PortraitLayerSpriteCache
{
    private static readonly Dictionary<string, Sprite[]> _sprites = new(
        StringComparer.OrdinalIgnoreCase
    );
    private static readonly Dictionary<string, string[]> _names = new(
        StringComparer.OrdinalIgnoreCase
    );

    private static string TagToPartial(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return string.Empty;
        }

        return $"PortraitComponents/{tag.Trim().Replace(' ', '_')}/";
    }

    public static void RefreshAll()
    {
        _sprites.Clear();
        _names.Clear();
    }

    public static void Refresh(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            // Remove empty/null tag entries if they exist
            _sprites.Remove(tag);
            _names.Remove(tag);
            return;
        }

        var partial = TagToPartial(tag);
        var results = new List<Sprite>();
        var names = new List<string>();

        var guids = AssetDatabase.FindAssets("t:Sprite");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]).Replace("\\", "/");
            if (path.IndexOf(partial, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null)
                {
                    results.Add(s);
                    names.Add(s.name);
                }
            }
        }

        _sprites[tag] = results.ToArray();
        _names[tag] = names.ToArray();
    }

    public static Sprite[] GetSprites(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return Array.Empty<Sprite>();
        }

        // Use TryGetValue to avoid double lookup - refresh if not found
        if (!_sprites.TryGetValue(tag, out var arr))
        {
            Refresh(tag);
            return _sprites.TryGetValue(tag, out arr) ? arr : Array.Empty<Sprite>();
        }
        return arr;
    }

    public static string[] GetNames(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return Array.Empty<string>();
        }

        // Use TryGetValue to avoid double lookup - refresh if not found
        if (!_names.TryGetValue(tag, out var arr))
        {
            Refresh(tag);
            return _names.TryGetValue(tag, out arr) ? arr : Array.Empty<string>();
        }
        return arr;
    }
}
