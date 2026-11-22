using Turnroot.Gameplay.PlayerSettings;
using Turnroot.GameSettings;
using Turnroot.Graphics3D;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager : MonoBehaviour
    {
        private enum GroundSurface
        {
            Grass,
            Sand,
            Rock,
            Snow,
        }

        private Mesh _cachedGroundMesh;
        private int[] _cachedGroundTriangles;
        private Color[] _cachedGroundColors;
        private bool _hasGroundSample;
        private Color _lastGroundColor;



    }
}
