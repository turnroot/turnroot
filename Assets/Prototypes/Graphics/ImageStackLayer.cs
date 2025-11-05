using System;
using System.Collections.Generic;
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
}
