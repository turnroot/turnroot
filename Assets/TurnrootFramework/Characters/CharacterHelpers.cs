using System;
using System.Collections.Generic;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;
using Turnroot.Graphics2D;
using Turnroot.Utilities;

namespace Turnroot.Characters
{
    /// <summary>
    /// Small helpers used across CharacterData/CharacterInstance to reduce duplication.
    /// </summary>
    public static class CharacterHelpers
    {
        /// <summary>
        /// Key used when looking up the default portrait in a dictionary of portraits.
        /// This avoids hard‑coding the literal string throughout the codebase.
        /// </summary>
        public const string DefaultPortraitKey = "default";

        public static List<BoundedCharacterStat> CloneBoundedStats(List<BoundedCharacterStat> src)
        {
            var list = new List<BoundedCharacterStat>();
            if (!ValidationHelper.ValidateNotNull(src, nameof(src)))
            {
                return list;
            }

            foreach (var s in src)
            {
                if (s != null)
                {
                    // Use copy constructor to preserve exact internal state
                    list.Add(new BoundedCharacterStat(s));
                }
                else
                {
                    "CharacterHelpers.CloneBoundedStats: encountered null stat in source list".LogWarning();
                }
            }

            return list;
        }

        public static List<CharacterStat> CloneUnboundedStats(List<CharacterStat> src)
        {
            var list = new List<CharacterStat>();
            if (!ValidationHelper.ValidateNotNull(src, nameof(src)))
            {
                return list;
            }
            // Note: new CharacterStat(stat) uses the same constructor pattern used by the template
            foreach (var s in src)
            {
                if (s != null)
                {
                    // Use copy constructor to preserve exact internal state (_current, _bonus, _statType)
                    list.Add(new CharacterStat(s));
                }
                else
                {
                    "CharacterHelpers.CloneUnboundedStats: encountered null stat in source list".LogWarning();
                }
            }

            return list;
        }

        public static List<SupportRelationshipInstance> CloneSupportRelationships(
            List<SupportRelationship> templates,
            CharacterData owner
        )
        {
            var list = new List<SupportRelationshipInstance>();
            if (!ValidationHelper.ValidateNotNull(templates, nameof(templates)))
            {
                return list;
            }

            foreach (var rel in templates)
            {
                // Skip invalid relationships (same character)
                if (rel.Character == owner)
                {
                    continue;
                }

                list.Add(new SupportRelationshipInstance(rel));
            }
            return list;
        }

        public static void ForEachPortraitLayer(
            IEnumerable<Portrait> portraits,
            Action<ImageStackLayer> action
        )
        {
            if (
                !ValidationHelper.ValidateNotNull(portraits, nameof(portraits))
                || !ValidationHelper.ValidateNotNull(action, nameof(action))
            )
            {
                return;
            }

            foreach (var p in portraits)
            {
                if (p == null)
                {
                    continue;
                }
                var layers = p.Layers;
                if (layers == null)
                {
                    continue;
                }

                foreach (var layer in layers)
                {
                    action(layer);
                }
            }
        }

        public static Portrait GetDefaultPortrait(
            IEnumerable<CharacterData.PortraitEntry> portraits
        )
        {
            if (!ValidationHelper.ValidateNotNull(portraits, nameof(portraits)))
            {
                $"CharacterHelpers: No default portrait ({DefaultPortraitKey}) because no portraits found".LogWarning();
                return null;
            }

            Portrait first = null;
            foreach (var entry in portraits)
            {
                if (entry.Portrait == null)
                {
                    continue;
                }

                first ??= entry.Portrait;

                if (
                    string.Equals(entry.Key, DefaultPortraitKey, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return entry.Portrait;
                }
            }

            return first;
        }
    }
}
