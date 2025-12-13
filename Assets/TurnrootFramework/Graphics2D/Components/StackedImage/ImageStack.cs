using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Graphics.Portrait
{
    [CreateAssetMenu(fileName = "NewImageStack", menuName = "Turnroot/Graphics/ImageStack")]
    public class ImageStack : ScriptableObject
    {
        [SerializeField]
        private List<ImageStackLayer> _layers = new();

        public List<ImageStackLayer> Layers => _layers;
    }
}
