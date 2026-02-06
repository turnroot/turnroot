using System;
using System.Collections.Generic;
using Turnroot.Graphics2D.Tags;
using UnityEditor;
using UnityEngine;

namespace Turnroot.Graphics2D
{
    /// <summary>
    /// Caches sprite assets for portrait layers organized by tag.
    /// Provides efficient lookup of sprites by layer tag from the asset database.
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

            // Refresh caches for all known portrait tags from the registry
            foreach (var name in PortraitLayerTags.Names())
            {
                Refresh(name);
            }
        }

        public static void Refresh(ILayerTag tag) => Refresh(tag?.Name);

        public static void Refresh(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                // Remove empty/null tag entries if they exist
                _sprites.Remove(tag);
                _names.Remove(tag);
                return;
            }

            // If the tag isn't registered, warn -- still allow refreshing custom tags.
            if (!PortraitLayerTags.TryGet(tag, out _))
            {
                Debug.LogWarning(
                    $"Refreshing sprites for unknown portrait tag '{tag}'. This tag is not registered in PortraitLayerTags."
                );
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

        public static Sprite[] GetSprites(ILayerTag tag) =>
            tag == null ? Array.Empty<Sprite>() : GetSprites(tag.Name);

        public static string[] GetNames(ILayerTag tag) =>
            tag == null ? Array.Empty<string>() : GetNames(tag.Name);

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
}
