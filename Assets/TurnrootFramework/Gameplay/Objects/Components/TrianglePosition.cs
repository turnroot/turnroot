using UnityEngine;

namespace Turnroot.Gameplay.Objects.Components
{
    /// <summary>
    /// Defines positions in the weapon triangle system.
    /// </summary>
    public enum TrianglePositionEnum
    {
        Top,
        Left,
        Right,
        NotOnTriangle,
    }

    /// <summary>
    /// Represents a position in the weapon triangle and provides comparison methods for advantage calculations.
    /// </summary>
    [System.Serializable]
    public class TrianglePosition
    {
        [SerializeField]
        private TrianglePositionEnum _position;

        public TrianglePositionEnum Position
        {
            get => _position;
            set => _position = value;
        }

        public TrianglePosition(TrianglePositionEnum position)
        {
            _position = position;
        }

        public TrianglePosition()
        {
            _position = TrianglePositionEnum.Top;
        }

        public override string ToString() => Position.ToString();

        public bool WinsAgainst(TrianglePosition other) =>
            (Position == TrianglePositionEnum.Top && other.Position == TrianglePositionEnum.Left)
            || (
                Position == TrianglePositionEnum.Left
                && other.Position == TrianglePositionEnum.Right
            )
            || (
                Position == TrianglePositionEnum.Right && other.Position == TrianglePositionEnum.Top
            );

        public bool LosesTo(TrianglePosition other) =>
            (Position == TrianglePositionEnum.Top && other.Position == TrianglePositionEnum.Right)
            || (Position == TrianglePositionEnum.Left && other.Position == TrianglePositionEnum.Top)
            || (
                Position == TrianglePositionEnum.Right
                && other.Position == TrianglePositionEnum.Left
            );

        public bool Equals(TrianglePosition other) => Position == other.Position;
    }
}
