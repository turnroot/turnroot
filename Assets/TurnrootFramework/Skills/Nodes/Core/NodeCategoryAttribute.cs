using System;
using UnityEngine;

namespace Turnroot.Skills.Nodes
{
    /// <summary>
    /// Attribute to categorize skill nodes by their purpose.
    /// Automatically applies tint color based on category.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeCategoryAttribute : Attribute
    {
        public NodeCategory Category { get; }

        public NodeCategoryAttribute(NodeCategory category)
        {
            Category = category;
        }

        public Color GetTintColor() => GetCategoryColor(Category);

        /// <summary>
        /// Get the tint color for a specific category without requiring an attribute instance.
        /// </summary>
        public static Color GetCategoryColor(NodeCategory category)
        {
            return category switch
            {
                // Tailwind green-900 (#14532d)
                NodeCategory.Flow => new Color(20f / 255f, 83f / 255f, 45f / 255f),

                // Tailwind teal-900 (#134e4a)
                NodeCategory.Math => new Color(19f / 255f, 78f / 255f, 74f / 255f),

                // Tailwind sky-900 (#0c4a6e)
                NodeCategory.Events => new Color(12f / 255f, 74f / 255f, 110f / 255f),

                // Tailwind indigo-900 (#312e81)
                NodeCategory.Conditions => new Color(49f / 255f, 46f / 255f, 129f / 255f),

                _ => Color.gray,
            };
        }
    }

    /// <summary>
    /// Categories for organizing and color-coding skill nodes in the editor.
    /// </summary>
    public enum NodeCategory
    {
        Flow,
        Math,
        Events,
        Conditions,
    }
}
