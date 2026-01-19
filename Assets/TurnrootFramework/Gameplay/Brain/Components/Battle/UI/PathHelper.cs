using System.Collections.Generic;
using Turnroot.Gameplay.Maps;
using Turnroot.GameSettings;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class PathHelper
    {
        public Sprite PathTipSprite;

        public Sprite PathPreTipSprite; // end -1 of the path
        public Sprite PathStraightSprite;
        public Sprite PathCornerSprite;
        public Sprite PathStartSprite;

        public GamewideUiSettings UiSettings;

        public PathHelper()
        {
            UiSettings = GameSettingsLoader.LoadFirst<GamewideUiSettings>();
            PathTipSprite = UiSettings.PathTipSprite;
            PathPreTipSprite = UiSettings.PathPreTipSprite;
            PathStraightSprite = UiSettings.PathStraightSprite;
            PathCornerSprite = UiSettings.PathCornerSprite;
            PathStartSprite = UiSettings.PathStartSprite;
        }

        public struct PathSprite
        {
            public Sprite sprite;
            public float rotation;
        }

        public Dictionary<Vector2Int, PathSprite> PathSprites = new();

        private void SetSpriteForPoints(List<MapGridPoint> path)
        {
            // Clear any existing path sprites
            PathSprites.Clear();

            // No path to show
            if (path.Count <= 0)
            {
                return;
            }

            // Handle different path lengths
            switch (path.Count)
            {
                case 1:
                    // Staying in place - no path visualization needed
                    return;

                case 2:
                    // Simple move: start -> end
                    SetStartSprite(path[0], path[1]);
                    SetEndSprite(path[1]);
                    break;

                case 3:
                    // Short path: start -> middle -> end
                    SetStartSprite(path[0], path[1]);
                    SetPreTipSprite(path[1], path[0]);
                    SetEndSprite(path[2]);
                    break;

                default:
                    // Long path: start -> middles -> pre-tip -> end
                    SetStartSprite(path[0], path[1]);
                    SetMiddleSprites(path);
                    SetPreTipSprite(path[^2], path[^3]);
                    SetEndSprite(path[^1]);
                    break;
            }
        }

        private void SetStartSprite(MapGridPoint start, MapGridPoint next)
        {
            GetRotation(start.CoordinatesInt, next.CoordinatesInt, out float rotation);
            PathSprites[start.CoordinatesInt] = new PathSprite
            {
                sprite = PathStartSprite,
                rotation = rotation,
            };
        }

        private void SetPreTipSprite(MapGridPoint preTip, MapGridPoint before)
        {
            GetRotation(before.CoordinatesInt, preTip.CoordinatesInt, out float rotation);
            PathSprites[preTip.CoordinatesInt] = new PathSprite
            {
                sprite = PathPreTipSprite,
                rotation = rotation,
            };
        }

        private void SetEndSprite(MapGridPoint end)
        {
            PathSprites[end.CoordinatesInt] = new PathSprite
            {
                sprite = PathTipSprite,
                rotation = 0,
            };
        }

        private void SetMiddleSprites(List<MapGridPoint> path)
        {
            // Process middle points (excluding start, pre-tip, and end)
            for (int i = 1; i < path.Count - 2; i++)
            {
                var current = path[i];
                var previous = path[i - 1];
                var next = path[i + 1];

                var fromDir = current.CoordinatesInt - previous.CoordinatesInt;
                var toDir = next.CoordinatesInt - current.CoordinatesInt;

                Sprite spriteToUse;
                float rotation;

                if (fromDir == toDir)
                {
                    // Straight line
                    spriteToUse = PathStraightSprite;
                    GetRotation(previous.CoordinatesInt, current.CoordinatesInt, out rotation);
                }
                else
                {
                    // Corner
                    spriteToUse = PathCornerSprite;
                    rotation = GetCornerRotation(fromDir, toDir);
                }

                PathSprites[current.CoordinatesInt] = new PathSprite
                {
                    sprite = spriteToUse,
                    rotation = rotation,
                };
            }
        }

        private float GetCornerRotation(Vector2Int fromDir, Vector2Int toDir)
        {
            return (fromDir, toDir) switch
            {
                (var from, var to)
                    when (from == Vector2Int.up && to == Vector2Int.right)
                        || (from == Vector2Int.left && to == Vector2Int.down) => 270f,
                (var from, var to)
                    when (from == Vector2Int.up && to == Vector2Int.left)
                        || (from == Vector2Int.right && to == Vector2Int.down) => 180f,
                (var from, var to)
                    when (from == Vector2Int.down && to == Vector2Int.left)
                        || (from == Vector2Int.right && to == Vector2Int.up) => 90f,
                _ => 0f,
            };
        }

        private void GetRotation(Vector2Int from, Vector2Int to, out float rotation)
        {
            var direction = to - from;
            rotation = direction switch
            {
                Vector2Int up when up == Vector2Int.up => 0,
                Vector2Int right when right == Vector2Int.right => 270,
                Vector2Int down when down == Vector2Int.down => 180,
                Vector2Int left when left == Vector2Int.left => 90,
                _ => 0,
            };
        }
    }
}
