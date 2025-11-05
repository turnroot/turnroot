using System.Collections.Generic;
using Assets.Prototypes.Characters;
using UnityEngine;

namespace Assets.Prototypes.Graphics.Portrait
{
    [CreateAssetMenu(fileName = "NewImageStack", menuName = "Graphics/Portrait/ImageStack")]
    public class ImageStack : ScriptableObject
    {
        [SerializeField]
        private Character _ownerCharacter;

        [SerializeField]
        private List<ImageStackLayer> _layers = new List<ImageStackLayer>();

        public List<ImageStackLayer> Layers => _layers;
        public Character OwnerCharacter => _ownerCharacter;

        public Texture2D PreRender()
        {
            GraphicsPrototypesSettings gps;
            gps = Resources.Load<GraphicsPrototypesSettings>("GraphicsPrototypesSettings");

            int width = gps.portraitRenderWidth;
            int height = gps.portraitRenderHeight;

            // Create a new texture to render into
            Texture2D renderTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Clear the texture with transparent pixels
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 0);
            }
            renderTexture.SetPixels32(pixels);
            renderTexture.Apply();

            return renderTexture;
        }

        public Sprite Render()
        {
            return null;
        }
    }
}
