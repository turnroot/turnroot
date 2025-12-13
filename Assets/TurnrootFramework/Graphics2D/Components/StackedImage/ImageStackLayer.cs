using System;
using UnityEngine;

[Serializable]
public class ImageStackLayer
{
    [SerializeField]
    public Sprite Sprite;

    [SerializeField]
    public Sprite Mask;

    [SerializeField]
    public Vector2 Offset;

    [SerializeField]
    public float Scale = 1f;

    [SerializeField]
    public float Rotation = 0f;

    [SerializeField]
    public int Order = 0;

    // Optional per-layer color for unmasked (grayscale) layers, e.g. Hair
    // Stored on the base class to ensure Unity serialization (avoid polymorphism issues)
    // Shown in the drawer only for unmasked layers (Tag == "Hair")
    [SerializeField]
    public Color Tint = Color.white;

    // Tag for set components (e.g. portrait "Hair", "Face", "Eyes")
    // Used by portrait editor to enforce mandatory layers. This field should be left empty for generic layers
    [SerializeField]
    public string Tag = string.Empty;
}
