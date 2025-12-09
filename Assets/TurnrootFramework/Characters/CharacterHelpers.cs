using System;
using System.Collections.Generic;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Characters.Subclasses;

namespace Turnroot.Characters
{
    /// <summary>
    /// Small helpers used across CharacterData/CharacterInstance to reduce duplication.
    /// These contain pure utility functions that do not change runtime semantics.
    /// </summary>
    public static class CharacterHelpers
    {
        public static List<BoundedCharacterStat> CloneBoundedStats(List<BoundedCharacterStat> src)
        {
            var list = new List<BoundedCharacterStat>();
            if (src == null)
            {
                return list;
            }

            foreach (var s in src)
            {
                list.Add(new BoundedCharacterStat(s.Max, s.Current, s.Min, s.StatType));
            }

            return list;
        }

        public static List<CharacterStat> CloneUnboundedStats(List<CharacterStat> src)
        {
            var list = new List<CharacterStat>();
            if (src == null)
            {
                return list;
            }
            // Note: new CharacterStat(stat) uses the same constructor pattern used by the template
            foreach (var s in src)
            {
                list.Add(new CharacterStat(s.Current, s.StatType));
            }

            return list;
        }

        public static List<SupportRelationshipInstance> CloneSupportRelationships(
            List<SupportRelationship> templates,
            CharacterData owner
        )
        {
            var list = new List<SupportRelationshipInstance>();
            if (templates == null)
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
            SerializableDictionary<string, Portrait> portraits,
            Action<ImageStackLayer> action
        )
        {
            if (portraits == null || action == null)
            {
                return;
            }

            foreach (var p in portraits.Values)
            {
                var layers = p.ImageStack?.Layers;
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
    }
}
