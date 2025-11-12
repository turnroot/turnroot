using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class UnmaskedImageStackLayer : ImageStackLayer
{
    // Unmasked layers apply tinting only if the sprite is grayscale.
    // If the sprite has color, no tint is applied to preserve the original colors.
    // The check is performed by examining the PNG file header in the editor.
}
