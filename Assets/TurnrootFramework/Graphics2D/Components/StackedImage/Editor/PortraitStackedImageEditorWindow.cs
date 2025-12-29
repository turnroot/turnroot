using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Turnroot.Graphics2D.Editor
{
    /// <summary>
    /// Specialized editor window base for portrait-like stacked images.
    /// It enforces mandatory layers (by index or by Tag) and disables removal
    /// for those layers. Subclasses can override GetMandatoryLayerTags to
    /// specify required tags.
    /// </summary>
    public abstract class PortraitStackedImageEditorWindow<TOwner, TStackedImage>
        : StackedImageEditorWindow<TOwner, TStackedImage>
        where TOwner : UnityEngine.Object
        where TStackedImage : StackedImage<TOwner>
    {
        /// <summary>
        /// Return an array of tags that must exist in the ImageStack and cannot be removed.
        /// Default: empty (no enforced tags). Override in Portrait-specific editor.
        /// </summary>
        protected virtual string[] GetMandatoryLayerTags() => Array.Empty<string>();

        /// <summary>
        /// Return an array of indices (0-based) that cannot be removed from the stack.
        /// Default: empty. Override to reserve specific positions.
        /// </summary>
        protected virtual int[] GetMandatoryLayerIndices() => Array.Empty<int>();

        protected override void DrawControlPanel()
        {
            // Let base draw its panels (including layer management)
            base.DrawControlPanel();

            // If current image stack exists, ensure mandatory tags/indices are present
            var imageStack = _currentImage.ImageStack;
            if (imageStack == null)
            {
                return;
            }

            var tags = GetMandatoryLayerTags();
            var indices = GetMandatoryLayerIndices();

            // Ensure mandatory-tagged layers exist (if not, add placeholders at the end)
            foreach (var tag in tags)
            {
                bool found = imageStack.Layers.Any(l =>
                    string.Equals(l?.Tag ?? string.Empty, tag, StringComparison.OrdinalIgnoreCase)
                );
                if (!found)
                {
                    var newLayer = new ImageStackLayer()
                    {
                        Sprite = null,
                        Mask = null,
                        Offset = Vector2.zero,
                        Scale = 1f,
                        Rotation = 0f,
                        Order = imageStack.Layers.Count,
                        Tag = tag,
                    };
                    imageStack.Layers.Add(newLayer);
                    EditorUtility.SetDirty(imageStack);
                }
            }

            // Ensure mandatory indices are within bounds by inserting empty layers if necessary
            if (indices != null && indices.Length > 0)
            {
                int maxIndex = indices.Max();
                while (imageStack.Layers.Count <= maxIndex)
                {
                    var newLayer = new ImageStackLayer()
                    {
                        Sprite = null,
                        Mask = null,
                        Offset = Vector2.zero,
                        Scale = 1f,
                        Rotation = 0f,
                        Order = imageStack.Layers.Count,
                    };
                    imageStack.Layers.Add(newLayer);
                    EditorUtility.SetDirty(imageStack);
                }
            }

            // After base list has been drawn we need to modify the ReorderableList behavior so mandatory layers cannot be removed.
            // The base class creates and stores the ReorderableList in a protected field named _layersReorderList — we can access it via reflection.
            var type = typeof(StackedImageEditorWindow<TOwner, TStackedImage>);
            var field = type.GetField(
                "_layersReorderList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            if (field == null)
            {
                return;
            }

            var reorder = field.GetValue(this) as ReorderableList;
            if (reorder == null)
            {
                return;
            }

            // Replace onRemoveCallback to prevent removing mandatory layers
            reorder.onRemoveCallback = list =>
            {
                int removeIndex = list.index;

                // Check by index requirement
                if (indices != null && indices.Contains(removeIndex))
                {
                    EditorUtility.DisplayDialog(
                        "Cannot remove layer",
                        "This layer is mandatory for portraits and cannot be removed.",
                        "OK"
                    );
                    return;
                }

                // Check by tag requirement
                var layersProp = (new SerializedObject(imageStack)).FindProperty("_layers");
                if (layersProp != null && removeIndex >= 0 && removeIndex < layersProp.arraySize)
                {
                    var el = layersProp.GetArrayElementAtIndex(removeIndex);
                    var tagProp = el.FindPropertyRelative("Tag");
                    if (tagProp != null)
                    {
                        string tag = tagProp.stringValue;
                        if (
                            !string.IsNullOrEmpty(tag)
                            && tags.Contains(tag, StringComparer.OrdinalIgnoreCase)
                        )
                        {
                            EditorUtility.DisplayDialog(
                                "Cannot remove layer",
                                "This layer is mandatory for portraits (tag: "
                                    + tag
                                    + ") and cannot be removed.",
                                "OK"
                            );
                            return;
                        }
                    }
                }

                // Fallback to default removal behavior (delete element at index)
                var so = new SerializedObject(imageStack);
                var layers = so.FindProperty("_layers");
                if (layers != null)
                {
                    layers.DeleteArrayElementAtIndex(removeIndex);
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(imageStack);
                }
            };
        }
    }
}
