using Assets.Prototypes.Gameplay.Objects.Components;
using UnityEngine;

public enum TrianglePositionEnum
{
    Top,
    Left,
    Right,
}

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

    public override string ToString()
    {
        return Position.ToString();
    }

    public bool WinsAgainst(TrianglePosition other)
    {
        if (Position == TrianglePositionEnum.Top && other.Position == TrianglePositionEnum.Left)
            return true;
        if (Position == TrianglePositionEnum.Left && other.Position == TrianglePositionEnum.Right)
            return true;
        if (Position == TrianglePositionEnum.Right && other.Position == TrianglePositionEnum.Top)
            return true;
        return false;
    }

    public bool LosesTo(TrianglePosition other)
    {
        if (Position == TrianglePositionEnum.Top && other.Position == TrianglePositionEnum.Right)
            return true;
        if (Position == TrianglePositionEnum.Left && other.Position == TrianglePositionEnum.Top)
            return true;
        if (Position == TrianglePositionEnum.Right && other.Position == TrianglePositionEnum.Left)
            return true;
        return false;
    }

    public bool Equals(TrianglePosition other)
    {
        return Position == other.Position;
    }
}
