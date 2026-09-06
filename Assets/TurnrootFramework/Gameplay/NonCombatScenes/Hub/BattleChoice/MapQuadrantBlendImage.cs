using UnityEngine;
using UnityEngine.UI;
using static Turnroot.Gameplay.Brain.GamewideContextBrainHelpers;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    /// <summary>
    /// Drives a <see cref="RawImage"/> whose material uses the
    /// <c>Turnroot/UI/MapQuadrantBlend</c> shader.
    ///
    /// Assign <see cref="BaseMaterial"/> in the inspector (a Material asset
    /// referencing the shader).  At runtime a per-instance copy is created so
    /// multiple BattleChoiceUI panels never share the same material state.
    ///
    /// Call <see cref="SetSprites"/> or <see cref="SetFromExplorationStatus"/>
    /// whenever the highlighted battle changes.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MapQuadrantBlendImage : MonoBehaviour
    {
        [Tooltip(
            "A Material asset using the Turnroot/UI/MapQuadrantBlend shader. "
                + "A runtime copy is made automatically."
        )]
        public Material BaseMaterial;

        private static readonly int TopLeftId = Shader.PropertyToID("_TopLeft");
        private static readonly int TopRightId = Shader.PropertyToID("_TopRight");
        private static readonly int BottomLeftId = Shader.PropertyToID("_BottomLeft");
        private static readonly int BottomRightId = Shader.PropertyToID("_BottomRight");

        private RawImage _rawImage;
        private Material _materialInstance;

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();

            if (BaseMaterial != null)
            {
                _materialInstance = new Material(BaseMaterial);
                _rawImage.material = _materialInstance;
            }
            else
            {
                Debug.LogWarning(
                    "MapQuadrantBlendImage: BaseMaterial is not assigned. Assign a Material using the MapQuadrantBlend shader.",
                    this
                );
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

        public void SetTextures(
            Texture topLeft,
            Texture topRight,
            Texture bottomLeft,
            Texture bottomRight
        )
        {
            if (_materialInstance == null)
            {
                return;
            }

            if (topLeft != null)
            {
                _materialInstance.SetTexture(TopLeftId, topLeft);
            }

            if (topRight != null)
            {
                _materialInstance.SetTexture(TopRightId, topRight);
            }

            if (bottomLeft != null)
            {
                _materialInstance.SetTexture(BottomLeftId, bottomLeft);
            }

            if (bottomRight != null)
            {
                _materialInstance.SetTexture(BottomRightId, bottomRight);
            }
        }

        public void SetSprites(
            Sprite topLeft,
            Sprite topRight,
            Sprite bottomLeft,
            Sprite bottomRight
        )
        {
            SetTextures(
                topLeft?.texture,
                topRight?.texture,
                bottomLeft?.texture,
                bottomRight?.texture
            );
        }

        /// <summary>
        /// Chooses the explored or unexplored sprite for each quadrant based on
        /// <paramref name="status"/> and uploads all four to the material.
        /// A quadrant is shown as explored when its state is
        /// <see cref="QuadrantExploredState.FullyExplored"/>.
        /// </summary>
        public void SetFromExplorationStatus(ExploreStatusSprites sprites, ExploredStatus status)
        {
            SetSprites(
                IsExplored(status.TopLeft)
                    ? sprites.TopLeftExploredSprite
                    : sprites.TopLeftUnexploredSprite,
                IsExplored(status.TopRight)
                    ? sprites.TopRightExploredSprite
                    : sprites.TopRightUnexploredSprite,
                IsExplored(status.BottomLeft)
                    ? sprites.BottomLeftExploredSprite
                    : sprites.BottomLeftUnexploredSprite,
                IsExplored(status.BottomRight)
                    ? sprites.BottomRightExploredSprite
                    : sprites.BottomRightUnexploredSprite
            );
        }

        private static bool IsExplored(QuadrantExploredState state) =>
            state == QuadrantExploredState.FullyExplored;
    }
}
