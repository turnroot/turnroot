using System;
using System.Collections.Generic;

namespace Turnroot.Graphics2D.Tags
{
    /// <summary>
    /// Defines the contract for layer tags used in stacked images, including name, order, mandatory status, and folder path.
    /// </summary>
    public interface ILayerTag
    {
        string Name { get; }
        int Order { get; }
        bool IsMandatory { get; }
        string FolderPath { get; }
    }

    /// <summary>
    /// Concrete implementation of a layer tag with name, order, mandatory flag, and folder path properties.
    /// </summary>
    public class LayerTag : ILayerTag
    {
        public string Name { get; }
        public int Order { get; }
        public bool IsMandatory { get; }
        public string FolderPath { get; }

        public LayerTag(string name, int order, bool isMandatory = false, string folderPath = "")
        {
            Name = name;
            Order = order;
            IsMandatory = isMandatory;
            FolderPath = folderPath;
        }
    }

    /// <summary>
    /// Helper extension methods for working with collections of <see cref="ILayerTag"/>.
    /// These provide common lookups (by name or order) and simple filters (mandatory/optional).
    /// </summary>
    public static class LayerTagLookup
    {
        public static bool TryGet(this IEnumerable<ILayerTag> tags, string name, out ILayerTag tag)
        {
            if (string.IsNullOrEmpty(name) || tags == null)
            {
                tag = null;
                return false;
            }

            foreach (var t in tags)
            {
                if (t != null && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    tag = t;
                    return true;
                }
            }

            tag = null;
            return false;
        }

        public static ILayerTag Get(this IEnumerable<ILayerTag> tags, string name)
        {
            return tags.TryGet(name, out var t)
                ? t
                : throw new KeyNotFoundException($"Layer tag '{name}' not found.");
        }

        public static bool IsMandatory(this IEnumerable<ILayerTag> tags, string name)
        {
            if (string.IsNullOrEmpty(name) || tags == null)
            {
                return false;
            }

            foreach (var t in tags)
            {
                if (t != null && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return t.IsMandatory;
                }
            }

            return false;
        }

        public static bool TryGetByOrder(
            this IEnumerable<ILayerTag> tags,
            int order,
            out ILayerTag tag
        )
        {
            if (tags == null)
            {
                tag = null;
                return false;
            }

            foreach (var t in tags)
            {
                if (t != null && t.Order == order)
                {
                    tag = t;
                    return true;
                }
            }

            tag = null;
            return false;
        }

        public static ILayerTag GetByOrder(this IEnumerable<ILayerTag> tags, int order)
        {
            return tags.TryGetByOrder(order, out var t)
                ? t
                : throw new KeyNotFoundException($"Layer tag with order '{order}' not found.");
        }

        public static string[] Names(this IEnumerable<ILayerTag> tags)
        {
            if (tags == null)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            foreach (var t in tags)
            {
                if (t != null)
                {
                    list.Add(t.Name);
                }
            }

            return list.ToArray();
        }

        public static IEnumerable<ILayerTag> MandatoryTags(this IEnumerable<ILayerTag> tags)
        {
            if (tags == null)
            {
                yield break;
            }

            foreach (var t in tags)
            {
                if (t != null && t.IsMandatory)
                {
                    yield return t;
                }
            }
        }

        public static IEnumerable<ILayerTag> OptionalTags(this IEnumerable<ILayerTag> tags)
        {
            if (tags == null)
            {
                yield break;
            }

            foreach (var t in tags)
            {
                if (t != null && !t.IsMandatory)
                {
                    yield return t;
                }
            }
        }
    }

    /// <summary>
    /// Generic registry base class that provides static wrapper methods for types that expose
    /// a public static `Tags` collection (IEnumerable<ILayerTag> or IEnumerable<LayerTag>).
    /// Usage: `public sealed class PortraitLayerTags : LayerTagRegistry<PortraitLayerTags> { public static readonly List<LayerTag> Tags = ... }`
    /// Then call: `PortraitLayerTags.TryGet("Hair", out var tag)`
    /// </summary>
    public class LayerTagRegistry<T>
        where T : class
    {
        private static readonly Func<IEnumerable<ILayerTag>> _tagsAccessor = CreateTagsAccessor();

        // Ordered snapshot of tags (preserves original ordering)
        private static readonly Lazy<List<ILayerTag>> _orderedTags = new(
            () => new List<ILayerTag>(_tagsAccessor()),
            true
        );

        // Fast lookup caches (built once from the ordered list)
        private static readonly Lazy<Dictionary<string, ILayerTag>> _byName = new(
            () =>
            {
                var d = new Dictionary<string, ILayerTag>(StringComparer.OrdinalIgnoreCase);
                foreach (var t in _orderedTags.Value)
                {
                    if (t == null || string.IsNullOrEmpty(t.Name))
                    {
                        continue;
                    }

                    if (!d.ContainsKey(t.Name))
                    {
                        d[t.Name] = t;
                    }
                }

                return d;
            },
            true
        );

        private static readonly Lazy<Dictionary<int, ILayerTag>> _byOrder = new(
            () =>
            {
                var d = new Dictionary<int, ILayerTag>();
                foreach (var t in _orderedTags.Value)
                {
                    if (t == null)
                    {
                        continue;
                    }

                    if (!d.ContainsKey(t.Order))
                    {
                        d[t.Order] = t;
                    }
                }

                return d;
            },
            true
        );

        private static Func<IEnumerable<ILayerTag>> CreateTagsAccessor()
        {
            var t = typeof(T);
            var fi = t.GetField(
                "Tags",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
            if (fi != null && typeof(IEnumerable<ILayerTag>).IsAssignableFrom(fi.FieldType))
            {
                return () => (IEnumerable<ILayerTag>)fi.GetValue(null);
            }

            var prop = t.GetProperty(
                "Tags",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
            );
            return
                prop != null && typeof(IEnumerable<ILayerTag>).IsAssignableFrom(prop.PropertyType)
                ? (() => (IEnumerable<ILayerTag>)prop.GetValue(null))
                : throw new InvalidOperationException(
                    $"Type '{t.FullName}' must declare a public static field or property named 'Tags' of type IEnumerable<ILayerTag>."
                );
        }

        public static bool TryGet(string name, out ILayerTag tag)
        {
            if (string.IsNullOrEmpty(name))
            {
                tag = null;
                return false;
            }

            return _byName.Value.TryGetValue(name, out tag);
        }

        public static bool TryGet(string name, out LayerTag tag)
        {
            if (_byName.Value.TryGetValue(name, out var itag) && itag is LayerTag lt)
            {
                tag = lt;
                return true;
            }

            tag = null;
            return false;
        }

        public static ILayerTag Get(string name) =>
            TryGet(name, out ILayerTag t)
                ? t
                : throw new KeyNotFoundException($"Layer tag '{name}' not found.");

        public static LayerTag GetConcrete(string name)
        {
            var it = Get(name);
            return it is LayerTag lt
                ? lt
                : throw new InvalidCastException($"Layer tag '{name}' is not a LayerTag instance.");
        }

        public static bool IsMandatory(string name) =>
            TryGet(name, out ILayerTag t) && t.IsMandatory;

        public static bool TryGetByOrder(int order, out ILayerTag tag) =>
            _byOrder.Value.TryGetValue(order, out tag);

        public static ILayerTag GetByOrder(int order) =>
            TryGetByOrder(order, out ILayerTag t)
                ? t
                : throw new KeyNotFoundException($"Layer tag with order '{order}' not found.");

        /// <summary>
        /// Get the Order value for the tag with the given name. Throws if not found.
        /// </summary>
        public static int GetOrder(string name) => Get(name).Order;

        /// <summary>
        /// Try to get the Order value for the tag with the given name.
        /// Returns false and sets order to -1 if not found.
        /// </summary>
        public static bool TryGetOrder(string name, out int order)
        {
            if (TryGet(name, out ILayerTag itag))
            {
                order = itag.Order;
                return true;
            }

            order = -1;
            return false;
        }

        public static string[] Names() => _tagsAccessor().Names();

        public static IEnumerable<ILayerTag> MandatoryTags() => _tagsAccessor().MandatoryTags();

        public static IEnumerable<ILayerTag> OptionalTags() => _tagsAccessor().OptionalTags();
    }
}
