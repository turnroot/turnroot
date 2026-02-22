using System.Collections.Generic;
using System.Linq;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain.Components.Battle
{
    /// <summary>
    /// Partial class containing helper methods for tile highlighting, path preview, and range visualization in battles.
    /// </summary>
    public partial class TileHighlighter : MonoBehaviour
    {
        #region Public API

        /// <summary>
        /// Defines the different types of tile highlights available for visualizing battle actions.
        /// </summary>
        public enum HighlightType
        {
            Move,
            Attack,
            Heal,
            Danger,
            PathPreview,
        }

        public void HighlightTiles(IEnumerable<Vector2Int> tiles, HighlightType highlightType)
        {
            switch (highlightType)
            {
                case HighlightType.Move:
                    ClearMoveTiles();
                    BatchHighlightTiles(tiles, _moveRangeUVParams, _activeMoveTiles);
                    break;
                case HighlightType.Attack:
                    ClearAttackTiles();
                    BatchHighlightTiles(tiles, _attackRangeUVParams, _activeAttackTiles);
                    break;
                case HighlightType.Heal:
                    ClearHealTiles();
                    BatchHighlightTiles(tiles, _healRangeUVParams, _activeHealTiles);
                    break;
                case HighlightType.Danger:
                    ClearDangerTiles();
                    BatchHighlightTiles(tiles, _dangerZoneUVParams, _activeDangerTiles);
                    break;
                case HighlightType.PathPreview:
                    ClearPathPreview();
                    break;
            }
        }

        public void ClearMoveTiles() => BatchClearTiles(_activeMoveTiles);

        public void ClearAttackTiles() => BatchClearTiles(_activeAttackTiles);

        public void ClearHealTiles() => BatchClearTiles(_activeHealTiles);

        public void ClearDangerTiles() => BatchClearTiles(_activeDangerTiles);

        public void ClearPathPreview()
        {
            if (_pathDecalPool == null)
            {
                return;
            }

            for (int i = 0; i < _activePathDecalCount && i < MAX_PATH_LENGTH; i++)
            {
                _pathDecalPool[i]?.gameObject.SetActive(false);
            }
            _activePathDecalCount = 0;
        }

        public void ClearAll()
        {
            ClearMoveTiles();
            ClearAttackTiles();
            ClearHealTiles();
            ClearDangerTiles();
            ClearPathPreview();
        }

        public void HighlightPath(IList<Vector2Int> path, Vector2Int startTile)
        {
            ClearPathPreview();

            if (_pathDecalPool == null)
            {
                "Path decal pool is null!".LogError();
                return;
            }

            // Render only START tile if no movement
            if (path == null || path.Count == 0)
            {
                RenderPathDecal(0, startTile, _pathStartUVParams, 0f);
                _activePathDecalCount = 1;
                return;
            }

            // Render START tile pointing toward first movement tile
            Vector2Int startOutgoing = path[0] - startTile;
            RenderPathDecal(0, startTile, _pathStartUVParams, DirToRotation(startOutgoing));

            // Render movement tiles
            int maxMovable = Mathf.Min(path.Count, MAX_PATH_LENGTH - 1);
            int decalIndex = 1;

            for (int i = 0; i < maxMovable; i++)
            {
                var curr = path[i];
                Vector2Int prev = i == 0 ? startTile : path[i - 1];
                Vector2Int incoming = curr - prev;

                Vector4 uvParams;
                float rotation;

                if (i == maxMovable - 1)
                {
                    // End piece
                    uvParams = _pathEndUVParams;
                    rotation = DirToRotation(incoming);
                }
                else
                {
                    Vector2Int outgoing = path[i + 1] - curr;

                    if (IsStraight(incoming, outgoing))
                    {
                        uvParams = _pathStraightUVParams;
                        rotation = DirToRotation(incoming);
                    }
                    else
                    {
                        uvParams = _pathCornerUVParams;
                        rotation = ComputeCornerRotation(incoming, outgoing);
                    }
                }

                RenderPathDecal(decalIndex++, curr, uvParams, rotation);
            }

            _activePathDecalCount = Mathf.Min(decalIndex, MAX_PATH_LENGTH);
        }

        public void HighlightPath(IList<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                ClearPathPreview();
                return;
            }

            HighlightPath(path.Skip(1).ToList(), path[0]);
        }

        #endregion
        #region Path Helpers

        private static float DirToRotation(Vector2Int dir)
        {
            return dir == Vector2Int.up ? 0f
                : dir == Vector2Int.right ? 90f
                : dir == Vector2Int.down ? 180f
                : dir == Vector2Int.left ? 270f
                : 0f;
        }

        private static bool IsStraight(Vector2Int incoming, Vector2Int outgoing) =>
            (incoming.x == 0 && outgoing.x == 0) || (incoming.y == 0 && outgoing.y == 0);

        private static float ComputeCornerRotation(Vector2Int incoming, Vector2Int outgoing)
        {
            var cornerMap = new Dictionary<(int, int, int, int), float>
            {
                // Clockwise turns
                { (0, 1, 1, 0), 90f }, // Up -> Right
                { (1, 0, 0, -1), 180f }, // Right -> Down
                { (0, -1, -1, 0), 270f }, // Down -> Left
                { (-1, 0, 0, 1), 0f }, // Left -> Up
                // Counter-clockwise turns
                { (1, 0, 0, 1), 270f }, // Right -> Up
                { (0, 1, -1, 0), 180f }, // Up -> Left
                { (-1, 0, 0, -1), 90f }, // Left -> Down
                { (0, -1, 1, 0), 0f }, // Down -> Right
            };

            var key = (incoming.x, incoming.y, outgoing.x, outgoing.y);
            if (cornerMap.TryGetValue(key, out var rotation))
            {
                return rotation;
            }

            $"ComputeCornerRotation: no match for {incoming} -> {outgoing}".LogWarning();
            return 0f;
        }
        #endregion
    }
}


